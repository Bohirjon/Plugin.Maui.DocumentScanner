namespace Plugin.Maui.DocumentScanner;

/// <summary>Per-call scan settings.</summary>
public class DocumentScanOptions
{
    internal static readonly DocumentScanOptions Default = new();

    /// <summary>Maximum pages per scan. On iOS applies to photo import only; the camera scanner has no limit.</summary>
    public int PageLimit { get; set; } = 5;

    /// <summary>Scanner UI feature set. Android only; iOS ignores it.</summary>
    public DocumentScannerMode Mode { get; set; } = DocumentScannerMode.Full;
}

/// <summary>ML Kit scanner feature sets.</summary>
public enum DocumentScannerMode
{
    /// <summary>Cropping, filters, and cleaning controls.</summary>
    Full,

    /// <summary>Cropping and filters.</summary>
    BaseWithFilter,

    /// <summary>Cropping only.</summary>
    Base,
}
