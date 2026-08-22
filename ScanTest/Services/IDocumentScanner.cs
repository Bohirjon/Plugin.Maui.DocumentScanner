namespace ScanTest.Services;

public interface IDocumentScanner
{
    bool IsSupported { get; }
    Task<IReadOnlyList<string>> ScanAsync();   // returns file paths of cropped pages
}
