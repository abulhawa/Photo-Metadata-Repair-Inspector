# Release validation attempt — 2026-09-27

## Two-picker date repair refresh

The owner checked the various settings, approved the revised UI, and supplied
replacement Review and confirmation screenshots. Both were visually inspected
at 1905 × 1250 pixels with the approved icon, source/destination controls,
backup checkbox and explicit Apply/Cancel visible. The final four images and
captions are versioned under `artwork/store-screenshots/`; byte-identical upload
copies are in `.artifacts/store-screenshots/`. This completes screenshot
preparation and owner review of the settings. It does not independently attest
every destructive safety case, real-photo preservation or clean lifecycle.

The Windows repair UI now uses Set (Taken At/Created/Modified) and From
(Filename date/Taken At/Created/Modified, excluding the destination). The default
remains Taken At from Filename date. Choices stay selected during the session;
a source that becomes the destination falls back to Filename date.
All nine valid combinations use the existing preview, skip explanations,
backup capture and explicit confirmation flow. The Python reference retains
its original six-method menu; legacy native method semantics remain tested.

Local Release package build and reserved-manifest validation passed. 38 core
and 36 Windows tests passed, including actual application, original backups
and audit logging for Taken At from Modified, Created from Modified and
Modified from Created. Evidence: `.artifacts/date-source-validation/`.
Candidate SHA-256:
`1C2260096B91703ACC6216C60780EF9E9D8B001297E30E4E4754E2E254604642`.
Native appearance is verified by the owner and inspected final screenshots.
The Windows App Certification Kit passed this exact refreshed package:
OVERALL_RESULT=PASS, PARTIAL_RUN=FALSE, tool 10.0.26100.7705 on Windows build
26200. Report generated 2026-09-27 22:54:30 local time. Evidence:
`.artifacts/certification-two-picker/report.xml` and `summary.json`. The package
hash was rechecked after certification and is unchanged. This is local kit
validation; clean lifecycle, real-photo safety evidence, minimum-OS testing and
Microsoft Store certification remain separate gates.

## Icon capture refresh

Four owner-provided actual app captures from `C:\Projects\CaptureOutput` were
visually inspected and copied byte-for-byte to `.artifacts/store-screenshots/`.
The reviewed images are 1905 × 1250 PNGs, each below 221 KB, with the approved
title-bar icon visible and neutral `C:\TestPhotos` paths. Upload order, captions
and original/copy hashes are recorded in `screenshot-manifest.json` there.

Observed: seven Library records; three selected JPEGs missing Taken At; preview
with three applicable filename-to-Taken-At changes and explicit Apply/Cancel;
Repair log with three OK entries and backup paths. This supports successful
synthetic repair UI execution and icon appearance. Screenshots alone do not
verify package identity, cancellation without writes, backup integrity/retention,
all methods, real-photo preservation, post-repair row refresh or clean lifecycle.

The approved photo-and-clock artwork is now embedded in the executable and set
as the WinUI window icon. The icon contains 16/24/32/48/256 pixel frames and is
included in the rebuilt MSIX. Release build and manifest validation passed;
the optional symbol-tool warning remains. New candidate SHA-256:
`659CC3663C2BACD96D4018B9EDE2B716E5F5CF63FEE30C4E44CE39E1EB325288`.
This package includes uncommitted icon changes on top of `173cb9a`; the CI and
WACK PASS below apply to the preceding package hash. Icon appearance has now
been observed in the supplied captures; certification of this refreshed package
remains pending.

## Final desktop UI candidate (supersedes the earlier package build)

Source: `173cb9a88694a224cd6d3dab1732af405af9e778` (PR #12 merged).
Unsigned x64 candidate 1.0.0.0 rebuilt successfully with reserved identity,
Windows.Desktop minimum 10.0.17763.0 and runFullTrust verified. SHA-256:
`CB68F47DDCC57C0C65D08AC97B406E2A6A283129E5ACBC2859983AE592877DC6`.
The only packaging warning is unavailable optional mspdbcmf.exe symbol tooling.
Build/hash report: `.artifacts/store-release/package-report.json`.

- [Native CI for this exact commit](https://github.com/abulhawa/Windows-Photo-Repair-Inspector/actions/runs/36332238523)
  passed solution build, core tests, Windows adapter/presentation tests and MSIX build.
- [Python CI for this exact commit](https://github.com/abulhawa/Windows-Photo-Repair-Inspector/actions/runs/36332238502)
  passed; local Python tests also passed (46) and compileall succeeded.
- Local native runs were blocked loading PhotoRepair.Core.dll by Application
  Control (0x800711C7): core 0/34 executed successfully; Windows 4 passed and
  29 were blocked. These are not successful local native runs or demonstrated
  application regressions. No security policy was changed.
- Screenshot capture bundle preparation is available through
  `scripts/prepare-store-capture.ps1`; actual capture and manual observation
  remain pending because native desktop control is unavailable in this session.
- Final-candidate Windows App Certification Kit: OVERALL_RESULT=PASS,
  PARTIAL_RUN=FALSE, tool 10.0.26100.7705, Windows build 26200. Report generated
  2026-09-27 18:18:56 local time. Evidence:
  `.artifacts/certification-final-ui/report.xml` and `summary.json`.
  The candidate SHA-256 remained unchanged after testing. This is local package
  certification evidence, not Store certification or manual workflow evidence.

The WACK PASS and test-signed lifecycle evidence below belong to the earlier
candidate. The final rebuilt package has not yet completed clean signed
installation/update/launch/uninstall, real-photo workflow checks, minimum-OS
testing or Store certification. Existing install-test packages and sandbox-test
files predate PR #12; do not use them as final UI evidence.
See [store-capture-guide.md](store-capture-guide.md) for the final manual pass.

### Sandbox capture correction

The first capture launch failed registration with 0x80073CFF: the fresh Sandbox
did not permit unsigned development registration. Evidence:
`.artifacts/store-screenshots/sandbox-launch-error.txt`. App files were present;
the app was not registered or launched. The capture launcher now enables
Developer Mode inside the disposable Sandbox only, checks the Sandbox account
before doing so, and creates a packaged-activation desktop shortcut. The corrected
launcher has been prepared and syntax-checked; successful guest launch still
requires observation. No host security setting was changed.

## Earlier candidate evidence

Host: Windows 11 Pro, build 26200. Current account is not elevated and already
contains development package 0.1.0.0. This is not a clean test profile.

## Verified

- Candidate 1.0.0.0 builds and its Store identity/x64 manifest pass validation.
- Direct unsigned installation was attempted with Add-AppxPackage -AllowUnsigned.
  Windows refused it with 0x80073D2C: publisher is not in the unsigned namespace.
  The reserved Store publisher was not changed to bypass the restriction.
- Existing development registration remained version 0.1.0.0.
- Separate test-signed 1.0.0.0 and 1.0.1.0 packages were prepared under
  `.artifacts/install-test/`. The update fixture changes only package version.
- A three-day test certificate was created with a nonexportable signing key.
  After signing both packages, its private-key certificate was removed from the
  signing store. Only the public certificate is in the test bundle. It has not
  been added to this machine's trust store.

## Local certification kit result

The installed Windows App Certification Kit completed the unsigned candidate
check with OVERALL_RESULT=PASS, PARTIAL_RUN=FALSE. Report tool version:
10.0.26100.7705; package 1.0.0.0; Windows 10.0.26200.0. Generated at
2026-09-27 16:51:53. Evidence: `.artifacts/certification/report.xml` and
`summary.json`. The report is local package evidence, not a Microsoft Store
submission result or proof of clean install/update/uninstall. The development
package remained at 0.1.0.0 after this check.

## Not yet verified

- Clean signed-package installation/update/uninstallation.
- Packaged launch and actual app screenshots.
- Oldest supported Windows version.
- Microsoft Store certification (the local kit is a separate pre-submission check).

## Owner-authorized laptop lifecycle test

On 27 September 2026, the owner's Windows 11 laptop passed package registration
checks for removal of the previous 0.1.0.0 package, installation of test-signed
1.0.0.0, update to 1.0.1.0, uninstall, and reinstall of 1.0.0.0. External synthetic
sentinel files retained their hashes after uninstall. These sentinels were not
genuine repaired photos. This was an existing development profile, not a clean
Windows installation. Evidence: `.artifacts/install-test/laptop-lifecycle-results.json`.
The temporary publisher trust was removed after testing.

Packaged launch failed with AppModel error 0x800711C7. Code Integrity events 3077
and 3033 identified the installed PhotoRepair.App.exe as failing the Custom1
signing level; Smart App Control was enabled. The executable was individually
unsigned, despite the MSIX having a local test signature. No security policy
was changed. Version 1.0.0.0 remains registered, but launch is blocked on this
laptop. Lifecycle registration success and the local certification kit PASS
must not be treated as a successful end-to-end app test.

Use a Microsoft Store-signed test distribution or appropriately trusted code
signing for the next laptop launch test. See Microsoft's
[Smart App Control signing guidance](https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/code-signing-for-smart-app-control).
Actual screenshots, workflow verification, clean-machine checks and Store
certification remain outstanding.

Native desktop control is disabled in the current agent session, so actual app
screenshots cannot be captured or inspected through the available browser tools.

## Complete the lifecycle pass

Copy `.artifacts/install-test/` (the two MSIX files and test-publisher.cer) plus
`scripts/test-package-lifecycle.ps1` to a disposable Windows VM. Use a fresh test
profile without the app registered. Run the script in elevated PowerShell with
`-BundleDirectory` pointing at the copied bundle and `-Stage Install`, then
`-Stage Update`, then `-Stage Uninstall`. The Install stage explicitly trusts only
the short-lived test publisher; Uninstall removes that trust. Do not use the
test-signed packages for Partner Center submission. Recreate the bundle after
the test certificate expires (30 September 2026).

Between stages, launch the app from Start, exercise the workflow using disposable
photos and check that their hashes/backups/logs survive uninstall. The script
records package-registration evidence but does not claim to verify those UI and
user-file checks.

Capture genuine app-window screenshots for Library, selected Review rows, the
confirmation dialog and Repair log. Use synthetic filenames and a neutral test
folder; avoid personal photos/paths. Save screenshots under
`.artifacts/store-screenshots/`. Do not use generated artwork or mockups in place
of screenshots.
