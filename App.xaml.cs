using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace ScreenGrid
{
    public partial class App : Application
    {
        // ── State ───────────────────────────────────────────────────────
        private NotifyIcon? _trayIcon;
        private OverlayWindow? _overlay;
        private DispatcherTimer? _mouseTracker;        private GridConfig _gridConfig = GridConfig.CreateDefault();
        // Win-event hook (must be stored to prevent GC)
        private NativeMethods.WinEventDelegate? _winEventDelegate;
        private IntPtr _winEventHook;
        private NativeMethods.LowLevelMouseProc? _mouseHookProc;
        private IntPtr _mouseHook;

        // Drag state
        private IntPtr _draggedWindowHandle;
        private bool _isDragging;
        private bool _overlayVisible;

        // ── Startup ─────────────────────────────────────────────────────

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _gridConfig = GridConfig.LoadActive();
            SetupTrayIcon();
            SetupOverlay();
            SetupWinEventHook();
            SetupMouseWheelHook();
            SetupMouseTracker();
        }

        // ── Tray Icon ───────────────────────────────────────────────────

        private void SetupTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                Icon    = CreateGridIcon(),
                Visible = true,
                Text    = "ScreenGrid – Hold Shift while dragging a window"
            };

            var menu = new ContextMenuStrip();
            var header = new ToolStripMenuItem("ScreenGrid v1.0") { Enabled = false };
            menu.Items.Add(header);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Create / Edit Grid", null, (_, _) => ShowGridEditor());
            menu.Items.Add("Load Grid from File…", null, (_, _) => LoadGridFromFile());
            menu.Items.Add("Reset Grid to Defaults", null, (_, _) => ResetGridToDefaults());
            menu.Items.Add(new ToolStripSeparator());

            var startupItem = new ToolStripMenuItem("Run at Startup")
            {
                CheckOnClick = true,
                Checked = StartupManager.IsRegistered()
            };
            startupItem.CheckedChanged += (_, _) =>
            {
                try
                {
                    if (startupItem.Checked)
                        StartupManager.Register();
                    else
                        StartupManager.Unregister();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to update startup setting:\n{ex.Message}",
                        "ScreenGrid", MessageBoxButton.OK, MessageBoxImage.Warning);
                    startupItem.Checked = StartupManager.IsRegistered();
                }
            };
            menu.Items.Add(startupItem);

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("How to use", null, (_, _) => ShowUsageInfo());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (_, _) => ExitApp());

            _trayIcon.ContextMenuStrip = menu;
            _trayIcon.DoubleClick += (_, _) => ShowUsageInfo();
        }

        private static void ShowUsageInfo()
        {
            MessageBox.Show(
                "ScreenGrid is running in the background.\n\n" +
                "HOW TO USE:\n" +
                "1. Start dragging any window by its title bar.\n" +
                "2. While dragging, hold the SHIFT key.\n" +
                "3. Two grid variants are shown at a time.\n" +
                "4. Scroll the mouse wheel (up/down) to switch pairs.\n" +
                "5. Drag to the desired zone – it highlights.\n" +
                "6. Release the mouse button to snap the window.\n\n" +
                "Release SHIFT at any time to cancel.\n" +
                "Tip: the overlay header shows Pair X/Y and reminds\n" +
                "you to scroll for more variants.",
                "ScreenGrid",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        /// <summary>Creates a simple 16×16 grid icon for the system tray.</summary>
        private static Icon CreateGridIcon()
        {
            var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                using var pen = new Pen(Color.CornflowerBlue, 1f);
                g.DrawRectangle(pen, 1, 1, 13, 13);
                g.DrawLine(pen, 5, 1, 5, 14);
                g.DrawLine(pen, 10, 1, 10, 14);
                g.DrawLine(pen, 1, 5, 14, 5);
                g.DrawLine(pen, 1, 10, 14, 10);
            }
            return Icon.FromHandle(bmp.GetHicon());
        }

        // ── Overlay ─────────────────────────────────────────────────────

        private void SetupOverlay()
        {
            _overlay = new OverlayWindow();            _overlay.SetGridConfig(_gridConfig);
        }

        private void ApplyGridConfig(GridConfig config)
        {
            _gridConfig = config;
            _overlay?.SetGridConfig(config);
        }

        // ── Grid Editor ─────────────────────────────────────────────────────

        private GridEditorWindow? _editorWindow;

        private void ShowGridEditor()
        {
            if (_editorWindow != null && _editorWindow.IsVisible)
            {
                _editorWindow.Activate();
                return;
            }

            _editorWindow = new GridEditorWindow(_gridConfig);
            _editorWindow.Applied += config => ApplyGridConfig(config);
            _editorWindow.Closed += (_, _) => _editorWindow = null;
            _editorWindow.Show();
        }

        private void LoadGridFromFile()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter           = GridConfig.FileFilter,
                DefaultExt       = GridConfig.FileExtension,
                InitialDirectory = GridConfig.GetAppDataFolder()
            };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var config = GridConfig.LoadFromFile(dlg.FileName);
                    config.SaveAsActive();
                    ApplyGridConfig(config);
                    MessageBox.Show($"Loaded grid: {config.Name}\n({config.Rows.Count} rows)",
                        "Grid Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load grid file:\n{ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ResetGridToDefaults()
        {
            var result = MessageBox.Show(
                "Reset the grid layout to the built-in defaults?\n\n" +
                "This will restore default pairs (full-height + ½H variants).\n" +
                "Your current layout will be replaced.",
                "Reset to Defaults",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var config = GridConfig.CreateDefault();
                config.SaveAsActive();
                ApplyGridConfig(config);
            }
        }

        // ── Win-Event Hook (detect window drag start / end) ─────────────

        private void SetupWinEventHook()
        {
            _winEventDelegate = WinEventCallback;
            _winEventHook = NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_SYSTEM_MOVESIZESTART,
                NativeMethods.EVENT_SYSTEM_MOVESIZEEND,
                IntPtr.Zero,
                _winEventDelegate,
                0, 0,
                NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS);
        }

        private void WinEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            try
            {
                if (eventType == NativeMethods.EVENT_SYSTEM_MOVESIZESTART)
                {
                    // Only activate for normal application windows (title bar, visible, not games/tools)
                    if (!IsNormalAppWindow(hwnd))
                        return;

                    _isDragging = true;
                    _draggedWindowHandle = hwnd;
                    _mouseTracker?.Start();

                    // If Shift is already held, show overlay immediately
                    if (IsShiftHeld())
                        ShowOverlayOnCurrentMonitor();
                }
                else if (eventType == NativeMethods.EVENT_SYSTEM_MOVESIZEEND)
                {
                    if (_overlayVisible)
                    {
                        SnapWindowToHighlightedZone();
                        HideOverlay();
                    }
                    _isDragging = false;
                    _mouseTracker?.Stop();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WinEventCallback error: {ex.Message}");
            }
        }

        // ── Mouse tracker (polls cursor pos + Shift state at ~60fps) ────

        private void SetupMouseTracker()
        {
            _mouseTracker = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _mouseTracker.Tick += OnMouseTrackerTick;
        }

        private void SetupMouseWheelHook()
        {
            _mouseHookProc = MouseHookCallback;
            _mouseHook = NativeMethods.SetWindowsHookEx(
                NativeMethods.WH_MOUSE_LL,
                _mouseHookProc,
                IntPtr.Zero,
                0);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0
                && wParam.ToInt32() == NativeMethods.WM_MOUSEWHEEL
                && _isDragging
                && _overlayVisible
                && IsShiftHeld()
                && _overlay != null)
            {
                var data = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
                short delta = unchecked((short)((data.mouseData >> 16) & 0xFFFF));
                int direction = delta > 0 ? -1 : 1;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    _overlay?.ScrollVariantPair(direction);
                }));
            }

            return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        private void OnMouseTrackerTick(object? sender, EventArgs e)
        {
            if (!_isDragging)
            {
                _mouseTracker?.Stop();
                return;
            }

            bool shiftHeld = IsShiftHeld();

            // Show overlay when Shift is pressed mid-drag
            if (shiftHeld && !_overlayVisible)
            {
                ShowOverlayOnCurrentMonitor();
                return;
            }

            // Hide overlay when Shift is released mid-drag
            if (!shiftHeld && _overlayVisible)
            {
                HideOverlay();
                return;
            }

            // Update zone highlight
            if (_overlayVisible && _overlay != null)
            {
                NativeMethods.GetCursorPos(out var pos);
                var zone = _overlay.GetZoneAtPoint(pos.X, pos.Y);
                _overlay.HighlightZone(zone);
            }
        }

        // ── Overlay management ──────────────────────────────────────────

        private void ShowOverlayOnCurrentMonitor()
        {
            if (_overlayVisible || _overlay == null) return;

            NativeMethods.GetCursorPos(out var cursorPos);
            var hMonitor = NativeMethods.MonitorFromPoint(cursorPos, NativeMethods.MONITOR_DEFAULTTONEAREST);

            var mi = new NativeMethods.MONITORINFO
            {
                cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>()
            };
            if (!NativeMethods.GetMonitorInfo(hMonitor, ref mi))
                return;

            _overlay.ResetVariantPair();
            _overlay.SetupForScreen(mi.rcWork);
            _overlay.ShowOverlay();
            _overlayVisible = true;
        }

        private void HideOverlay()
        {
            _overlayVisible = false; // Immediately mark as hidden
            _overlay?.HideOverlay();
        }

        // ── Window snapping ─────────────────────────────────────────────

        private void SnapWindowToHighlightedZone()
        {
            if (_overlay == null) return;
            var zone = _overlay.GetHighlightedZone();
            if (zone == null || _draggedWindowHandle == IntPtr.Zero) return;

            IntPtr hwnd = _draggedWindowHandle;

            // Validate the window handle is still valid
            if (!NativeMethods.IsWindow(hwnd))
            {
                System.Diagnostics.Debug.WriteLine("SnapWindow: handle is no longer valid");
                return;
            }

            // If the window is maximized, restore it first so MoveWindow/SetWindowPos works
            if (NativeMethods.IsZoomed(hwnd))
            {
                NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);
                // Small delay to let the window finish restoring
                System.Threading.Thread.Sleep(50);
            }

            int x = (int)zone.SnapBounds.X;
            int y = (int)zone.SnapBounds.Y;
            int w = (int)zone.SnapBounds.Width;
            int h = (int)zone.SnapBounds.Height;

            // Compensate for Windows 10/11 invisible borders (DWM extended frame)
            try
            {
                int hr = NativeMethods.DwmGetWindowAttribute(
                    hwnd,
                    NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS,
                    out NativeMethods.RECT frameRect,
                    Marshal.SizeOf<NativeMethods.RECT>());

                if (hr == 0)
                {
                    NativeMethods.GetWindowRect(hwnd, out NativeMethods.RECT windowRect);

                    int borderL = frameRect.Left   - windowRect.Left;
                    int borderT = frameRect.Top    - windowRect.Top;
                    int borderR = windowRect.Right  - frameRect.Right;
                    int borderB = windowRect.Bottom - frameRect.Bottom;

                    x -= borderL;
                    y -= borderT;
                    w += borderL + borderR;
                    h += borderT + borderB;
                }
            }
            catch
            {
                // Fallback: just use raw zone bounds
            }

            // Try MoveWindow first, then SetWindowPos as fallback
            if (!NativeMethods.MoveWindow(hwnd, x, y, w, h, true))
            {
                System.Diagnostics.Debug.WriteLine("MoveWindow failed, trying SetWindowPos");
                NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, x, y, w, h,
                    NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_FRAMECHANGED);
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private static bool IsShiftHeld()
        {
            return (NativeMethods.GetAsyncKeyState(NativeMethods.VK_SHIFT) & 0x8000) != 0;
        }

        /// <summary>
        /// Returns true only for normal resizable app windows with a title bar.
        /// Rejects fullscreen/exclusive games, tool windows, invisible windows, etc.
        /// </summary>
        private static bool IsNormalAppWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd))
                return false;

            if (!NativeMethods.IsWindowVisible(hwnd))
                return false;

            int style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE);
            int exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);

            // Reject tool windows (small floating palettes) unless they also have WS_EX_APPWINDOW
            bool isToolWindow = (exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0;
            bool isAppWindow = (exStyle & NativeMethods.WS_EX_APPWINDOW) != 0;
            if (isToolWindow && !isAppWindow)
                return false;

            // Reject known fullscreen game/overlay class names
            var sb = new System.Text.StringBuilder(256);
            NativeMethods.GetClassName(hwnd, sb, 256);
            string className = sb.ToString();

            // Common fullscreen game / overlay classes to reject
            string[] blockedClasses = {
                "UnityWndClass", "UnrealWindow", "LaunchUnrealUWindowsClient",
                "SDL_app", "GLFW30", "ConsoleWindowClass",
                "CryENGINE", "Source Engine", "Valve001"
            };
            foreach (var blocked in blockedClasses)
            {
                if (className.Equals(blocked, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Reject windows covering the entire monitor (borderless fullscreen games)
            NativeMethods.GetWindowRect(hwnd, out var wndRect);
            var pt = new NativeMethods.POINT { X = wndRect.Left + 1, Y = wndRect.Top + 1 };
            var hMon = NativeMethods.MonitorFromPoint(pt, NativeMethods.MONITOR_DEFAULTTONEAREST);
            var mi = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>() };
            if (NativeMethods.GetMonitorInfo(hMon, ref mi))
            {
                var mon = mi.rcMonitor;
                bool coversFullScreen =
                    wndRect.Left <= mon.Left &&
                    wndRect.Top <= mon.Top &&
                    wndRect.Right >= mon.Right &&
                    wndRect.Bottom >= mon.Bottom;

                // A true maximized window is fine (it sits in the work area),
                // but a borderless fullscreen window covers the whole monitor
                if (coversFullScreen && !NativeMethods.IsZoomed(hwnd))
                    return false;
            }

            return true;
        }

        // ── Shutdown ────────────────────────────────────────────────────

        private void ExitApp()
        {
            if (_winEventHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(_winEventHook);
                _winEventHook = IntPtr.Zero;
            }

            if (_mouseHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }

            _mouseTracker?.Stop();
            _trayIcon?.Dispose();
            _trayIcon = null;
            _overlay?.Close();

            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_mouseHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }
            _trayIcon?.Dispose();
            base.OnExit(e);
        }
    }
}
