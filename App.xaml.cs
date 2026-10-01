using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using FluentConnect.Models;
using FluentConnect.Services;
using FluentConnect.UI;

namespace FluentConnect;

public partial class App : Microsoft.UI.Xaml.Application
{
    private readonly SettingsService _settings = new();
    private readonly DeviceIdentificationService _identification = new();
    private readonly BatteryService _battery = new();
    private AudioDeviceWatcher? _watcher;
    private TrayIconService? _tray;
    private SettingsWindow? _settingsWindow;
    private ConnectionPopup? _popup;
    private Window? _keepAliveWindow;
    private DispatcherQueue? _dispatcher;
    private readonly string? _smokeLogPath = Environment.GetEnvironmentVariable("SCM_SMOKE_LOG");

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, e) => Log($"unhandled: {e.Exception}");
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        Log("launched");
        await _settings.LoadAsync();
        _keepAliveWindow = new Window { Title = "FluentConnect background host" };
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        _tray = new TrayIconService(ShowSettings, ShowTestAnimation, ExitApplication);

        _watcher = new AudioDeviceWatcher(_identification, _battery, () => _settings.Current);
        _watcher.AudioDeviceConnected += OnAudioDeviceConnected;
        _watcher.Start();

        var commandLine = Environment.GetCommandLineArgs();
        if (commandLine.Contains("--test-popup", StringComparer.OrdinalIgnoreCase))
        {
            ShowTestAnimation();
            Log("test-popup-created");
            if (commandLine.Contains("--exit-after-test", StringComparer.OrdinalIgnoreCase))
            {
                await Task.Delay(4500);
                Log("test-cycle-complete");
                ExitApplication();
            }
        }
        else if (!commandLine.Contains("--background", StringComparer.OrdinalIgnoreCase))
        {
            ShowSettings();
        }
    }

    private void OnAudioDeviceConnected(object? sender, AudioDeviceInfo device)
    {
        _dispatcher?.TryEnqueue(() => ShowPopup(device));
    }

    private void ShowSettings()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settings, ShowTestAnimation);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Activate();
    }

    private void ShowTestAnimation() => ShowPopup(new AudioDeviceInfo(
        "test-surface-earbuds",
        "Surface Earbuds",
        "test-surface-earbuds",
        true,
        86,
        false));

    private void ShowPopup(AudioDeviceInfo device)
    {
        if (_popup is not null) _ = _popup.DismissAsync(true);
        var popup = new ConnectionPopup(device, _identification);
        _popup = popup;
        popup.Dismissed += (_, _) =>
        {
            if (ReferenceEquals(_popup, popup)) _popup = null;
            Log("popup-dismissed");
        };
        popup.ShowWithoutActivation();
    }

    private void ExitApplication()
    {
        _watcher?.Dispose();
        _watcher = null;
        _tray?.Dispose();
        _tray = null;
        _popup?.Close();
        _settingsWindow?.Close();
        _keepAliveWindow?.Close();
        _keepAliveWindow = null;
        Exit();
        Environment.Exit(0);
    }

    private void Log(string message)
    {
        if (string.IsNullOrWhiteSpace(_smokeLogPath)) return;
        try { File.AppendAllText(_smokeLogPath, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}"); }
        catch { }
    }
}
