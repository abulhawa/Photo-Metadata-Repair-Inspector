# Microsoft Store listing draft

Product: Photo Metadata Repair Inspector. Publisher: QortxAI.
Reserved Store ID: 9PPP5290T27G. Initial release candidate: 1.0.0.0, x64, English.
Suggested category: Utilities & tools. Pricing: free (owner-selected). Availability
regions and publication date remain to be selected in Partner Center.
Complete the age-rating questionnaire using actual app functionality; no rating is presumed here.

## Short description

Inspect photo dates, compare metadata and filename timestamps, and review repairs before applying them.

## Description

Copied, restored or exported photo collections can have confusing dates.
Photo Metadata Repair Inspector helps you compare Windows Created and Modified
timestamps, photo Taken At metadata and dates found in filenames in one table.

Choose a folder, scan its supported media files, and filter or sort the results.
The Review view highlights missing JPEG capture dates and capture dates later
than the file's Created or Modified timestamp. Common copy-related date ordering
is not automatically treated as an error.

Select files and choose where a replacement date should come from. Review the
proposed changes before confirming. Original-file backups are enabled by default,
and repair attempts are recorded in a local CSV log. Successfully repaired files
are refreshed in the table without rescanning the entire collection.

The app works on your computer without an account or a photo-upload service.
Taken At repairs support JPEG files only. Metadata reading for other image formats
depends on Windows codec support. Videos can be inspected using filesystem and
filename dates; the app does not write video capture metadata. It does not infer
missing timezone information or repair damaged image content.

## Feature bullets

- Compare Created, Modified, Taken At and filename dates side by side.
- Scan folders recursively with progress and a Stop control.
- Search, filter and sort supported photo and video records.
- Select files with checkboxes, keyboard shortcuts and range selection.
- Preview batch repairs and explicitly confirm changes.
- Keep first-original backups and a local repair audit log.
- Repair Windows Created/Modified dates and JPEG Taken At metadata.

## Release notes

Initial native Windows release with folder inspection, timestamp comparison,
review filters, batch repair previews, optional backups and local repair logs.

## Support and privacy fields

Support URL: https://github.com/abulhawa/Windows-Photo-Repair-Inspector/issues
Privacy policy URL: https://qortxai.com/projects/photo-metadata-repair-inspector/privacy/
Privacy source: docs/privacy.md; the public page also explains the website’s
separate hosting/font/analytics services.
Do not add personal photos or sensitive paths to public support tickets.

## Screenshot capture plan

Use the final packaged release and disposable synthetic media, with no personal
paths or photos. Capture actual app UI; do not substitute mockups.

1. Library: mixed photo/video results and the compared dates.
2. Review: selected JPEGs and visible repair method/backup controls.
3. Confirmation: proposed before/after dates and Apply count.
4. Results: refreshed Library/Review and the local repair log.

Artwork is owner-approved. Screenshots remain outstanding. Verify image requirements in
Partner Center before uploading. See the official
[submission guide](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/create-app-submission).
