using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace FluentConnect.Services;

public sealed class TrayIconService : IDisposable
{
    private const int CallbackMessage = 0x8000 + 42;
    private const int WmRButtonUp = 0x0205;
    private const int WmLButtonDblClk = 0x0203;
    private const uint NimAdd = 0;
    private const uint NimDelete = 2;
    private const uint NifMessage = 1;
    private const uint NifIcon = 2;
    private const uint NifTip = 4;
    private const uint MfString = 0;
    private const uint MfSeparator = 0x800;
    private const uint TpmRightButton = 2;
    private const uint TpmReturnCmd = 0x100;
    private const int SettingsCommand = 1;
    private const int TestCommand = 2;
    private const int ExitCommand = 3;
    private static readonly ConcurrentDictionary<IntPtr, TrayIconService> Instances = new();
    private static readonly WindowProc WindowProcedure = WndProc;
    private static ushort _windowClass;

    private readonly Action _showSettings;
    private readonly Action _showTest;
    private readonly Action _exit;
    private readonly IntPtr _window;
    private readonly bool _ownsIcon;
    private NotifyIconData _data;

    public TrayIconService(Action showSettings, Action showTest, Action exit)
    {
        _showSettings = showSettings;
        _showTest = showTest;
        _exit = exit;
        EnsureWindowClass();
        _window = CreateWindowEx(0, "FluentConnect.TrayWindow", string.Empty, 0,
            0, 0, 0, 0, new IntPtr(-3), IntPtr.Zero, GetModuleHandle(null), IntPtr.Zero);
        if (_window == IntPtr.Zero) throw new InvalidOperationException("Could not create the tray message window.");
        Instances[_window] = this;

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "FluentConnect.ico");
        var customIcon = File.Exists(iconPath)
            ? LoadImage(IntPtr.Zero, iconPath, 1, 0, 0, 0x10 | 0x40)
            : IntPtr.Zero;
        _ownsIcon = customIcon != IntPtr.Zero;
        var icon = _ownsIcon ? customIcon : LoadIcon(IntPtr.Zero, new IntPtr(32512));
        _data = new NotifyIconData
        {
            cbSize = Marshal.SizeOf<NotifyIconData>(), hWnd = _window, uID = 1,
            uFlags = NifMessage | NifIcon | NifTip, uCallbackMessage = CallbackMessage,
            hIcon = icon, szTip = "FluentConnect",
            szInfo = string.Empty, szInfoTitle = string.Empty
        };
        Shell_NotifyIcon(NimAdd, ref _data);
    }

    private static void EnsureWindowClass()
    {
        if (_windowClass != 0) return;
        var windowClass = new WndClass
        {
            lpfnWndProc = WindowProcedure,
            hInstance = GetModuleHandle(null),
            lpszClassName = "FluentConnect.TrayWindow"
        };
        _windowClass = RegisterClass(ref windowClass);
        if (_windowClass == 0 && Marshal.GetLastWin32Error() != 1410)
            throw new InvalidOperationException("Could not register the tray window class.");
    }

    private static IntPtr WndProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == CallbackMessage && Instances.TryGetValue(hwnd, out var instance))
        {
            var mouseMessage = lParam.ToInt32();
            if (mouseMessage == WmRButtonUp) instance.ShowMenu();
            else if (mouseMessage == WmLButtonDblClk) instance._showSettings();
            return IntPtr.Zero;
        }
        return DefWindowProc(hwnd, message, wParam, lParam);
    }

    private void ShowMenu()
    {
        var menu = CreatePopupMenu();
        try
        {
            AppendMenu(menu, MfString, SettingsCommand, "Settings");
            AppendMenu(menu, MfString, TestCommand, "Test animation");
            AppendMenu(menu, MfSeparator, 0, null);
            AppendMenu(menu, MfString, ExitCommand, "Exit");
            GetCursorPos(out var point);
            SetForegroundWindow(_window);
            var command = TrackPopupMenu(menu, TpmRightButton | TpmReturnCmd, point.X, point.Y, 0, _window, IntPtr.Zero);
            if (command == SettingsCommand) _showSettings();
            else if (command == TestCommand) _showTest();
            else if (command == ExitCommand) _exit();
        }
        finally { DestroyMenu(menu); }
    }

    public void Dispose()
    {
        Shell_NotifyIcon(NimDelete, ref _data);
        if (_ownsIcon && _data.hIcon != IntPtr.Zero) DestroyIcon(_data.hIcon);
        Instances.TryRemove(_window, out _);
        DestroyWindow(_window);
    }

    private delegate IntPtr WindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WndClass
    {
        public uint style;
        public WindowProc lpfnWndProc;
        public int cbClsExtra, cbWndExtra;
        public IntPtr hInstance, hIcon, hCursor, hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID, uFlags, uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szTip;
        public uint dwState, dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X, Y; }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern ushort RegisterClass(ref WndClass windowClass);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr CreateWindowEx(uint exStyle, string className, string windowName, uint style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr parameter);
    [DllImport("user32.dll")] private static extern IntPtr DefWindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DestroyWindow(IntPtr hwnd);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string? moduleName);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool Shell_NotifyIcon(uint message, ref NotifyIconData data);
    [DllImport("user32.dll")] private static extern IntPtr LoadIcon(IntPtr instance, IntPtr iconName);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr LoadImage(IntPtr instance, string name, uint type, int width, int height, uint load);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DestroyIcon(IntPtr icon);
    [DllImport("user32.dll")] private static extern IntPtr CreatePopupMenu();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool AppendMenu(IntPtr menu, uint flags, int id, string? text);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DestroyMenu(IntPtr menu);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern int TrackPopupMenu(IntPtr menu, uint flags, int x, int y, int reserved, IntPtr hwnd, IntPtr rect);
}
