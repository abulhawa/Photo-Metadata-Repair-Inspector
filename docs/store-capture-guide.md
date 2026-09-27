# Final Store capture and manual validation

This guide applies to candidate 1.0.0.0, based on desktop UI commit
`173cb9a88694a224cd6d3dab1732af405af9e778` plus the icon and two-picker repair UI.
Run `scripts/prepare-store-capture.ps1` after building the Store candidate.
Open the returned `StoreCapture.wsb` on a machine with Windows Sandbox available.
It copies synthetic fixtures into `C:\TestPhotos`, attempts loose package
registration, and opens the registered app. It does not change host trust or
security policy. The launcher enables Developer Mode only inside the disposable
Sandbox, where it is required for unsigned loose registration, and adds an app
shortcut to the Sandbox desktop. Source files are mapped read-only; only the screenshot output
folder is writable from Sandbox. Close Sandbox only after saving evidence.

If registration or launch fails, preserve `sandbox-launch-error.txt`. Use an
appropriately configured disposable VM or Store-signed distribution for packaged
testing. Direct executable launch cannot prove packaged activation. A successful
loose registration cannot prove signed-MSIX install/update/uninstall.

## Capture sequence

Use English UI, 100% display scaling if readable, and an app window at least
1366 × 768 pixels. Prefer 1920 × 1080. Capture the app window including title bar;
exclude the host desktop, console and Sandbox toolbar. Use actual pixels, without
upscaling, mockups or added marketing overlays. Save PNGs below 50 MB to
`C:\CaptureOutput` (mapped to the host `.artifacts/store-screenshots`).
Microsoft's [MSIX screenshot requirements](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/screenshots-and-images)
were checked on 2026-09-27. Captions must be at most 200 characters.

| File | Actual UI to capture | Store caption |
| --- | --- | --- |
| 01-library.png | Select C:\TestPhotos, then Scan. Library shows compared dates and rows. | Compare photo dates from file timestamps, capture metadata and filenames in one place. |
| 02-review.png | Review with missing capture dates selected, Set: Taken At, From: Filename date and backups enabled. | Select photos and choose a source date, with original-file backups enabled by default. |
| 03-confirmation.png | Review changes dialog with exact before/after dates and Apply count. | Inspect proposed date changes before explicitly confirming a batch repair. |
| 04-repair-log.png | After Apply, Repair log shows successful attempts; retain useful column widths. | Check the local repair log after applying your confirmed changes. |

Select only applicable JPEG rows for the confirmation screenshot. Capture the
dialog before applying, cancel once to validate cancellation, then reopen it and
explicitly Apply. The fixtures are tiny generated test JPEGs; they support
screenshots and synthetic checks, not real-photo preservation evidence.

## Record observations separately from screenshots

Save `manual-results.md` in CaptureOutput with OS/build, package version, candidate
SHA-256 from capture-bundle.json, actual launch outcome, and pass/fail/not-tested
for each check below. Include observed values and failures. Do not mark a check
passed solely because registration, CI or a screenshot succeeded.

1. Folder selection does not scan. Scan/Stop, search, sort, resize columns,
   keyboard/range selection, menus, Help and About work.
2. Cancel confirmation leaves content hashes, Created/Modified timestamps,
   backup directory and log unchanged. Read-only metadata access may affect Accessed.
3. Exercise all nine repair combinations on fresh suitable copies; verify displayed
   and actual target dates. Confirm unavailable-source, already-matching and
   non-JPEG Taken At skips.
4. Apply with backups on; compare the backup hash to the original. Repeat a
   repair and verify the first backup remains unchanged. Apply on fresh copies
   with backups off and verify the warning, absence of backup and audit entry.
5. Copy a genuine camera JPEG with existing EXIF into the disposable VM.
   Verify Taken At changes, filesystem timestamps are preserved from immediately
   before Apply, and bytes from JPEG Start Of Scan onward remain identical.
   Retain original/after hashes and metadata readings privately.
6. Confirm refreshed Library/Review rows and meaningful Repair log status.
7. Separately test signed-MSIX installation, update, launch after both, and
   uninstall in a clean disposable VM. Compare user-photo, backup and log hashes
   after uninstall. Existing laptop registration checks do not satisfy this gate.
8. Test actual Windows 10 build 17763 (advertised minimum) and Windows 11.

The final icon and two-picker candidate passed the local certification kit
(full run) on Windows build 26200; see
`.artifacts/certification-two-picker/summary.json`. Final captures and Partner
Center submission are complete. Microsoft Store certification remains pending.
The manual checks above retain any evidence gaps recorded in release-backlog.md.
