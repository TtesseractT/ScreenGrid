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
            menu.Items.Add("Load Grid from File…", null, (_, _) => LoadGridFromFile());            menu.Items.Add("Reset Grid to Defaults", null, (_, _) => ResetGridToDefaults());            menu.Items.Add(new ToolStripSeparator());
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
                "3. A grid overlay appears with five rows:\n" +
                "       • Halves   (2 columns)\n" +
                "       • Thirds   (3 columns)\n" +
                "       • 4:3      (2 columns, 4:3 ratio)\n" +
                "       • Quarters (4 columns)\n" +
                "       • Fifths   (5 columns)\n" +
                "4. Drag to the desired zone – it highlights.\n" +
                "5. Release the mouse button to snap the window.\n\n" +
                "Release SHIFT at any time to cancel.\n" +
                "The window snaps to the full height of your\n" +
                "screen at the chosen column width.",
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
                "This will restore: Halves, Thirds, 4:3, Quarters, and Fifths.\n" +
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

            int x = (int)zone.SnapBounds.X;
            int y = (int)zone.SnapBounds.Y;
            int w = (int)zone.SnapBounds.Width;
            int h = (int)zone.SnapBounds.Height;

            // Compensate for Windows 10 invisible borders (DWM extended frame)
            try
            {
                int hr = NativeMethods.DwmGetWindowAttribute(
                    _draggedWindowHandle,
                    NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS,
                    out NativeMethods.RECT frameRect,
                    Marshal.SizeOf<NativeMethods.RECT>());

                if (hr == 0)
                {
                    NativeMethods.GetWindowRect(_draggedWindowHandle, out NativeMethods.RECT windowRect);

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

            NativeMethods.MoveWindow(_draggedWindowHandle, x, y, w, h, true);
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private static bool IsShiftHeld()
        {
            return (NativeMethods.GetAsyncKeyState(NativeMethods.VK_SHIFT) & 0x8000) != 0;
        }

        // ── Shutdown ────────────────────────────────────────────────────

        private void ExitApp()
        {
            if (_winEventHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(_winEventHook);
                _winEventHook = IntPtr.Zero;
            }

            _mouseTracker?.Stop();
            _trayIcon?.Dispose();
            _trayIcon = null;
            _overlay?.Close();

            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayIcon?.Dispose();
            base.OnExit(e);
        }
    }
}
