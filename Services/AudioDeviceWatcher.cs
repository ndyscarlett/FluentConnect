using FluentConnect.Models;
using Windows.Devices.Enumeration;
using Windows.Media.Devices;

namespace FluentConnect.Services;

public sealed class AudioDeviceWatcher : IDisposable
{
    private const string IsEnabled = "System.Devices.InterfaceEnabled";
    private const string IsConnected = "System.Devices.Aep.IsConnected";
    private const string ContainerId = "System.Devices.ContainerId";
    private static readonly string[] RequestedProperties =
    [
        IsEnabled,
        IsConnected,
        ContainerId
    ];

    private readonly DeviceIdentificationService _identification;
    private readonly BatteryService _battery;
    private readonly Func<AppSettings> _settings;
    private readonly object _gate = new();
    private readonly Dictionary<string, bool> _enabledByEndpoint = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTimeOffset> _recentPhysicalConnections = new(StringComparer.OrdinalIgnoreCase);
    private DeviceWatcher? _watcher;
    private bool _initialEnumerationComplete;

    public event EventHandler<AudioDeviceInfo>? AudioDeviceConnected;

    public AudioDeviceWatcher(DeviceIdentificationService identification, BatteryService battery, Func<AppSettings> settings)
    {
        _identification = identification;
        _battery = battery;
        _settings = settings;
    }

    public void Start()
    {
        if (_watcher is not null) return;
        _watcher = DeviceInformation.CreateWatcher(
            MediaDevice.GetAudioRenderSelector(),
            RequestedProperties,
            DeviceInformationKind.DeviceInterface);
        _watcher.Added += OnAdded;
        _watcher.Updated += OnUpdated;
        _watcher.Removed += OnRemoved;
        _watcher.EnumerationCompleted += OnEnumerationCompleted;
        _watcher.Start();
    }

    private void OnAdded(DeviceWatcher sender, DeviceInformation info)
    {
        var enabled = IsAvailable(info.Properties);
        lock (_gate) _enabledByEndpoint[info.Id] = enabled;
        if (_initialEnumerationComplete && enabled) _ = ProcessConnectionAsync(info.Id, info.Name, info.Properties);
    }

    private void OnUpdated(DeviceWatcher sender, DeviceInformationUpdate update)
    {
        bool wasEnabled;
        bool nowEnabled;
        lock (_gate)
        {
            _enabledByEndpoint.TryGetValue(update.Id, out wasEnabled);
            nowEnabled = update.Properties.Count == 0 ? wasEnabled : IsAvailable(update.Properties, wasEnabled);
            _enabledByEndpoint[update.Id] = nowEnabled;
        }

        if (_initialEnumerationComplete && !wasEnabled && nowEnabled)
        {
            _ = RefreshAndProcessAsync(update.Id);
        }
    }

    private void OnRemoved(DeviceWatcher sender, DeviceInformationUpdate update)
    {
        lock (_gate) _enabledByEndpoint.Remove(update.Id);
    }

    private void OnEnumerationCompleted(DeviceWatcher sender, object args) => _initialEnumerationComplete = true;

    private async Task RefreshAndProcessAsync(string id)
    {
        try
        {
            var info = await DeviceInformation.CreateFromIdAsync(id, RequestedProperties, DeviceInformationKind.DeviceInterface);
            if (info is not null) await ProcessConnectionAsync(info.Id, info.Name, info.Properties);
        }
        catch { }
    }

    private async Task ProcessConnectionAsync(string id, string name, IReadOnlyDictionary<string, object> properties)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        var settings = _settings();
        var isSurface = _identification.IsSurfaceAudioDevice(name);
        if (!settings.ShowConnectionAnimation || (!settings.AnyAudioDevice && !isSurface)) return;

        var physicalKey = GetPhysicalKey(id, properties);
        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var stale in _recentPhysicalConnections.Where(x => now - x.Value > TimeSpan.FromMinutes(1)).Select(x => x.Key).ToArray())
                _recentPhysicalConnections.Remove(stale);
            if (_recentPhysicalConnections.TryGetValue(physicalKey, out var seen) && now - seen < TimeSpan.FromSeconds(8)) return;
            _recentPhysicalConnections[physicalKey] = now;
        }

        BatterySnapshot? battery = null;
        if (settings.ShowBatteryLevel)
            battery = await _battery.TryGetBatteryAsync(id, properties);

        AudioDeviceConnected?.Invoke(this, new AudioDeviceInfo(id, CleanName(name), physicalKey, isSurface, battery?.Percent, battery?.IsCharging ?? false));
    }

    private static bool IsAvailable(IReadOnlyDictionary<string, object> properties, bool fallback = true)
    {
        if (properties.TryGetValue(IsConnected, out var connected) && connected is bool connectedBool) return connectedBool;
        if (properties.TryGetValue(IsEnabled, out var enabled) && enabled is bool enabledBool) return enabledBool;
        return fallback;
    }

    private static string GetPhysicalKey(string id, IReadOnlyDictionary<string, object> properties)
    {
        if (properties.TryGetValue(ContainerId, out var container) && container is not null)
        {
            var value = container.ToString();
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

        var parts = id.Split('#');
        return parts.Length >= 3 ? string.Join('#', parts.Take(3)) : id;
    }

    private static string CleanName(string name)
    {
        foreach (var suffix in new[] { " (Stereo)", " (Hands-Free AG Audio)", " Hands-Free AG Audio", " Stereo" })
        {
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                name = name[..^suffix.Length].Trim();
                break;
            }
        }

        // Windows localizes endpoint role names, for example
        // "Headphones (realme Buds Air6 Pro)" or "Наушники (realme Buds Air6 Pro)".
        // The text in parentheses is the stable device-facing name.
        var openParenthesis = name.IndexOf('(');
        if (openParenthesis > 0 && name.EndsWith(')'))
        {
            var deviceName = name[(openParenthesis + 1)..^1].Trim();
            if (!string.IsNullOrWhiteSpace(deviceName)) return deviceName;
        }

        return name.Trim();
    }

    public void Dispose()
    {
        if (_watcher is null) return;
        _watcher.Added -= OnAdded;
        _watcher.Updated -= OnUpdated;
        _watcher.Removed -= OnRemoved;
        _watcher.EnumerationCompleted -= OnEnumerationCompleted;
        if (_watcher.Status is DeviceWatcherStatus.Started or DeviceWatcherStatus.EnumerationCompleted)
            _watcher.Stop();
        _watcher = null;
    }
}
