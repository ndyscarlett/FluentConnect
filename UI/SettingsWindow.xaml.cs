using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using FluentConnect.Models;
using FluentConnect.Native;
using FluentConnect.Services;

namespace FluentConnect.UI;

public sealed partial class SettingsWindow : Window
{
    private readonly SettingsService _settings;
    private readonly Action _showTest;

    public SettingsWindow(SettingsService settings, Action showTest)
    {
        InitializeComponent();
        _settings = settings;
        _showTest = showTest;
        Title = "FluentConnect";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBar);
        SystemBackdrop = new MicaBackdrop();
        ApplySystemTheme();
        WindowPlacement.ConfigureSettings(this);
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "FluentConnect.ico");
        if (File.Exists(iconPath)) WindowPlacement.GetAppWindow(this).SetIcon(iconPath);

        AnyAudioDeviceCheckBox.IsChecked = settings.Current.AnyAudioDevice;
        ShowAnimationCheckBox.IsChecked = settings.Current.ShowConnectionAnimation;
        ShowBatteryCheckBox.IsChecked = settings.Current.ShowBatteryLevel;
        RunAtStartupCheckBox.IsChecked = settings.Current.RunAtStartup;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var updated = new AppSettings
        {
            AnyAudioDevice = AnyAudioDeviceCheckBox.IsChecked == true,
            ShowConnectionAnimation = ShowAnimationCheckBox.IsChecked == true,
            ShowBatteryLevel = ShowBatteryCheckBox.IsChecked == true,
            RunAtStartup = RunAtStartupCheckBox.IsChecked == true
        };
        await _settings.SaveAsync(updated);
        if (sender is Button button)
        {
            var original = button.Content;
            button.Content = "Saved";
            await Task.Delay(900);
            button.Content = original;
        }
    }

    private void TestAnimation_Click(object sender, RoutedEventArgs e) => _showTest();

    private void CheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/ndyscarlett/FluentConnect/releases",
            UseShellExecute = true
        });
    }

    private void ApplySystemTheme()
    {
        var ui = new Windows.UI.ViewManagement.UISettings();
        var background = ui.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
        var luminance = (0.2126 * background.R + 0.7152 * background.G + 0.0722 * background.B) / 255d;
        Root.RequestedTheme = luminance < 0.5 ? ElementTheme.Dark : ElementTheme.Light;
    }
}
