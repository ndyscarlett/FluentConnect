using Windows.Devices.Enumeration;

namespace FluentConnect.Services;

public sealed record BatterySnapshot(int Percent, bool IsCharging);

public sealed class BatteryService
{
    public const string BluetoothLeBattery = "System.Devices.Aep.Bluetooth.Le.BatteryLevel";
    public const string BatteryLife = "System.Devices.BatteryLife";
    public const string Charging = "System.Devices.BatteryPlusCharging";

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
        return null;
    }

    private static BatterySnapshot? Parse(IReadOnlyDictionary<string, object>? properties)
    {
        if (properties is null) return null;

        int? percent = null;
        foreach (var key in new[] { BluetoothLeBattery, BatteryLife })
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
