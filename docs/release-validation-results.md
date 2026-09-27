# Release validation attempt — 2026-09-27

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
