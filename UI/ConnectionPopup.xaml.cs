using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Media;
using FluentConnect.Animation;
using FluentConnect.Models;
using FluentConnect.Native;
using FluentConnect.Services;

namespace FluentConnect.UI;

public sealed partial class ConnectionPopup : Window
{
    private readonly AudioDeviceInfo _device;
    private readonly ConnectionAnimationController _animation;
    private readonly CancellationTokenSource _lifetime = new();
    private bool _closing;

    public event EventHandler? Dismissed;

    public ConnectionPopup(AudioDeviceInfo device, DeviceIdentificationService identification)
    {
        InitializeComponent();
        _device = device;
        Title = device.Name;
        SystemBackdrop = new DesktopAcrylicBackdrop();
        ApplySystemTheme();

        DeviceNameText.Text = device.Name;
        ConfigureBattery(device);
        ConfigureArtwork(identification);

        _animation = new ConnectionAnimationController(
            PopupRoot, RootTransform,
            ArtworkHost, ArtworkTransform,
            LeftArtwork, LeftArtworkTransform,
            RightArtwork, RightArtworkTransform,
            DeviceNameText, TitleTransform,
            BatteryRow, BatteryTransform,
            Illumination, GlowTransform);

        WindowPlacement.ConfigurePopup(this, 420, 410);
        Closed += OnClosed;
    }

    public void ShowWithoutActivation()
    {
        WindowPlacement.GetAppWindow(this).Show(false);
        _animation.StartEntrance(_device.BatteryPercent.HasValue);
        _ = AutoDismissAsync();
    }

    public async Task DismissAsync(bool fast)
    {
        if (_closing) return;
        _closing = true;
        _lifetime.Cancel();
        await _animation.StartExitAsync(fast);
        Close();
    }

    private async Task AutoDismissAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(3200), _lifetime.Token);
            await DismissAsync(false);
        }
        catch (OperationCanceledException) { }
    }

    private void ConfigureBattery(AudioDeviceInfo device)
    {
        if (device.BatteryPercent is not int percent)
        {
            BatteryRow.Visibility = Visibility.Collapsed;
            return;
        }

        BatteryText.Text = $"{percent}%";
        BatteryIcon.Glyph = device.IsCharging
            ? "\uEC3D"
            : percent <= 15 ? "\uEBA0" : percent <= 45 ? "\uEBA4" : percent <= 75 ? "\uEBA7" : "\uEBAA";
    }

    private void ConfigureArtwork(DeviceIdentificationService identification)
    {
        var profile = identification.GetVisualProfile(_device.Name);
        var asset = identification.ResolveExistingAsset(profile.DisplayAsset);
        var leftAsset = identification.ResolveExistingAsset(profile.LeftAsset) ?? asset;
        var rightAsset = identification.ResolveExistingAsset(profile.RightAsset) ?? asset;
        LeftArtworkTransform.ScaleX = LeftArtworkTransform.ScaleY = profile.AssetScale * profile.LeftAssetScale;
        RightArtworkTransform.ScaleX = RightArtworkTransform.ScaleY = profile.AssetScale * profile.RightAssetScale;
        if (leftAsset is null || rightAsset is null)
        {
            LeftDeviceArtwork.Visibility = Visibility.Collapsed;
            RightDeviceArtwork.Visibility = Visibility.Collapsed;
            LeftFallbackArtwork.Visibility = Visibility.Visible;
            RightFallbackArtwork.Visibility = Visibility.Visible;
            return;
        }

        LeftDeviceArtwork.Source = new BitmapImage(new Uri(leftAsset));
        RightDeviceArtwork.Source = new BitmapImage(new Uri(rightAsset));
        LeftDeviceArtwork.Visibility = Visibility.Visible;
        RightDeviceArtwork.Visibility = Visibility.Visible;
        LeftFallbackArtwork.Visibility = Visibility.Collapsed;
        RightFallbackArtwork.Visibility = Visibility.Collapsed;
    }

    private void ApplySystemTheme()
    {
        var ui = new Windows.UI.ViewManagement.UISettings();
        var background = ui.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
        var luminance = (0.2126 * background.R + 0.7152 * background.G + 0.0722 * background.B) / 255d;
        PopupRoot.RequestedTheme = luminance < 0.5 ? ElementTheme.Dark : ElementTheme.Light;
    }

    private void PopupRoot_PointerPressed(object sender, PointerRoutedEventArgs e) => _ = DismissAsync(true);

    private void OnClosed(object sender, WindowEventArgs args)
    {
        _lifetime.Cancel();
        _lifetime.Dispose();
        _animation.Stop();
        LeftDeviceArtwork.Source = null;
        RightDeviceArtwork.Source = null;
        Dismissed?.Invoke(this, EventArgs.Empty);
    }
}
