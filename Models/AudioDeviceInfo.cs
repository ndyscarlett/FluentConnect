namespace FluentConnect.Models;

public sealed record AudioDeviceInfo(
    string Id,
    string Name,
    string PhysicalKey,
    bool IsSurface,
    int? BatteryPercent = null,
    bool IsCharging = false);
