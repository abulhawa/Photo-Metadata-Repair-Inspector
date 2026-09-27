# Native Windows release backlog

Updated 2026-09-27. Python remains the behavior reference. M1/M2 shipped to
`main` through PR #7. M3 merged through PR #9; issue #8 is closed. M4 is tracked by issue #10.

## Integration completed

- Approved table UI integrated with the M3 repair engine and its single planner.
- Folder selection remains separate from scanning; loaded results retain their
  scanned root when another folder is selected.
- Preview captures backup preference and scanned root; applying a preview after
  scanning another root is refused.
- Confirmation defaults to Cancel and labels Apply with the change count.
- Sorting retains row objects, selection and scroll offsets; column resizing,
  fixed headers, keyboard selection and empty-result guidance are retained.

## M3 evidence and remaining release validation

- [x] Combined native Release build, core/Windows tests, Python tests and unsigned
  MSIX pass in GitHub Actions on the final commit.
- [ ] Manually run the combined packaged app on disposable copies of photos:
  scan, filter, select, preview, cancel without writes, then explicitly Apply.
- [ ] Exercise backups on/off, first-original retention, existing-EXIF real JPEG,
  filesystem timestamp preservation and unchanged JPEG image data.
- [ ] Check Repair log presentation and Library/Review state after repair.
- [x] Owner reported the direct executable workflow working; PR #9 merged.
  Detailed safety cases and packaged activation were not individually attested.

Automated synthetic-fixture coverage is not evidence of the remaining live UI
and real-photo checks. The table UI was owner-approved before integration.

## M4 Store preparation

- [x] Replace template artwork with the owner-requested photo-and-clock icon and tile assets.
  Owner approved the artwork on 2026-09-27.
- [x] Prepare candidate 1.0.0.0 and verify built identity against store-identity.md.
- [x] Build direct unsigned x64 MSIX and validate manifest/hash.
  Declared minimum is Windows 10 build 17763; actual oldest-OS testing remains pending.
- [ ] Test clean-profile install, packaged launch, folder access, update and uninstall.
  Owner-authorized laptop installation/update/uninstallation registration checks
  passed, including external sentinel retention and reinstall. Packaged launch
  is blocked by Smart App Control; this existing developer profile does not
  replace clean-machine or actual workflow verification.
- [x] Local Windows App Certification Kit: PASS for 1.0.0.0 on build 26200.
  Microsoft Store certification remains pending.
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

## Prepared materials

- Free listing and screenshot plan: [store-listing.md](store-listing.md).
- Privacy policy: https://qortxai.com/projects/photo-metadata-repair-inspector/privacy/
  ([local source](privacy.md)).
- Packaging, clean-profile checklist and reviewer notes: [store-validation.md](store-validation.md).
- Reproducible package/report generation: `scripts/build-store-package.ps1`.
- Source artwork and reproducible size variants: `artwork/` and `scripts/build-icon-assets.ps1`.

Latest installation/certification evidence and blocked checks:
[release-validation-results.md](release-validation-results.md).

## Desktop UI polish (next batch)

Adds File/View/Help menus, visible Library/Review/Repair log tabs, a persistent
status footer, severity-aware native notifications, usage help, privacy/support
links and About with package version and approved artwork. Existing scan,
selection and repair actions are reused.

Release build passed without warnings; 33 Windows presentation tests passed.
Owner reviewed and accepted the revised UI in Windows Sandbox, including
view-specific controls, review-column order and compact spacing. Discovery now
reports live folder/media counts before metadata reading. Native host launch
previously hit Application Control restrictions; security settings were retained.
Genuine Store screenshots and packaged workflow checks remain pending.

## Final UI candidate validation

The repair dropdown has since been replaced by compact Set/From controls with
all nine valid date combinations. Local verification: 38 core and 36 Windows
tests passed, plus Release package build and manifest validation. The owner
checked the settings and supplied final Review/confirmation screenshots; the
four-image set is versioned in `artwork/store-screenshots/`.
The current package also passed a fresh, full local certification-kit run;
evidence is in `.artifacts/certification-two-picker/`. Store submission and the
remaining manual safety/clean-profile/minimum-OS gates are still open.

Final Store screenshots are captured and visually reviewed: Library, selected
Review rows, explicit confirmation and successful Repair log. All four PNGs are
1905 × 1250 with the approved title-bar icon. Files and captions are under
`.artifacts/store-screenshots/`. Remaining manual safety/lifecycle gates are
not closed by these images. The icon-refreshed package also needs certification
of its new hash; the preceding package passed the local kit.

Final UI commit `173cb9a` has successful native and Python CI. The local unsigned
1.0.0.0 candidate was rebuilt and its reserved manifest identity verified.
Local Python tests passed (46); local native test execution is blocked by
Application Control (0x800711C7), so CI supplies the automated native evidence.
The earlier WACK and lifecycle reports predate this final UI. A fresh WACK pass
on the rebuilt candidate completed with PASS and PARTIAL_RUN=FALSE; evidence is
under `.artifacts/certification-final-ui/`. Final screenshots and live workflow
checks remain open.

Run `scripts/prepare-store-capture.ps1` to prepare an isolated Sandbox capture
bundle without host installation. See [store-capture-guide.md](store-capture-guide.md)
for final captions and the remaining manual gates. Preparation is not capture
or packaged lifecycle evidence.
