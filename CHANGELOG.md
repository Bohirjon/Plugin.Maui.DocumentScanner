# Changelog

## 0.2.1 — 2026-09-10

### Fixed
- Android `IsSupported` returned `true` on devices where the scanner cannot run: it only checked that Google Play services was present, but the scanner needs Play services 23.39 or newer. It now checks that version too, so the scan control is hidden instead of failing on tap.
- Android `ScanAsync` surfaced the raw Play services error ("Feature not available in the current version of the Google Play services") instead of `NotSupportedException`. That path throws ML Kit's `UNAVAILABLE` (14), which was not caught; it is now, alongside `UNSUPPORTED` (18) for low-RAM devices.

## 0.2.0 — 2026-09-07

### Changed
- **Breaking:** `ScanFromPhotosAsync` is now iOS-only (`[SupportedOSPlatform("ios")]`) and throws `NotSupportedException` on Android — ML Kit cannot start in the gallery, so the Android flow just opened the camera and confused users; Android users reach the gallery through the import button inside `ScanAsync`'s scanner, now always enabled

### Added
- `CancellationToken` parameter on `ScanAsync` and `ScanFromPhotosAsync`; cancelling dismisses the native scanner UI and throws `OperationCanceledException`
- Source Link and symbol package (`.snupkg`) so consumers can step into the library
- Package icon

## 0.1.0 — 2026-08-24

Initial release.

- Android: ML Kit document scanner (camera and gallery import)
- iOS: VisionKit document camera; photo import with Vision document detection and a manual corner editor
- `DocumentScanOptions` with `PageLimit` and `Mode`
- `UseDocumentScanner()` builder extension registering DI and Android activity-result plumbing
