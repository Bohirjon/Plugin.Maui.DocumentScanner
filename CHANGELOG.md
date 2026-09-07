# Changelog

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
