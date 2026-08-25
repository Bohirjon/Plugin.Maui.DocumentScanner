# Changelog

## 0.2.0 (unreleased)

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
