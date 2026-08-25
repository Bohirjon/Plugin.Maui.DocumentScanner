namespace Plugin.Maui.DocumentScanner;

/// <summary>Scans documents with the platform's native scanner UI.</summary>
public interface IDocumentScanner
{
    /// <summary>Whether the native scanner is available on this device.</summary>
    bool IsSupported { get; }

    /// <summary>Opens the camera scanner. Returns file paths of cropped pages; empty on user cancel.</summary>
    /// <exception cref="OperationCanceledException">The token was cancelled; the scanner UI is dismissed.</exception>
    Task<IReadOnlyList<string>> ScanAsync(DocumentScanOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Crops already-taken photos into document pages. Returns file paths; empty on user cancel.</summary>
    /// <exception cref="OperationCanceledException">The token was cancelled; any open UI is dismissed.</exception>
    Task<IReadOnlyList<string>> ScanFromPhotosAsync(DocumentScanOptions? options = null, CancellationToken cancellationToken = default);
}
