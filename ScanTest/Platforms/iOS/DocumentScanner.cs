using CoreGraphics;
using CoreImage;
using Foundation;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using UIKit;
using Vision;
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

    public async Task<IReadOnlyList<string>> ScanFromPhotosAsync()
    {
        // Matches the Android page limit
        var photos = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions { SelectionLimit = 5 });
        if (photos is null || photos.Count == 0)
            return [];

        var paths = new List<string>();
        foreach (var photo in photos)
        {
            // PHPicker results are only readable through the stream API, not FullPath
            using var stream = await photo.OpenReadAsync();
            using var data = NSData.FromStream(stream)
                ?? throw new InvalidOperationException("Could not read the selected photo.");
            using var loaded = UIImage.LoadFromData(data)
                ?? throw new InvalidOperationException("Could not load the selected photo.");
            using var image = NormalizeOrientation(loaded);

            var quad = await Task.Run(() => DetectQuad(image));
            var corners = await CornerEditorViewController.EditAsync(image, quad);
            if (corners is null)
                continue;
            paths.Add(await Task.Run(() => SaveCorrected(image, corners)));
        }
        return paths;
    }

    // Returns normalized TL/TR/BR/BL, or a default quad so the user can place corners manually
    static CGPoint[] DetectQuad(UIImage image)
    {
        var cgImage = image.CGImage
            ?? throw new InvalidOperationException("Photo has no bitmap data.");
        using var request = new VNDetectDocumentSegmentationRequest();
        using var handler = new VNImageRequestHandler(cgImage, new NSDictionary());
        if (handler.Perform([request], out _) && request.Results?.FirstOrDefault() is { } document)
            return [document.TopLeft, document.TopRight, document.BottomRight, document.BottomLeft];
        return [new(0.1, 0.9), new(0.9, 0.9), new(0.9, 0.1), new(0.1, 0.1)];
    }

    static string SaveCorrected(UIImage image, CGPoint[] corners)
    {
        var cgImage = image.CGImage
            ?? throw new InvalidOperationException("Photo has no bitmap data.");
        using var input = CIImage.FromCGImage(cgImage);
        var extent = input.Extent;
        using var filter = new CIPerspectiveCorrection
        {
            InputImage = input,
            InputTopLeft = Scale(corners[0], extent),
            InputTopRight = Scale(corners[1], extent),
            InputBottomRight = Scale(corners[2], extent),
            InputBottomLeft = Scale(corners[3], extent),
        };
        using var output = filter.OutputImage
            ?? throw new InvalidOperationException("Perspective correction failed.");
        using var context = CIContext.FromOptions(null);
        using var corrected = context.CreateCGImage(output, output.Extent)
            ?? throw new InvalidOperationException("Could not render corrected image.");
        using var result = UIImage.FromImage(corrected);
        using var jpeg = result.AsJPEG(0.8f)
            ?? throw new InvalidOperationException("JPEG encode failed.");

        var path = Path.Combine(FileSystem.CacheDirectory, $"import_{Guid.NewGuid():N}.jpg");
        using var file = File.Create(path);
        jpeg.AsStream().CopyTo(file);
        return path;
    }

    // Vision and CIImage share a normalized bottom-left origin
    static CGPoint Scale(CGPoint p, CGRect extent) =>
        new(extent.X + p.X * extent.Width, extent.Y + p.Y * extent.Height);

    // Bakes EXIF orientation into the bitmap so CGImage matches what the user sees
    static UIImage NormalizeOrientation(UIImage image)
    {
        UIGraphics.BeginImageContextWithOptions(image.Size, false, image.CurrentScale);
        image.Draw(new CGRect(CGPoint.Empty, image.Size));
        var normalized = UIGraphics.GetImageFromCurrentImageContext();
        UIGraphics.EndImageContext();
        return normalized ?? throw new InvalidOperationException("Could not normalize photo orientation.");
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
