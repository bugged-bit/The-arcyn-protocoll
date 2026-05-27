using System.Runtime.InteropServices;

namespace ARCYN.UI;

internal static class NativeMethods
{
    // ------------------------------------------------------------
    // P/Invoke wrappers – call native APIs only on Windows.
    // ------------------------------------------------------------
    // SetWindowCompositionAttribute – enable acrylic blur.
    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttributeNative(IntPtr hwnd, ref WindowCompositionAttributeData data);
    internal static int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return SetWindowCompositionAttributeNative(hwnd, ref data);
        return 0; // no‑op on non‑Windows
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    internal enum WindowCompositionAttribute
    {
        WCA_ACCENT_POLICY = 19
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AccentPolicy
    {
        public AccentState AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    internal enum AccentState
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_GRADIENT = 1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_ENABLE_ACRYLICBLURBEHIND = 4
    }

    internal static void EnableAcrylic(IntPtr hwnd, uint opacityColor = 0xEB000000)
    {
        var accent = new AccentPolicy
        {
            AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
            GradientColor = (int)opacityColor
        };

        var accentStructSize = Marshal.SizeOf(accent);
        var accentPtr = Marshal.AllocHGlobal(accentStructSize);
        Marshal.StructureToPtr(accent, accentPtr, false);

        var data = new WindowCompositionAttributeData
        {
            Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
            Data = accentPtr,
            SizeOfData = accentStructSize
        };

        // Windows only – safe no‑op on Linux.
        SetWindowCompositionAttribute(hwnd, ref data);
        Marshal.FreeHGlobal(accentPtr);
    }

    internal const int WS_EX_TRANSPARENT = 0x00000020;
    internal const int WS_EX_TOOLWINDOW = 0x00000080;
    internal const int WS_EX_LAYERED = 0x00080000;
    internal const int GWL_EXSTYLE = -20;
    internal const int SW_SHOW = 5;
    internal const int SW_HIDE = 0;

    // Get/Set window styles
    [DllImport("user32.dll")]
    private static extern int GetWindowLongNative(IntPtr hWnd, int nIndex);
    internal static int GetWindowLong(IntPtr hWnd, int nIndex)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return GetWindowLongNative(hWnd, nIndex);
        return 0;
    }

    [DllImport("user32.dll")]
    private static extern int SetWindowLongNative(IntPtr hWnd, int nIndex, int dwNewLong);
    internal static int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return SetWindowLongNative(hWnd, nIndex, dwNewLong);
        return 0;
    }

    // Show/Hide window
    [DllImport("user32.dll")]
    private static extern bool ShowWindowNative(IntPtr hWnd, int nCmdShow);
    internal static bool ShowWindow(IntPtr hWnd, int nCmdShow)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return ShowWindowNative(hWnd, nCmdShow);
        return false;
    }

    // Set window position
    [DllImport("user32.dll")]
    private static extern bool SetWindowPosNative(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    internal static bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return SetWindowPosNative(hWnd, hWndInsertAfter, X, Y, cx, cy, uFlags);
        return false;
    }

    internal static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    internal static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    internal const uint SWP_NOMOVE = 0x0002;
    internal const uint SWP_NOSIZE = 0x0001;
    internal const uint SWP_SHOWWINDOW = 0x0040;

    // Console window handle – only relevant on Windows.
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindowNative();
    internal static IntPtr GetConsoleWindow()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return GetConsoleWindowNative();
        return IntPtr.Zero;
    }

    // Asynchronous show – Windows only.
    [DllImport("user32.dll")]
    private static extern bool ShowWindowAsyncNative(IntPtr hWnd, int nCmdShow);
    internal static bool ShowWindowAsync(IntPtr hWnd, int nCmdShow)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return ShowWindowAsyncNative(hWnd, nCmdShow);
        return false;
    }
}
