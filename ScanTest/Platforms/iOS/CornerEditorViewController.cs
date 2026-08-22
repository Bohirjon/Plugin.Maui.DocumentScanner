using CoreAnimation;
using CoreGraphics;
using Microsoft.Maui.ApplicationModel;
using UIKit;

namespace ScanTest.Services;

// Lets the user adjust the detected document corners before cropping
sealed class CornerEditorViewController : UIViewController
{
    const float HandleSize = 32;
    const float BarHeight = 72;

    readonly UIImage image;
    readonly CGPoint[] corners;   // normalized, bottom-left origin (Vision convention)
    readonly TaskCompletionSource<CGPoint[]?> tcs = new();
    readonly UIImageView imageView;
    readonly CAShapeLayer quadLayer = new();
    readonly UIView[] handles = new UIView[4];
    CGRect imageFrame;

    CornerEditorViewController(UIImage image, CGPoint[] corners)
    {
        this.image = image;
        this.corners = (CGPoint[])corners.Clone();
        imageView = new UIImageView(image) { ContentMode = UIViewContentMode.ScaleAspectFit };
        ModalPresentationStyle = UIModalPresentationStyle.FullScreen;
    }

    // Returns adjusted normalized corners, or null on cancel
    public static Task<CGPoint[]?> EditAsync(UIImage image, CGPoint[] detectedCorners) =>
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var host = await GetStableHostAsync();
            var editor = new CornerEditorViewController(image, detectedCorners);
            await host.PresentViewControllerAsync(editor, true);
            return await editor.tcs.Task;
        });

    // The picker is still animating its dismissal when the pick task completes
    static async Task<UIViewController> GetStableHostAsync()
    {
        for (var i = 0; i < 50; i++)
        {
            var host = Platform.GetCurrentUIViewController();
            if (host is not null && !host.IsBeingDismissed && host.View?.Window is not null)
                return host;
            await Task.Delay(100);
        }
        throw new InvalidOperationException("No view controller to present from.");
    }

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        View!.BackgroundColor = UIColor.Black;
        View.AddSubview(imageView);

        quadLayer.FillColor = UIColor.SystemBlue.ColorWithAlpha(0.2f).CGColor;
        quadLayer.StrokeColor = UIColor.SystemBlue.CGColor;
        quadLayer.LineWidth = 2;
        View.Layer.AddSublayer(quadLayer);

        for (var i = 0; i < handles.Length; i++)
        {
            var handle = new UIView(new CGRect(0, 0, HandleSize, HandleSize))
            {
                BackgroundColor = UIColor.White.ColorWithAlpha(0.9f),
            };
            handle.Layer.CornerRadius = HandleSize / 2;
            handle.Layer.BorderColor = UIColor.SystemBlue.CGColor;
            handle.Layer.BorderWidth = 2;
            handle.AddGestureRecognizer(new UIPanGestureRecognizer(OnPan));
            View.AddSubview(handle);
            handles[i] = handle;
        }

        View.AddSubview(MakeButton("Cancel", () => Finish(null)));
        View.AddSubview(MakeButton("Use", () => Finish((CGPoint[])corners.Clone())));
    }

    UIButton MakeButton(string title, Action onTap)
    {
        var button = new UIButton(UIButtonType.System);
        button.SetTitle(title, UIControlState.Normal);
        button.SetTitleColor(UIColor.White, UIControlState.Normal);
        button.TitleLabel!.Font = UIFont.BoldSystemFontOfSize(18)!;
        button.TouchUpInside += (_, _) => onTap();
        return button;
    }

    public override void ViewDidLayoutSubviews()
    {
        base.ViewDidLayoutSubviews();
        var safe = View!.SafeAreaInsets;
        var content = new CGRect(0, safe.Top, View.Bounds.Width, View.Bounds.Height - safe.Top - safe.Bottom - BarHeight);
        imageView.Frame = content;
        imageFrame = AspectFit(image.Size, content);

        var buttons = View.Subviews.OfType<UIButton>().ToArray();
        var barTop = View.Bounds.Height - safe.Bottom - BarHeight;
        buttons[0].Frame = new CGRect(0, barTop, View.Bounds.Width / 2, BarHeight);
        buttons[1].Frame = new CGRect(View.Bounds.Width / 2, barTop, View.Bounds.Width / 2, BarHeight);

        for (var i = 0; i < handles.Length; i++)
            handles[i].Center = ToView(corners[i]);
        UpdateQuad();
    }

    static CGRect AspectFit(CGSize size, CGRect bounds)
    {
        var scale = Math.Min(bounds.Width / size.Width, bounds.Height / size.Height);
        var w = size.Width * scale;
        var h = size.Height * scale;
        return new CGRect(bounds.X + (bounds.Width - w) / 2, bounds.Y + (bounds.Height - h) / 2, w, h);
    }

    // Vision is bottom-left origin, UIKit is top-left
    CGPoint ToView(CGPoint normalized) => new(
        imageFrame.X + normalized.X * imageFrame.Width,
        imageFrame.Y + (1 - normalized.Y) * imageFrame.Height);

    void OnPan(UIPanGestureRecognizer gesture)
    {
        var handle = gesture.View!;
        var translation = gesture.TranslationInView(View);
        gesture.SetTranslation(CGPoint.Empty, View);
        var center = new CGPoint(
            Math.Clamp(handle.Center.X + translation.X, imageFrame.Left, imageFrame.Right),
            Math.Clamp(handle.Center.Y + translation.Y, imageFrame.Top, imageFrame.Bottom));
        handle.Center = center;
        var i = Array.IndexOf(handles, handle);
        corners[i] = new CGPoint(
            (center.X - imageFrame.X) / imageFrame.Width,
            1 - (center.Y - imageFrame.Y) / imageFrame.Height);
        UpdateQuad();
    }

    void UpdateQuad()
    {
        var path = new UIBezierPath();
        path.MoveTo(handles[0].Center);
        for (var i = 1; i < handles.Length; i++)
            path.AddLineTo(handles[i].Center);
        path.ClosePath();
        quadLayer.Path = path.CGPath;
    }

    void Finish(CGPoint[]? result)
    {
        DismissViewController(true, () => tcs.TrySetResult(result));
    }
}
