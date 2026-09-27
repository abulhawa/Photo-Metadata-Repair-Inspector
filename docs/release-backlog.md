# Native Windows release backlog

Updated 2026-09-27. Python remains the behavior reference. M1/M2 shipped to
`main` through PR #7. M3 is tracked by issue #8 and draft PR #9.

## Integration completed

- Approved table UI integrated with the M3 repair engine and its single planner.
- Folder selection remains separate from scanning; loaded results retain their
  scanned root when another folder is selected.
- Preview captures backup preference and scanned root; applying a preview after
  scanning another root is refused.
- Confirmation defaults to Cancel and labels Apply with the change count.
- Sorting retains row objects, selection and scroll offsets; column resizing,
  fixed headers, keyboard selection and empty-result guidance are retained.

## M3 merge gate

- [ ] Combined native Release build, core/Windows tests, Python tests and unsigned
  MSIX pass in GitHub Actions on the final commit.
- [ ] Manually run the combined packaged app on disposable copies of photos:
  scan, filter, select, preview, cancel without writes, then explicitly Apply.
- [ ] Exercise backups on/off, first-original retention, existing-EXIF real JPEG,
  filesystem timestamp preservation and unchanged JPEG image data.
- [ ] Check Repair log presentation and Library/Review state after repair.
- [ ] Record manual evidence before marking PR #9 ready and closing issue #8.

Automated synthetic-fixture coverage is not evidence of the remaining live UI
and real-photo checks. The table UI was owner-approved before integration.

## M4 Store preparation

- [ ] Replace development template artwork with final app icons and tile assets.
- [ ] Choose release version and confirm manifest identity against store-identity.md.
- [ ] Produce the Store submission package; confirm x64 and supported Windows versions.
- [ ] Test clean-profile install, packaged launch, folder access, update and uninstall.
- [ ] Perform available local certification checks and resolve actionable findings.
- [ ] Prepare screenshots, description, features, category, age ratings, pricing,
  availability, support URL/contact and applicable privacy disclosures.
- [ ] Complete Partner Center capability declarations, including runFullTrust,
  and reviewer instructions explaining local photo access and confirmed repairs.
- [ ] Upload package and complete certification/submission using the reserved product.

Microsoft re-signs MSIX packages during Store publishing; a purchased production
certificate is not assumed to be a Store submission prerequisite. See
[package requirements](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/app-package-requirements).

## Deferred

M5 Python macOS/Linux work, repository renaming, RAW support and non-JPEG Taken At
writes remain separate work after native Windows stability.
