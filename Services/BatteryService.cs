using Windows.Devices.Enumeration;

namespace FluentConnect.Services;

public sealed record BatterySnapshot(int Percent, bool IsCharging);

public sealed class BatteryService
{
    public const string ContainerId = "System.Devices.ContainerId";
    public const string BluetoothLeBattery = "System.Devices.Aep.Bluetooth.Le.BatteryLevel";
    public const string BatteryLife = "System.Devices.BatteryLife";
    public const string Charging = "System.Devices.BatteryPlusCharging";
    // DEVPKEY_Device_BatteryLifePercent. Some Bluetooth drivers expose this only
    // on a sibling PnP device rather than on the audio endpoint itself.
    public const string DeviceBatteryLifePercent = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";

    public async Task<BatterySnapshot?> TryGetBatteryAsync(string deviceId, IReadOnlyDictionary<string, object>? knownProperties = null)
    {
        var fromKnown = Parse(knownProperties);
        if (fromKnown is not null) return fromKnown;

        foreach (var batteryProperty in new[] { BatteryLife, BluetoothLeBattery })
        {
            try
            {
                var info = await DeviceInformation.CreateFromIdAsync(deviceId, [batteryProperty]);
                var snapshot = Parse(info?.Properties);
                if (snapshot is not null)
                {
                    try
                    {
                        var chargingInfo = await DeviceInformation.CreateFromIdAsync(deviceId, [Charging]);
                        var isCharging = chargingInfo?.Properties.TryGetValue(Charging, out var value) == true &&
                                         value is not null && Convert.ToBoolean(value);
                        return snapshot with { IsCharging = isCharging };
                    }
                    catch { return snapshot; }
                }
            }
            catch
            {
                // Property availability differs between endpoint transports and Windows builds.
            }
        }

        var fromContainer = await TryGetBatteryFromContainerAsync(knownProperties);
        if (fromContainer is not null) return fromContainer;

        return null;
    }

    private static async Task<BatterySnapshot?> TryGetBatteryFromContainerAsync(
        IReadOnlyDictionary<string, object>? knownProperties)
    {
        if (knownProperties is null ||
            !knownProperties.TryGetValue(ContainerId, out var containerValue) ||
            containerValue is null ||
            !Guid.TryParse(containerValue.ToString(), out var containerId))
        {
            return null;
        }

        return await TryGetBatteryFromContainerAsync(containerId);
    }

    internal static async Task<BatterySnapshot?> TryGetBatteryFromContainerAsync(Guid containerId)
    {
        try
        {
            var selector = $"System.Devices.ContainerId:=\"{containerId:B}\"";
            var devices = await DeviceInformation.FindAllAsync(
                selector,
                [DeviceBatteryLifePercent],
                DeviceInformationKind.Device);

            foreach (var device in devices)
            {
                var snapshot = Parse(device.Properties);
                if (snapshot is not null) return snapshot;
            }
        }
        catch
        {
            // Battery support is optional and differs between Bluetooth drivers.
        }

        return null;
    }

    private static BatterySnapshot? Parse(IReadOnlyDictionary<string, object>? properties)
    {
        if (properties is null) return null;

        int? percent = null;
        foreach (var key in new[] { DeviceBatteryLifePercent, BluetoothLeBattery, BatteryLife })
        {
            if (!properties.TryGetValue(key, out var value) || value is null) continue;
            try
            {
                percent = Math.Clamp(Convert.ToInt32(value), 0, 100);
                break;
            }
            catch { }
        }

        if (percent is null) return null;
        var charging = properties.TryGetValue(Charging, out var chargingValue) &&
                       chargingValue is not null && Convert.ToBoolean(chargingValue);
        return new BatterySnapshot(percent.Value, charging);
    }
}
