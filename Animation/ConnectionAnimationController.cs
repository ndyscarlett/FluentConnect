using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Composition;
using System.Numerics;

namespace FluentConnect.Animation;

public sealed class ConnectionAnimationController
{
    private readonly FrameworkElement _root;
    private readonly CompositeTransform _rootTransform;
    private readonly FrameworkElement _artwork;
    private readonly CompositeTransform _artworkTransform;
    private readonly FrameworkElement _leftArtwork;
    private readonly CompositeTransform _leftArtworkTransform;
    private readonly FrameworkElement _rightArtwork;
    private readonly CompositeTransform _rightArtworkTransform;
    private readonly FrameworkElement _title;
    private readonly TranslateTransform _titleTransform;
    private readonly FrameworkElement _battery;
    private readonly TranslateTransform _batteryTransform;
    private readonly FrameworkElement _glow;
    private readonly ScaleTransform _glowTransform;
    private readonly Visual _rootVisual;
    private Storyboard? _active;
    private const float SlideDistance = 110f;

    public ConnectionAnimationController(
        FrameworkElement root, CompositeTransform rootTransform,
        FrameworkElement artwork, CompositeTransform artworkTransform,
        FrameworkElement leftArtwork, CompositeTransform leftArtworkTransform,
        FrameworkElement rightArtwork, CompositeTransform rightArtworkTransform,
        FrameworkElement title, TranslateTransform titleTransform,
        FrameworkElement battery, TranslateTransform batteryTransform,
        FrameworkElement glow, ScaleTransform glowTransform)
    {
        _root = root;
        _rootTransform = rootTransform;
        _rootVisual = ElementCompositionPreview.GetElementVisual(root);
        _artwork = artwork;
        _artworkTransform = artworkTransform;
        _leftArtwork = leftArtwork;
        _leftArtworkTransform = leftArtworkTransform;
        _rightArtwork = rightArtwork;
        _rightArtworkTransform = rightArtworkTransform;
        _title = title;
        _titleTransform = titleTransform;
        _battery = battery;
        _batteryTransform = batteryTransform;
        _glow = glow;
        _glowTransform = glowTransform;
    }

    public void StartEntrance(bool showBattery)
    {
        Stop();
        _root.Opacity = 1;
        _rootTransform.TranslateY = 0;
        _rootTransform.ScaleX = _rootTransform.ScaleY = 1;
        _artwork.Opacity = 0;
        _artworkTransform.TranslateY = 7;
        _artworkTransform.ScaleX = _artworkTransform.ScaleY = 0.9;
        _leftArtwork.Opacity = 0;
        _leftArtworkTransform.TranslateX = 8;
        _leftArtworkTransform.TranslateY = 7;
        _leftArtworkTransform.Rotation = 2;
        _rightArtwork.Opacity = 0;
        _rightArtworkTransform.TranslateX = -8;
        _rightArtworkTransform.TranslateY = 6;
        _rightArtworkTransform.Rotation = -2;
        _title.Opacity = 0;
        _titleTransform.Y = 4;
        _battery.Opacity = 0;
        _batteryTransform.Y = 3;
        _glow.Opacity = 0;
        _glowTransform.ScaleX = _glowTransform.ScaleY = 0.75;

        var storyboard = new Storyboard();
        AnimateSurface(SlideDistance, 0, 0, 1, 320);

        Add(storyboard, _artwork, "Opacity", 0, 1, 100, 230);
        Add(storyboard, _artworkTransform, "TranslateY", 7, 0, 100, 260);
        AddKeyFrames(storyboard, _artworkTransform, "ScaleX", (330, 1.02), (410, 1));
        AddKeyFrames(storyboard, _artworkTransform, "ScaleY", (330, 1.02), (410, 1));

        Add(storyboard, _leftArtwork, "Opacity", 0, 1, 105, 250);
        Add(storyboard, _leftArtworkTransform, "TranslateX", 8, 0, 105, 300);
        Add(storyboard, _leftArtworkTransform, "TranslateY", 7, 0, 105, 300);
        Add(storyboard, _leftArtworkTransform, "Rotation", 2, 0, 105, 300);
        Add(storyboard, _rightArtwork, "Opacity", 0, 1, 125, 250);
        Add(storyboard, _rightArtworkTransform, "TranslateX", -8, 0, 125, 300);
        Add(storyboard, _rightArtworkTransform, "TranslateY", 6, 0, 125, 300);
        Add(storyboard, _rightArtworkTransform, "Rotation", -2, 0, 125, 300);

        Add(storyboard, _title, "Opacity", 0, 1, 220, 180);
        Add(storyboard, _titleTransform, "Y", 4, 0, 220, 180);
        if (showBattery)
        {
            Add(storyboard, _battery, "Opacity", 0, 1, 310, 170);
            Add(storyboard, _batteryTransform, "Y", 3, 0, 310, 170);
        }

        AddKeyFrames(storyboard, _glow, "Opacity", (90, 0), (240, 0.08), (440, 0));
        Add(storyboard, _glowTransform, "ScaleX", 0.75, 1.15, 90, 350);
        Add(storyboard, _glowTransform, "ScaleY", 0.75, 1.15, 90, 350);
        _active = storyboard;
        storyboard.Begin();
    }

    public Task StartExitAsync(bool fast)
    {
        _active?.Stop();
        _active = null;
        var duration = fast ? 140 : 200;
        return AnimateSurface(0, SlideDistance, 1, 0, duration);
    }

    public void Stop()
    {
        _active?.Stop();
        _active = null;
        _rootVisual.StopAnimation("Offset");
        _rootVisual.StopAnimation("Opacity");
    }

    private Task AnimateSurface(float fromY, float toY, float fromOpacity, float toOpacity, int durationMs)
    {
        var compositor = _rootVisual.Compositor;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        var easing = toY > fromY
            ? compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.40f, 0.0f), new Vector2(1.0f, 1.0f))
            : compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.20f, 0.0f), new Vector2(0.15f, 1.0f));

        var offset = compositor.CreateVector3KeyFrameAnimation();
        offset.InsertKeyFrame(0, new Vector3(0, fromY, 0));
        offset.InsertKeyFrame(1, new Vector3(0, toY, 0), easing);
        offset.Duration = TimeSpan.FromMilliseconds(durationMs);
        offset.StopBehavior = AnimationStopBehavior.SetToFinalValue;

        var opacity = compositor.CreateScalarKeyFrameAnimation();
        opacity.InsertKeyFrame(0, fromOpacity);
        opacity.InsertKeyFrame(1, toOpacity, easing);
        opacity.Duration = TimeSpan.FromMilliseconds(durationMs);
        opacity.StopBehavior = AnimationStopBehavior.SetToFinalValue;

        _rootVisual.StartAnimation("Offset", offset);
        _rootVisual.StartAnimation("Opacity", opacity);
        batch.Completed += (_, _) => completion.TrySetResult();
        batch.End();
        return completion.Task;
    }

    private static void Add(Storyboard storyboard, DependencyObject target, string property,
        double from, double to, int beginMs, int durationMs)
    {
        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            BeginTime = TimeSpan.FromMilliseconds(beginMs),
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, property);
        storyboard.Children.Add(animation);
    }

    private static void AddKeyFrames(Storyboard storyboard, DependencyObject target, string property,
        params (int TimeMs, double Value)[] frames)
    {
        var animation = new DoubleAnimationUsingKeyFrames { EnableDependentAnimation = true };
        foreach (var frame in frames)
        {
            animation.KeyFrames.Add(new EasingDoubleKeyFrame
            {
                KeyTime = TimeSpan.FromMilliseconds(frame.TimeMs),
                Value = frame.Value,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            });
        }
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, property);
        storyboard.Children.Add(animation);
    }
}
