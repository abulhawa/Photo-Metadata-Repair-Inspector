# Native Windows release backlog

Updated 2026-09-27. Store submission is complete. Microsoft certification is
pending; publication is configured to happen automatically if approved.
No further submission preparation is currently required unless Microsoft requests changes.

## Completed release work

- [x] M1/M2 merged through PR #7; M3 repair integration merged through PR #9.
- [x] Store preparation, desktop polish and final captures merged through PRs #11–#13.
- [x] M4 issue #10 closed as Completed.
- [x] Approved table UI, File/View/Help menus, Library/Review/Repair log tabs,
  status footer, notifications, help, privacy/support links and About integrated.
- [x] Compact Set/From controls support all nine valid repair date combinations.
- [x] Owner approved the UI, artwork and final four Store screenshots.
- [x] Candidate 1.0.0.0 identity, manifest and package hash verified.
- [x] Native and Python CI passed; final local tests passed: 38 core,
  36 Windows and 46 Python tests.
- [x] Final candidate passed a full local Windows App Certification Kit run.
- [x] Store listing, screenshots, privacy/support information, capability
  declarations and required Partner Center fields completed for submission.
- [x] Package uploaded and Microsoft Store submission completed.
- [x] Repository and local folder renamed to Photo-Metadata-Repair-Inspector;
  active repository references and public website links updated.
- [x] Release tag v1.0.0 marks main commit 1678dbf. Its tree matches submitted
  package source commit 673b0e414753f83515641b656c47c93d87912b9c.
  Repository-reference updates are a separate later commit, 4d2276e.

## Pending Store outcome

- [ ] Receive Microsoft's certification decision and confirm automatic publication.
- [ ] Address certification feedback if Microsoft requests changes.

Submission is not evidence of certification approval. The local certification
kit is a separate completed check.

## Validation follow-ups without complete recorded evidence

These checks remain unverified in the saved evidence. They are not unfinished
Store submission tasks and should not be marked passed merely because submission
or screenshots are complete.

- [ ] Full manual repair-safety matrix: cancel without writes, backups on/off,
  first-original retention, existing-EXIF real JPEG, filesystem timestamp
  preservation and unchanged JPEG image data.
- [ ] Full post-repair Library/Review state verification. Successful Repair log
  presentation is visible in the accepted captures.
- [ ] Clean-profile signed-package install, launch, folder access, update and
  uninstall, including retention of actual user photos, backups and logs.
  Laptop registration/update/uninstall/reinstall and external sentinel checks
  passed; earlier laptop launch was blocked by Smart App Control. These checks
  do not establish a complete clean-profile workflow pass.
- [ ] Actual oldest-supported-OS testing: Windows 10 build 17763.

Owner-approved UI and observed workflows, automated fixtures, and screenshots
provide evidence for their respective checks, not the entire safety matrix.

## Deferred product work

- M5 Python macOS/Linux work.
- RAW support.
- Non-JPEG Taken At writes.

## Evidence and supporting materials

- Final source/captures: PR #13, artwork/store-screenshots/.
- Submitted package and validation snapshot: .artifacts/store-submission-673b0e4/.
- Submission proof: .artifacts/store-submission-proof/.
- Final local certification: .artifacts/certification-two-picker/.
- [Detailed validation history](release-validation-results.md).
- [Store listing source](store-listing.md).
- [Manual validation and capture guide](store-capture-guide.md).
- [Packaging and reviewer notes](store-validation.md).
- Public privacy policy: https://qortxai.com/projects/photo-metadata-repair-inspector/privacy/.

Earlier validation reports and submission snapshots retain their historical
status and package identity; this backlog is the current release-work summary.
