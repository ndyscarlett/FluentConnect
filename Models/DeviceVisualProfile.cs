namespace FluentConnect.Models;

public enum DeviceAnimationPreset
{
    Standard,
    SplitEarbuds
}

public sealed record DeviceVisualProfile(
    string Key,
    string? DisplayAsset,
    double AssetScale = 1.0,
    double OffsetX = 0,
    double OffsetY = 0,
    string? LeftAsset = null,
    string? RightAsset = null,
    DeviceAnimationPreset AnimationPreset = DeviceAnimationPreset.Standard);
