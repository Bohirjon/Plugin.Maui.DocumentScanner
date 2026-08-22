using Foundation;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using VisionKit;

namespace ScanTest.Services;

public partial class DocumentScanner
{
    public bool IsSupported => VNDocumentCameraViewController.Supported;

    public async Task<IReadOnlyList<string>> ScanAsync()
    {
        var host = Platform.GetCurrentUIViewController()
            ?? throw new InvalidOperationException("No view controller to present from.");

        var tcs = new TaskCompletionSource<IReadOnlyList<string>>();
        var camera = new VNDocumentCameraViewController
        {
            Delegate = new ScanDelegate(tcs),
        };
        await host.PresentViewControllerAsync(camera, true);
        return await tcs.Task;
    }

    sealed class ScanDelegate(TaskCompletionSource<IReadOnlyList<string>> tcs)
        : VNDocumentCameraViewControllerDelegate
    {
        public override void DidFinish(VNDocumentCameraViewController controller, VNDocumentCameraScan scan)
        {
            var paths = new List<string>();
            try
            {
                for (nuint i = 0; i < scan.PageCount; i++)
                {
                    using var image = scan.GetImage(i);
                    using var jpeg = image.AsJPEG(0.8f)
                        ?? throw new InvalidOperationException($"JPEG encode failed for page {i}.");
                    var path = Path.Combine(FileSystem.CacheDirectory, $"scan_{Guid.NewGuid():N}_{i}.jpg");
                    using var file = File.Create(path);
                    jpeg.AsStream().CopyTo(file);
                    paths.Add(path);
                }
            }
            catch (Exception ex)
            {
                controller.DismissViewController(true, null);
                tcs.TrySetException(ex);
                return;
            }
            controller.DismissViewController(true, null);
            tcs.TrySetResult(paths);
        }

        public override void DidCancel(VNDocumentCameraViewController controller)
        {
            controller.DismissViewController(true, null);
            tcs.TrySetResult([]);
        }

        public override void DidFail(VNDocumentCameraViewController controller, NSError error)
        {
            controller.DismissViewController(true, null);
            tcs.TrySetException(new InvalidOperationException(error.LocalizedDescription));
        }
    }
}
