using System.Runtime.Versioning;

namespace Plugin.Maui.DocumentScanner;

/// <summary>Scans documents with the platform's native scanner UI.</summary>
public interface IDocumentScanner
{
    /// <summary>Whether the native scanner is available on this device.</summary>
    bool IsSupported { get; }

    /// <summary>
    /// Opens the native camera scanner and returns file paths of the cropped pages;
    /// an empty list if the user cancels. Supported on Android and iOS.
    /// </summary>
    /// <remarks>
    /// Android: the ML Kit scanner. It always starts on the camera and offers an
    /// import-from-gallery button inside that UI.
    /// iOS: the VisionKit document camera.
    /// </remarks>
    /// <exception cref="OperationCanceledException">The token was cancelled; the scanner UI is dismissed.</exception>
    Task<IReadOnlyList<string>> ScanAsync(DocumentScanOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// iOS only. Opens the system photo picker for photos that were already taken, then a corner
    /// editor to adjust the crop of each one. Returns file paths of the cropped pages;
    /// an empty list if the user cancels.
    /// </summary>
    /// <remarks>
    /// There is no Android counterpart: ML Kit's scanner cannot start in the gallery, so calling
    /// this on Android always throws. Guard calls with <c>OperatingSystem.IsIOS()</c>. Android
    /// users reach the gallery through the import button inside <see cref="ScanAsync"/>'s scanner.
    /// </remarks>
    /// <exception cref="NotSupportedException">Called on Android.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled; any open UI is dismissed.</exception>
    [SupportedOSPlatform("ios")]
    Task<IReadOnlyList<string>> ScanFromPhotosAsync(DocumentScanOptions? options = null, CancellationToken cancellationToken = default);
}
