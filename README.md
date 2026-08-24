# Plugin.Maui.DocumentScanner

Native document scanning for .NET MAUI — no paid SDK required.

- **Android**: [ML Kit document scanner](https://developers.google.com/ml-kit/vision/doc-scanner) (full scanner UI, auto-crop, filters; models downloaded via Google Play services)
- **iOS**: [VisionKit](https://developer.apple.com/documentation/visionkit) document camera for scanning, plus Vision document segmentation with a built-in corner editor for cropping already-taken photos

Supports Android API 23+ (with Google Play services) and iOS 15+.

## Setup

Install the package, then register it in `MauiProgram.cs`:

```csharp
builder
    .UseMauiApp<App>()
    .UseDocumentScanner();
```

`UseDocumentScanner()` registers `IDocumentScanner` in dependency injection and hooks the Android activity-result plumbing — no `MainActivity` changes needed.

## Usage

Inject `IDocumentScanner` (or use `DocumentScanner.Default` without DI):

```csharp
// Camera scan — returns file paths of cropped pages, empty list on cancel
IReadOnlyList<string> pages = await scanner.ScanAsync();

// Crop already-taken photos from the photo library
IReadOnlyList<string> pages = await scanner.ScanFromPhotosAsync();

// With options
var pages = await scanner.ScanAsync(new DocumentScanOptions
{
    PageLimit = 3,
    Mode = DocumentScannerMode.Base, // Android only: Full, BaseWithFilter, or Base
});
```

Check `scanner.IsSupported` first; `ScanAsync` throws `NotSupportedException` on devices without a scanner implementation.

Returned files are JPEGs written to the app's cache directory — move or copy them if you need them to persist.

### Platform notes

| | Android | iOS |
|---|---|---|
| `ScanAsync` | ML Kit scanner UI | VisionKit document camera |
| `ScanFromPhotosAsync` | ML Kit scanner with gallery import | Photo picker + auto-detected corners + manual corner editor |
| `PageLimit` | Applies to both methods | Photo import only (VisionKit has no limit) |
| `Mode` | Full / BaseWithFilter / Base | Ignored |

## Sample

The [`samples/ScanTest`](samples/ScanTest) app exercises both scan paths and shows page sizes and timings.
