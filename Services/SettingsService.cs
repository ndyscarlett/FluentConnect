using System.Text.Json;
using Microsoft.Win32;
using FluentConnect.Models;

namespace FluentConnect.Services;

public sealed class SettingsService
{
    private const string StartupKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupValue = "FluentConnect";
    private readonly string _settingsPath;

    public AppSettings Current { get; private set; } = new();

    public SettingsService()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentConnect");
        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "settings.json");
    }

    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                await using var stream = File.OpenRead(_settingsPath);
                Current = await JsonSerializer.DeserializeAsync<AppSettings>(stream) ?? new AppSettings();
            }
        }
        catch
        {
            Current = new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings, bool updateStartupRegistration = true)
    {
        Current = settings;
        await using (var stream = File.Create(_settingsPath))
        {
            await JsonSerializer.SerializeAsync(stream, Current, new JsonSerializerOptions { WriteIndented = true });
        }

        if (updateStartupRegistration)
        {
            SetStartupRegistration(Current.RunAtStartup);
        }
    }

    private static void SetStartupRegistration(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupKey, writable: true);
        if (key is null) return;

        if (enabled)
        {
            var executable = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(executable))
            {
                key.SetValue(StartupValue, $"\"{executable}\" --background");
            }
        }
        else
        {
            key.DeleteValue(StartupValue, throwOnMissingValue: false);
        }
    }
}
