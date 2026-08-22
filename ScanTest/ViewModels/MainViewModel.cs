using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ScanTest.Services;

namespace ScanTest.ViewModels;

public sealed record ScannedPage(string Path, string Info);

public sealed class MainViewModel : INotifyPropertyChanged
{
    readonly IDocumentScanner scanner;
    string status = "Ready.";
    bool busy;

    public MainViewModel(IDocumentScanner scanner)
    {
        this.scanner = scanner;
        ScanCommand = new Command(async () => await ScanAsync(), () => !busy);
    }

    public ObservableCollection<ScannedPage> Pages { get; } = [];
    public Command ScanCommand { get; }

    public string Status
    {
        get => status;
        set { status = value; OnPropertyChanged(); }
    }

    async Task ScanAsync()
    {
        if (!scanner.IsSupported)
        {
            Status = "Scanner not supported on this device.";
            return;
        }

        busy = true;
        ScanCommand.ChangeCanExecute();
        try
        {
            // Wall-clock includes first-run module download on Android
            var stopwatch = Stopwatch.StartNew();
            var paths = await scanner.ScanAsync();
            stopwatch.Stop();

            Pages.Clear();
            long totalBytes = 0;
            foreach (var path in paths)
            {
                var bytes = new FileInfo(path).Length;
                totalBytes += bytes;
                Pages.Add(new ScannedPage(path, $"{bytes / 1024.0:F0} KB — {Path.GetFileName(path)}"));
            }
            Status = paths.Count == 0
                ? "Cancelled — no pages returned."
                : $"{paths.Count} page(s), {totalBytes / 1024.0:F0} KB total, {stopwatch.Elapsed.TotalSeconds:F1}s";
        }
        catch (Exception ex)
        {
            Status = $"{ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            busy = false;
            ScanCommand.ChangeCanExecute();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
