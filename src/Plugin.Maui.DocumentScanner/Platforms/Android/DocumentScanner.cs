using Android.Content;
using Android.Gms.Common;
using Android.Gms.Extensions;
using Google.MLKit.Vision.Documentscanner;
using Xamarin.Google.MLKit.Common;

namespace Plugin.Maui.DocumentScanner;

sealed class DocumentScannerImplementation : IDocumentScanner
{
    const int ScanRequestCode = 4711;

    static TaskCompletionSource<IReadOnlyList<string>>? pending;
    static bool unsupportedReported;

    public bool IsSupported =>
        !unsupportedReported
        && GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(Platform.AppContext) == ConnectionResult.Success;

    public Task<IReadOnlyList<string>> ScanAsync(DocumentScanOptions? options = null) =>
        LaunchAsync(galleryImport: false, options ?? DocumentScanOptions.Default);

    // Same scanner UI plus an import-from-gallery button
    public Task<IReadOnlyList<string>> ScanFromPhotosAsync(DocumentScanOptions? options = null) =>
        LaunchAsync(galleryImport: true, options ?? DocumentScanOptions.Default);

    static async Task<IReadOnlyList<string>> LaunchAsync(bool galleryImport, DocumentScanOptions options)
    {
        var activity = Platform.CurrentActivity
            ?? throw new InvalidOperationException("No current activity.");

        var scannerOptions = new GmsDocumentScannerOptions.Builder()
            .SetPageLimit(options.PageLimit)
            .SetGalleryImportAllowed(galleryImport)
            .SetScannerMode(ToScannerMode(options.Mode))
            .SetResultFormats(GmsDocumentScannerOptions.ResultFormatJpeg, [])
            .Build();

        var scanner = GmsDocumentScanning.GetClient(scannerOptions);
        pending?.TrySetCanceled();
        var tcs = pending = new TaskCompletionSource<IReadOnlyList<string>>();
        try
        {
            var sender = await scanner.GetStartScanIntent(activity).AsAsync<IntentSender>();
            activity.StartIntentSenderForResult(sender, ScanRequestCode, null, 0, 0, 0);
        }
        catch (MlKitException ex) when (ex.ErrorCode == MlKitException.Unsupported)
        {
            unsupportedReported = true;
            pending = null;
            throw new NotSupportedException("Device does not support the ML Kit document scanner.", ex);
        }
        catch
        {
            pending = null;
            throw;
        }
        return await tcs.Task;
    }

    static int ToScannerMode(DocumentScannerMode mode) => mode switch
    {
        DocumentScannerMode.Base => GmsDocumentScannerOptions.ScannerModeBase,
        DocumentScannerMode.BaseWithFilter => GmsDocumentScannerOptions.ScannerModeBaseWithFilter,
        _ => GmsDocumentScannerOptions.ScannerModeFull,
    };

    // Wired to the activity-result lifecycle event by UseDocumentScanner
    internal static void HandleActivityResult(int requestCode, Android.App.Result resultCode, Intent? data)
    {
        if (requestCode != ScanRequestCode || pending is null)
            return;

        var tcs = pending;
        pending = null;
        try
        {
            if (resultCode != Android.App.Result.Ok || data is null)
            {
                tcs.TrySetResult([]);
                return;
            }

            var result = GmsDocumentScanningResult.FromActivityResultIntent(data);
            var paths = new List<string>();
            foreach (var page in result?.Pages ?? [])
            {
                if (page?.ImageUri is null)
                    continue;
                using var input = Platform.AppContext.ContentResolver!.OpenInputStream(page.ImageUri)
                    ?? throw new InvalidOperationException($"Cannot open {page.ImageUri}.");
                var path = Path.Combine(FileSystem.CacheDirectory, $"scan_{Guid.NewGuid():N}.jpg");
                using var file = File.Create(path);
                input.CopyTo(file);
                paths.Add(path);
            }
            tcs.TrySetResult(paths);
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }
    }
}
