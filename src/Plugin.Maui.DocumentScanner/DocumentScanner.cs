namespace Plugin.Maui.DocumentScanner;

/// <summary>Entry point for consumers not using dependency injection.</summary>
public static class DocumentScanner
{
    static IDocumentScanner? defaultInstance;

    /// <summary>Shared scanner instance.</summary>
    public static IDocumentScanner Default => defaultInstance ??= new DocumentScannerImplementation();
}
