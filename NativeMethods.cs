using System;
using System.Runtime.InteropServices;

namespace ScreenGrid
{
    /// <summary>
    /// Win32 P/Invoke declarations for window management, hooks, and DPI.
    /// </summary>
    internal static class NativeMethods
    {
        // ── Win Event Hook ──────────────────────────────────────────────
        public delegate void WinEventDelegate(
            IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(
            uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        // ── Low-level Mouse Hook ───────────────────────────────────────
        public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        // ── Window Positioning ──────────────────────────────────────────
        [DllImport("user32.dll")]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        // ── Extended Window Styles ──────────────────────────────────────
        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        // ── Cursor & Keyboard ───────────────────────────────────────────
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        // ── Monitor Info ────────────────────────────────────────────────
        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        // ── DPI ─────────────────────────────────────────────────────────
        [DllImport("shcore.dll")]
        public static extern int GetDpiForMonitor(IntPtr hMonitor, int dpiType, out uint dpiX, out uint dpiY);

        // ── DWM (for invisible border compensation) ─────────────────────
        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute,
            out RECT pvAttribute, int cbAttribute);

        // ── Window State ────────────────────────────────────────────────
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);   // minimized?

        [DllImport("user32.dll")]
        public static extern bool IsZoomed(IntPtr hWnd);   // maximized?

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // ── Icon (cleanup) ──────────────────────────────────────────────
        [DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr handle);

        // ── Constants ───────────────────────────────────────────────────

        // WinEvent
        public const uint EVENT_SYSTEM_MOVESIZESTART = 0x000A;
        public const uint EVENT_SYSTEM_MOVESIZEEND   = 0x000B;
        public const uint WINEVENT_OUTOFCONTEXT      = 0x0000;
        public const uint WINEVENT_SKIPOWNPROCESS    = 0x0002;

        // Hooks / mouse messages
        public const int WH_MOUSE_LL = 14;
        public const int WM_MOUSEWHEEL = 0x020A;

        // Extended window styles
        public const int GWL_EXSTYLE        = -20;
        public const int GWL_STYLE          = -16;
        public const int WS_EX_TRANSPARENT  = 0x00000020;
        public const int WS_EX_TOOLWINDOW   = 0x00000080;
        public const int WS_EX_NOACTIVATE   = 0x08000000;
        public const int WS_MAXIMIZEBOX     = 0x00010000;
        public const int WS_THICKFRAME      = 0x00040000;
        public const int WS_CAPTION         = 0x00C00000;

        // ShowWindow commands
        public const int SW_RESTORE         = 9;

        // Virtual key codes
        public const int VK_SHIFT   = 0x10;
        public const int VK_CONTROL = 0x11;
        public const int VK_MENU    = 0x12; // Alt
        public const int VK_ESCAPE  = 0x1B;

        // Monitor flags
        public const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        // DWM attributes
        public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

        // SetWindowPos flags
        public const uint SWP_NOZORDER       = 0x0004;
        public const uint SWP_NOACTIVATE     = 0x0010;
        public const uint SWP_FRAMECHANGED   = 0x0020;

        // ── Structures ──────────────────────────────────────────────────

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
    }
}
