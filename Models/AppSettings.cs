namespace FluentConnect.Models;

public sealed class AppSettings
{
    public bool AnyAudioDevice { get; set; }
    public bool ShowConnectionAnimation { get; set; } = true;
    public bool ShowBatteryLevel { get; set; } = true;
    public bool RunAtStartup { get; set; } = true;
}
