# Store release validation

## Build the candidate

From the repository root:

```powershell
pwsh -NoProfile -File scripts/build-store-package.ps1
```

If local policy blocks script files, run its reviewed contents inline from the
repository root without changing the execution policy:

```powershell
& ([scriptblock]::Create((Get-Content scripts/build-store-package.ps1 -Raw)))
```

The script builds an unsigned x64 MSIX accepted by Partner Center and checks the built
manifest's reserved identity, publisher, version, architecture, device family and
desktop capability. Generated artifacts and SHA-256 report are under
`.artifacts/store-release`. Build success is not installation/certification evidence.
Never upload signing keys or certificates to GitHub.

## Clean-profile checks â€” still required

Use a disposable Windows user/VM with Developer Mode or an appropriate test-signing
setup. Unsigned MSIX files are for Store upload and cannot be treated as ordinarily
installable sideload packages. A loose development registration is useful for
packaged activation checks, but does not prove clean MSIX installation.

- [ ] Install a test-signed candidate without Visual Studio or a Python runtime.
- [ ] Launch from Start and verify the process has the reserved package identity.
- [ ] Select a folder; ensure selection does not start scanning automatically.
- [ ] Scan, cancel, search, sort, resize columns and exercise range selection.
- [ ] Cancel a repair preview and verify there are no writes/backups/log entries.
- [ ] Apply each of the six methods on suitable disposable examples.
- [ ] Check skips for unavailable sources, matching dates and non-JPEG Taken At.
- [ ] Exercise backups on/off and preserve the first original after repeated repair.
- [ ] Check a real JPEG's metadata, image payload and filesystem timestamp preservation.
- [ ] Check log output and Library/Review refresh after repair.
- [ ] Install a higher-version candidate and verify update and launch.
- [ ] Uninstall; confirm the package is removed and user files/backups/logs remain.
- [ ] Check the oldest advertised Windows version as well as Windows 11.

Record OS build, package version/hash, observations and failures for each pass.
The owner's direct executable workflow test on 2026-09-27 passed, but was not a
clean-profile MSIX install/update/uninstall test.

## Certification reviewer notes draft

This is a packaged WinUI desktop utility. The runFullTrust capability is needed
for local filesystem and Windows photo metadata access, timestamp mutation,
backup creation, CSV audit logging and shell integration. No elevation, sign-in,
cloud upload or background service is required.

To exercise repairs, create disposable JPEG copies with filename dates such as
IMG_20240321_174532.jpg, select and scan their folder, open Review, select the files,
choose a method and click Review changes. Changes require explicit Apply
confirmation. Backups are enabled by default. Taken At writes are JPEG-only;
other supported media types remain inspectable.

The package targets Windows.Desktop and x64. The app icons are owner-approved. Actual packaged-app screenshots remain
required before submission. Review Partner Center's
current capability questions and certification feedback before submitting.

The StoreUpload target currently fails locally on missing symbol tooling and
System.Security.Permissions. The direct MSIX build avoids that optional container
path. Upload the MSIX reported by the script, not artifacts from a failed build.
