"""Offline release tests: never call Microsoft Store or read credentials."""
import copy
import importlib.util
from pathlib import Path
import zipfile

import pytest

spec = importlib.util.spec_from_file_location("store_submission", Path(__file__).parents[1] / "scripts/store_submission.py")
store = importlib.util.module_from_spec(spec)
spec.loader.exec_module(store)


def published():
    return {"id": "100", "fileUploadUrl": "secret", "status": "Published",
            "pricing": {"priceId": "Free"}, "visibility": "Public",
            "listings": {"en-us": {"baseListing": {"description": "Keep this", "releaseNotes": "Old", "images": [{"fileName": "image.png", "fileStatus": "Uploaded"}]}}},
            "applicationPackages": [{"id": "200", "fileName": "old.msix", "version": "1.0.0.0", "architecture": "X64", "fileStatus": "Uploaded"}]}


def test_preserves_listing_and_price_and_replaces_package():
    original = published()
    before = copy.deepcopy(original)
    updated = store.updated_submission(original, "new.msix", "1.0.1.0", "Menu fix")
    assert original == before
    assert updated["pricing"] == original["pricing"]
    assert updated["visibility"] == "Public"
    listing = updated["listings"]["en-us"]["baseListing"]
    assert listing["description"] == "Keep this"
    assert listing["images"] == original["listings"]["en-us"]["baseListing"]["images"]
    assert listing["releaseNotes"] == "Menu fix"
    assert updated["applicationPackages"][0]["fileStatus"] == "PendingDelete"
    assert updated["applicationPackages"][1]["fileStatus"] == "PendingUpload"
    assert updated["targetPublishMode"] == "Immediate"
    assert "fileUploadUrl" not in updated


@pytest.mark.parametrize("version", ["1.0.0.0", "0.9.0.0", "1.0.1.1", "1.0.1", "65536.0.0.0", "bad"])
def test_rejects_invalid_or_non_increasing_version(version):
    with pytest.raises(ValueError):
        store.updated_submission(published(), "new.msix", version, "Notes")


def test_does_not_silently_remove_other_architectures():
    base = published()
    base["applicationPackages"][0]["architecture"] = "ARM64"
    with pytest.raises(ValueError, match="architecture"):
        store.updated_submission(base, "new.msix", "1.0.1.0", "Notes")


def test_checks_actual_package_identity(tmp_path):
    package = tmp_path / "test.msix"
    manifest = f'<Package xmlns="urn:test"><Identity Name="{store.IDENTITY}" Publisher="{store.PUBLISHER}" Version="1.0.1.0" ProcessorArchitecture="x64" /></Package>'
    with zipfile.ZipFile(package, "w") as archive:
        archive.writestr("AppxManifest.xml", manifest)
    store.read_package(package, "1.0.1.0")
    with pytest.raises(ValueError, match="identity"):
        store.read_package(package, "1.0.2.0")


def test_pending_submission_blocks_every_mutation(tmp_path):
    class Client:
        def call(self, method, path, body=None):
            assert (method, path) == ("GET", "")
            return {"packageIdentityName": store.IDENTITY, "publisherName": store.PUBLISHER, "pendingApplicationSubmission": {"id": "123"}}
    with pytest.raises(ValueError, match="pending"):
        store.submit(Client(), tmp_path / "unused.msix", "1.0.1.0", "Notes", tmp_path / "state.json")


def test_saves_submission_before_upload_failure(tmp_path, monkeypatch):
    package = tmp_path / "new.msix"
    package.write_bytes(b"fixture")
    calls = []

    class Client:
        def call(self, method, path, body=None):
            calls.append((method, path))
            if path == "":
                return {"packageIdentityName": store.IDENTITY, "publisherName": store.PUBLISHER, "lastPublishedApplicationSubmission": {"id": "100"}}
            if method == "GET":
                return published()
            if method == "POST":
                return {"id": "123", "fileUploadUrl": "https://example.blob.core.windows.net/upload?sig=secret"}
            return {}

    def fail(*args):
        raise RuntimeError("Upload failed")

    monkeypatch.setattr(store, "request", fail)
    state = tmp_path / "state.json"
    with pytest.raises(RuntimeError, match="Upload"):
        store.submit(Client(), package, "1.0.1.0", "Notes", state)
    assert '"submissionId": "123"' in state.read_text()
    assert "secret" not in state.read_text()
    assert ("POST", "/submissions/123/commit") not in calls


def test_successful_release_uploads_zip_then_commits_once(tmp_path, monkeypatch):
    package = tmp_path / "new.msix"
    package.write_bytes(b"package bytes")
    calls = []

    class Client:
        def call(self, method, path, body=None):
            calls.append((method, path))
            if path == "":
                return {"packageIdentityName": store.IDENTITY, "publisherName": store.PUBLISHER, "lastPublishedApplicationSubmission": {"id": "100"}}
            if method == "GET":
                return published()
            if path == "/submissions":
                return {"id": "123", "fileUploadUrl": "https://example.blob.core.windows.net/upload?sig=secret"}
            return {}

    def upload(method, url, headers, data):
        import io
        assert method == "PUT"
        assert headers == {"x-ms-blob-type": "BlockBlob", "Content-Type": "application/zip"}
        with zipfile.ZipFile(io.BytesIO(data)) as archive:
            assert archive.namelist() == ["new.msix"]
            assert archive.read("new.msix") == b"package bytes"
        calls.append(("PUT", "upload"))
        return {}

    monkeypatch.setattr(store, "request", upload)
    state = tmp_path / "state.json"
    assert store.submit(Client(), package, "1.0.1.0", "Notes", state) == "123"
    assert calls == [("GET", ""), ("GET", "/submissions/100"), ("POST", "/submissions"),
                     ("PUT", "/submissions/123"), ("PUT", "upload"), ("POST", "/submissions/123/commit")]
    assert '"status": "CommitStarted"' in state.read_text()


def test_http_error_does_not_expose_upload_url_or_body(monkeypatch):
    import urllib.error
    import io

    def fail(*args, **kwargs):
        raise urllib.error.HTTPError("https://host?sig=secret", 403, "secret", {}, io.BytesIO(b"secret"))

    monkeypatch.setattr(store.urllib.request, "urlopen", fail)
    with pytest.raises(RuntimeError) as error:
        store.request("POST", "https://host?sig=secret")
    assert "HTTP 403" in str(error.value)
    assert "secret" not in str(error.value)
