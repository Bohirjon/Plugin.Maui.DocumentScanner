#if ANDROID
using Microsoft.Maui.LifecycleEvents;
#endif

namespace Plugin.Maui.DocumentScanner;

/// <summary>MauiAppBuilder setup for the document scanner.</summary>
public static class AppBuilderExtensions
{
    /// <summary>Registers IDocumentScanner and the Android activity-result hook.</summary>
    public static MauiAppBuilder UseDocumentScanner(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton(DocumentScanner.Default);
#if ANDROID
        builder.ConfigureLifecycleEvents(events =>
            events.AddAndroid(android =>
                android.OnActivityResult((_, requestCode, resultCode, data) =>
                    DocumentScannerImplementation.HandleActivityResult(requestCode, resultCode, data))));
#endif
        return builder;
    }
}
