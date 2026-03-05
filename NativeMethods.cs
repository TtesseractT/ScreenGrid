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

        // ── Icon (cleanup) ──────────────────────────────────────────────
        [DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr handle);

        // ── Constants ───────────────────────────────────────────────────

        // WinEvent
        public const uint EVENT_SYSTEM_MOVESIZESTART = 0x000A;
        public const uint EVENT_SYSTEM_MOVESIZEEND   = 0x000B;
        public const uint WINEVENT_OUTOFCONTEXT      = 0x0000;
        public const uint WINEVENT_SKIPOWNPROCESS    = 0x0002;

        // Extended window styles
        public const int GWL_EXSTYLE        = -20;
        public const int WS_EX_TRANSPARENT  = 0x00000020;
        public const int WS_EX_TOOLWINDOW   = 0x00000080;
        public const int WS_EX_NOACTIVATE   = 0x08000000;

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
        public const uint SWP_NOZORDER = 0x0004;

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
    }
}
