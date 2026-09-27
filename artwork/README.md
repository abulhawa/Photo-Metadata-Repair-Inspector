# App artwork

`photo-clock-master.png` is the photo-and-clock icon requested by the owner for
the free Store release. Generated with the built-in imagegen tool on 2026-09-27.
`scripts/build-icon-assets.ps1` derives transparent package PNGs at Windows tile,
splash, scale and target sizes without changing the design. The source master is
retained so assets can be regenerated. The owner approved the artwork on
2026-09-27. Generated artwork is not a Store screenshot.

`Assets/PhotoRepair.ico` packages the approved 16, 24, 32, 48 and 256 pixel PNG
variants into a Windows icon. It is embedded in the executable and used by the
app window so the title bar, taskbar and desktop shortcut share the artwork.

`store-screenshots/` contains the four actual owner-captured app images, reviewed
at 1905 × 1250 pixels. `captions.json` provides upload order, captions and SHA-256
hashes. These genuine app captures include the final Set/From repair controls;
they are separate from generated icon artwork.

Generation prompt:

> Use case: logo-brand. Generate a final production app icon master for Photo Metadata Repair Inspector, a Windows photo timestamp repair utility. One clean photo frame with a simple mountain silhouette and sun, combined with one prominent analog clock badge. Crisp flat vector-like geometric forms, strong silhouette readable at 24px, professional Windows desktop aesthetic. No words, no numbers, no letters, no mockup, no surrounding presentation. Center the complete symbol with generous transparent margin (roughly 15 percent on every edge). Distinct photo-and-clock symbol, restrained blue/teal palette with high contrast white/light elements, no hairline strokes or tiny details. Square 1024x1024 transparent PNG, actual alpha transparency.
