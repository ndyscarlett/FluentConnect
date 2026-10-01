using FluentConnect.Models;

namespace FluentConnect.Services;

public sealed class DeviceIdentificationService
{
    private static readonly DeviceVisualProfile Earbuds = new(
        "surface-earbuds",
        "Assets/Devices/SurfaceEarbuds_Buds.png",
        1.0,
        LeftAsset: "Assets/Devices/SurfaceEarbuds_Buds.png",
        RightAsset: "Assets/Devices/SurfaceEarbuds_Case.png",
        AnimationPreset: DeviceAnimationPreset.SplitEarbuds,
        LeftAssetScale: 0.92,
        RightAssetScale: 1.18);

    private static readonly DeviceVisualProfile Headphones2 = new(
        "surface-headphones-2",
        "Assets/Devices/SurfaceHeadphones2.png",
        0.92);

    private static readonly DeviceVisualProfile Headphones = new(
        "surface-headphones",
        "Assets/Devices/SurfaceHeadphones.png",
        0.92);

    private static readonly DeviceVisualProfile Generic = new(
        "generic-audio",
        "Assets/Devices/SurfaceEarbuds_Buds.png",
        1.0,
        LeftAsset: "Assets/Devices/SurfaceEarbuds_Buds.png",
        RightAsset: "Assets/Devices/SurfaceEarbuds_Case.png",
        AnimationPreset: DeviceAnimationPreset.SplitEarbuds,
        LeftAssetScale: 0.92,
        RightAssetScale: 1.18);

    public bool IsSurfaceAudioDevice(string name) =>
        name.Contains("Surface", StringComparison.OrdinalIgnoreCase);

    public DeviceVisualProfile GetVisualProfile(string name)
    {
        if (name.Contains("Earbuds", StringComparison.OrdinalIgnoreCase)) return Earbuds;
        if (name.Contains("Headphones 2", StringComparison.OrdinalIgnoreCase)) return Headphones2;
        if (name.Contains("Headphones", StringComparison.OrdinalIgnoreCase)) return Headphones;
        return Generic;
    }

    public string? ResolveExistingAsset(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return null;
        var fullPath = Path.Combine(AppContext.BaseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(fullPath) ? fullPath : null;
    }
}
