"""Package-only Microsoft Store releases. Uses only the Python standard library.

No automatic retries: a timed-out POST may already have created/committed a
submission. Inspect status before taking any further action.
"""
import argparse
import copy
import json
import os
from pathlib import Path
import re
import sys
import urllib.error
import urllib.parse
import urllib.request
import xml.etree.ElementTree as ET
import zipfile

APP_ID = "9PPP5290T27G"
IDENTITY = "QortxAI.PhotoMetadataRepairInspector"
PUBLISHER = "CN=7F9981DB-6481-4EE5-8747-A3A63C186D7D"
API = "https://manage.devcenter.microsoft.com/v1.0/my"
FAILED = {"Canceled", "CommitFailed", "PreProcessingFailed", "CertificationFailed", "ReleaseFailed", "PublishFailed"}


def version_tuple(value, release=True):
    if not re.fullmatch(r"\d+\.\d+\.\d+\.\d+", value):
        raise ValueError("Store version must have four numeric components.")
    parts = tuple(map(int, value.split(".")))
    if release and parts[3] != 0:
        raise ValueError("Store release version must end in .0.")
    if parts[0] == 0 or any(part > 65535 for part in parts):
        raise ValueError("Version components must be at most 65535; major must be positive.")
    return parts


def read_package(path, expected_version):
    version_tuple(expected_version)
    with zipfile.ZipFile(path) as archive:
        manifest = ET.fromstring(archive.read("AppxManifest.xml"))
    identity = next(child for child in manifest if child.tag.endswith("}Identity"))
    expected = {"Name": IDENTITY, "Publisher": PUBLISHER, "Version": expected_version, "ProcessorArchitecture": "x64"}
    if any(identity.get(key) != value for key, value in expected.items()):
        raise ValueError("Package identity, version or architecture does not match this release.")


def updated_submission(original, filename, version, notes):
    result = copy.deepcopy(original)
    packages = result.get("applicationPackages", [])
    if not packages:
        raise ValueError("No published packages to replace. Complete the first release manually.")
    for package in packages:
        if package.get("architecture", "").lower() != "x64":
            raise ValueError("This workflow only replaces x64 packages; another architecture needs explicit handling.")
        if version_tuple(version) <= version_tuple(package["version"], release=False):
            raise ValueError("Release version must exceed every published package version.")
        package["fileStatus"] = "PendingDelete"
    packages.append({"fileName": filename, "fileStatus": "PendingUpload", "minimumDirectXVersion": "None", "minimumSystemRam": "None"})
    result["targetPublishMode"] = "Immediate"
    for listing in result.get("listings", {}).values():
        listing["baseListing"]["releaseNotes"] = notes
        for override in listing.get("platformOverrides", {}).values():
            override["releaseNotes"] = notes
    # These fields are server generated, including the secret upload URL.
    for field in ("id", "status", "statusDetails", "fileUploadUrl"):
        result.pop(field, None)
    return result


def request(method, url, headers=None, data=None):
    try:
        with urllib.request.urlopen(urllib.request.Request(url, data=data, headers=headers or {}, method=method), timeout=120) as response:
            content = response.read()
            return json.loads(content) if content else {}
    except urllib.error.HTTPError as exc:
        # Do not print error bodies or URLs: they can contain tokens or SAS URLs.
        raise RuntimeError(f"Store request returned HTTP {exc.code}. No automatic retry was made.") from None
    except (urllib.error.URLError, TimeoutError):
        raise RuntimeError("Store request timed out or could not connect. Inspect Partner Center before retrying; the request may have succeeded.") from None


class StoreClient:
    def __init__(self):
        values = {}
        for name in ("STORE_TENANT_ID", "STORE_CLIENT_ID", "STORE_CLIENT_SECRET"):
            values[name] = os.environ.get(name, "")
            if not values[name]:
                raise ValueError(f"Missing {name}. Configure GitHub environment microsoft-store.")
        if not re.fullmatch(r"[0-9a-fA-F-]{36}", values["STORE_TENANT_ID"]):
            raise ValueError("STORE_TENANT_ID must be a tenant GUID.")
        token = request("POST", f"https://login.microsoftonline.com/{values['STORE_TENANT_ID']}/oauth2/token",
                        {"Content-Type": "application/x-www-form-urlencoded"},
                        urllib.parse.urlencode({"grant_type": "client_credentials", "client_id": values["STORE_CLIENT_ID"],
                                                "client_secret": values["STORE_CLIENT_SECRET"], "resource": "https://manage.devcenter.microsoft.com"}).encode())
        self.headers = {"Authorization": f"Bearer {token['access_token']}", "Content-Type": "application/json"}

    def call(self, method, path, body=None):
        return request(method, f"{API}/applications/{APP_ID}{path}", self.headers,
                       json.dumps(body).encode() if body is not None else None)


def save_state(path, submission_id, status, version=None):
    # Only non-sensitive identifiers/status go into the workflow artifact.
    state = {"applicationId": APP_ID, "submissionId": submission_id, "status": status}
    if version:
        state["version"] = version
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(state, indent=2), encoding="utf-8")
    print(f"Store submission {submission_id}: {status}")
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as output:
            output.write(f"Store application: {APP_ID}\n\nSubmission: {submission_id}\n\nStatus: {status}\n\n")


def submit(client, package, version, notes, state_path):
    app = client.call("GET", "")
    if app.get("packageIdentityName") != IDENTITY or app.get("publisherName") != PUBLISHER:
        raise ValueError("Partner Center app identity does not match the package.")
    if app.get("pendingApplicationSubmission"):
        raise ValueError("An existing submission is pending. Check its status; this workflow will not overwrite or delete it.")
    published = (app.get("lastPublishedApplicationSubmission") or {}).get("id")
    if not published:
        raise ValueError("No published submission. Finish the first release in Partner Center.")
    base = client.call("GET", f"/submissions/{published}")
    payload = updated_submission(base, package.name, version, notes)
    upload_path = state_path.parent / "store-upload.zip"
    upload_path.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(upload_path, "w", zipfile.ZIP_DEFLATED) as archive:
        archive.write(package, package.name)
    created = client.call("POST", "/submissions")
    sid = created["id"]
    save_state(state_path, sid, "PendingCommit", version)
    # From here on, failures deliberately leave the draft for inspection.
    client.call("PUT", f"/submissions/{sid}", payload)
    url = urllib.parse.urlparse(created["fileUploadUrl"])
    if url.scheme != "https" or not (url.hostname or "").endswith(".blob.core.windows.net"):
        raise ValueError("Unexpected Store upload endpoint.")
    request("PUT", created["fileUploadUrl"], {"x-ms-blob-type": "BlockBlob", "Content-Type": "application/zip"}, upload_path.read_bytes())
    client.call("POST", f"/submissions/{sid}/commit")
    save_state(state_path, sid, "CommitStarted", version)
    return sid


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=["submit", "status", "preflight"])
    parser.add_argument("--package", type=Path)
    parser.add_argument("--version")
    parser.add_argument("--submission-id")
    parser.add_argument("--state", type=Path, default=Path(".artifacts/store-submission/state.json"))
    args = parser.parse_args()
    if args.mode == "submit":
        if not args.package or not args.version:
            parser.error("submit requires --package and --version")
        read_package(args.package, args.version)
        notes = os.environ.get("STORE_RELEASE_NOTES", "").strip()
        if not notes:
            raise ValueError("STORE_RELEASE_NOTES is required.")
    client = StoreClient()
    if args.mode == "preflight":
        app = client.call("GET", "")
        if app.get("packageIdentityName") != IDENTITY or app.get("publisherName") != PUBLISHER:
            raise ValueError("Unexpected Partner Center app identity.")
        print("Store authentication and app access verified (read-only).")
        return
    sid = args.submission_id
    if args.mode == "submit":
        sid = submit(client, args.package, args.version, notes, args.state)
    if not sid or not re.fullmatch(r"\d+", sid):
        raise ValueError("A numeric submission ID is required for status checks.")
    status = client.call("GET", f"/submissions/{sid}/status")
    save_state(args.state, sid, status["status"], args.version)
    if status["status"] in FAILED:
        raise RuntimeError("Store submission failed. Review certification details in Partner Center.")


if __name__ == "__main__":
    try:
        main()
    except (ValueError, RuntimeError, KeyError, OSError, ET.ParseError, zipfile.BadZipFile) as error:
        # Key/OSError can include paths but never request headers or response bodies.
        print(f"Release stopped: {error}", file=sys.stderr)
        sys.exit(1)
