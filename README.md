# FluentConnect

FluentConnect is a lightweight, native Windows 11 utility that displays a polished hardware animation when an audio device connects. Inspired by iOS Airpods connection animation.
<img width="800" height="450" alt="13-51-02-ezgif com-video-to-gif-converter" src="https://github.com/user-attachments/assets/0b49a93a-2da6-44a5-95df-ff125bdecb07" />

It is designed to feel like part of Windows and Microsoft Surface, using WinUI 3, the Windows App SDK, Fluent motion, per-monitor DPI awareness, system light/dark themes, and a non-activating overlay above the taskbar.

## Features

- Event-driven audio endpoint detection using Windows `DeviceWatcher`
- Surface audio-device filtering by default, with an optional **Any Audio Device** mode
- Physical-device deduplication so one Bluetooth connection produces one popup
- Centered, non-activating overlay that never steals keyboard focus
- Per-monitor DPI v2 positioning above the taskbar on the foreground monitor
- Restrained Fluent entrance, hardware, battery, and exit animations
- Separate animated Surface Earbuds and charging-case artwork
- Battery percentage and charging state when Windows exposes them
- Automatic light/dark theme support with Mica and Acrylic materials
- Tray controls for Settings, Test animation, and Exit
- Persistent preferences and optional per-user startup registration
- No periodic Bluetooth scans, aggressive polling, audio capture, or input collection

## Requirements

- Windows 11 x64
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) when using a framework-dependent build

## Setup

1. Download the latest FluentConnect archive from [GitHub Releases](https://github.com/ndyscarlett/FluentConnect/releases).
2. Extract the complete archive to a permanent folder.
3. Run `FluentConnect.exe` and configure the utility from its Settings window.

Keep the executable, DLL files, and `Assets` folder together. FluentConnect continues running from the notification area after Settings is closed.

# Advanced Instructions

## Build from source

Install the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0), then run:

```powershell
git clone https://github.com/ndyscarlett/FluentConnect.git
cd FluentConnect
dotnet restore FluentConnect.csproj
dotnet build FluentConnect.csproj -c Release -p:Platform=x64
dotnet run --project FluentConnect.csproj -c Release -p:Platform=x64
```

Run the automated popup smoke test with:

```powershell
dotnet run --project FluentConnect.csproj -c Release -p:Platform=x64 -- --test-popup --exit-after-test
```

## Project structure

```text
FluentConnect.csproj
Animation/       Popup motion orchestration
Assets/Devices/  Lazily loaded product artwork
Models/          Device, visual-profile, and settings models
Native/          Win32 window placement and monitor work-area integration
Services/        Audio watcher, battery, identification, settings, and tray services
UI/              WinUI popup and Settings windows
```

## How device detection works

FluentConnect watches Windows audio render endpoints rather than relying only on Bluetooth names. Existing endpoints discovered during startup are recorded silently, so launching the app does not replay connection animations for devices that were already present.

When an endpoint becomes available, FluentConnect identifies its Windows device container, applies the Surface or Any Audio Device filter, and suppresses duplicate endpoints from the same physical connection. Battery information is requested only for the connection being displayed and is omitted when Windows does not expose a reliable value.

## Privacy

FluentConnect observes audio endpoint availability, device names, Windows device-container identifiers, and battery properties exposed by Windows. It does not record audio, access microphones, inspect media, read typed text, use the clipboard, or collect browsing activity.

Settings are stored locally under `%LOCALAPPDATA%\FluentConnect`. The application does not include analytics or telemetry.

## Compatibility

Device naming, Bluetooth profiles, and battery reporting vary by hardware vendor and Windows driver. Surface audio devices are supported by default; other headphones, headsets, earbuds, and audio outputs can be enabled from Settings.

Positioning uses the foreground monitor's real work area and scales at 100%, 125%, 150%, and 200% DPI. Taskbar placement and auto-hide behavior are handled through the monitor work area reported by Windows.

## Contributing

Issues and pull requests are welcome. Please include the Windows build, audio-device model, connection type, and relevant reproduction steps when reporting device-detection problems.

## Future Updates

The visual-profile architecture supports model-specific artwork, scale, offsets, split assets, and animation presets. Future releases may add more Surface devices and vendor-specific battery integrations without changing the core popup.

## Legal

FluentConnect v1.0 © 2026 Andy Scarlett. All rights reserved.

Microsoft, Windows, Surface, and related marks are trademarks of the Microsoft group of companies. FluentConnect is an independent project and is not affiliated with, endorsed by, or sponsored by Microsoft Corporation.
