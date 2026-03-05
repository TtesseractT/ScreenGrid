using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace ScreenGrid
{
    public partial class OverlayWindow : Window
    {
        // ── Zone data ───────────────────────────────────────────────────
        private readonly List<GridZone> _zones = new();
        private readonly Dictionary<GridZone, Border> _zoneBorders = new();
        private GridZone? _highlightedZone;
        private Rectangle? _snapPreview;

        // ── Screen info (physical pixels) ───────────────────────────────
        private NativeMethods.RECT _workArea;
        private double _dpiScale = 1.0;

        // ── Grid configuration (loaded from GridConfig) ─────────────────────────
        private GridConfig _gridConfig = GridConfig.CreateDefault();

        // ── Colors ──────────────────────────────────────────────────────
        private static readonly Color OverlayBg         = Color.FromArgb(200, 10, 10, 15);   // more opaque
        private static readonly Color ZoneFill           = Color.FromArgb(25, 255, 255, 255);
        private static readonly Color ZoneBorder         = Color.FromArgb(65, 255, 255, 255);
        private static readonly Color HighlightFill      = Color.FromArgb(85, 0, 140, 255);
        private static readonly Color HighlightBorder    = Color.FromArgb(220, 0, 140, 255);
        private static readonly Color SnapPreviewFill    = Color.FromArgb(40, 0, 150, 255);   // more visible preview
        private static readonly Color SnapPreviewStroke  = Color.FromArgb(160, 0, 150, 255);
        private static readonly Color LabelPrimary       = Color.FromArgb(230, 255, 255, 255);
        private static readonly Color LabelSecondary     = Color.FromArgb(130, 255, 255, 255);
        private static readonly Color LabelDim           = Color.FromArgb(90, 255, 255, 255);

        // Snap preview label
        private TextBlock? _snapLabel;

        // ─────────────────────────────────────────────────────────────────
        public OverlayWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            MakeClickThrough();
        }

        /// <summary>
        /// Makes this window transparent to all mouse / keyboard input so it
        /// never interferes with the window drag happening underneath.
        /// </summary>
        private void MakeClickThrough()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE,
                exStyle
                | NativeMethods.WS_EX_TRANSPARENT
                | NativeMethods.WS_EX_TOOLWINDOW
                | NativeMethods.WS_EX_NOACTIVATE);
        }

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>
        /// Update the grid configuration and rebuild zones on next SetupForScreen.
        /// </summary>
        public void SetGridConfig(GridConfig config)
        {
            _gridConfig = config;
        }

        /// <summary>
        /// Configure the overlay for the given monitor work area and rebuild
        /// all grid zones.  workArea is in physical screen pixels.
        /// </summary>
        internal void SetupForScreen(NativeMethods.RECT workArea)
        {
            _workArea = workArea;
            _dpiScale = GetDpiScaleForWorkArea(workArea);

            // Position & size in WPF DIPs
            Left   = workArea.Left / _dpiScale;
            Top    = workArea.Top  / _dpiScale;
            Width  = (workArea.Right  - workArea.Left) / _dpiScale;
            Height = (workArea.Bottom - workArea.Top)  / _dpiScale;

            BuildZones();
            RenderZones();
        }

        /// <summary>
        /// Returns the zone whose display bounds contain the given point
        /// (physical screen coordinates), or null.
        /// </summary>
        public GridZone? GetZoneAtPoint(int screenX, int screenY)
        {
            var pt = new Point(screenX, screenY);
            foreach (var z in _zones)
                if (z.DisplayBounds.Contains(pt))
                    return z;
            return null;
        }

        /// <summary>
        /// Highlight a single zone (and show snap preview).  Pass null to clear.
        /// </summary>
        public void HighlightZone(GridZone? zone)
        {
            if (zone == _highlightedZone) return;

            // Un-highlight previous
            if (_highlightedZone != null && _zoneBorders.TryGetValue(_highlightedZone, out var prev))
            {
                prev.Background      = new SolidColorBrush(ZoneFill);
                prev.BorderBrush     = new SolidColorBrush(ZoneBorder);
                prev.BorderThickness = new Thickness(1);
                prev.Effect          = null;
                _highlightedZone.IsHighlighted = false;
            }

            // Highlight new
            if (zone != null && _zoneBorders.TryGetValue(zone, out var cur))
            {
                cur.Background      = new SolidColorBrush(HighlightFill);
                cur.BorderBrush     = new SolidColorBrush(HighlightBorder);
                cur.BorderThickness = new Thickness(2);
                cur.Effect = new DropShadowEffect
                {
                    Color       = HighlightBorder,
                    BlurRadius  = 24,
                    ShadowDepth = 0,
                    Opacity     = 0.6
                };
                zone.IsHighlighted = true;
            }

            UpdateSnapPreview(zone);
            _highlightedZone = zone;
        }

        public GridZone? GetHighlightedZone() => _highlightedZone;

        /// <summary>Fade the overlay in.</summary>
        public void ShowOverlay()
        {
            _highlightedZone = null;
            Opacity = 0;
            Show();
            BeginAnimation(OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(120))
                { EasingFunction = new QuadraticEase() });
        }

        /// <summary>Fade the overlay out then hide.</summary>
        public void HideOverlay(Action? onComplete = null)
        {
            var anim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(90));
            anim.Completed += (_, _) =>
            {
                Hide();
                onComplete?.Invoke();
            };
            BeginAnimation(OpacityProperty, anim);
        }

        // ── Zone building ───────────────────────────────────────────────

        private void BuildZones()
        {
            _zones.Clear();

            int screenW    = _workArea.Right  - _workArea.Left;
            int screenH    = _workArea.Bottom - _workArea.Top;
            int screenLeft = _workArea.Left;
            int screenTop  = _workArea.Top;

            var rows = _gridConfig.Rows;
            int rowCount = rows.Count;
            if (rowCount == 0) return;

            int rowH     = screenH / rowCount;
            const int gap = 4;

            for (int row = 0; row < rowCount; row++)
            {
                var rowDef = rows[row];
                int dispTop = screenTop + row * rowH;

                int totalParts = 0;
                foreach (int r in rowDef.Ratios) totalParts += r;
                if (totalParts <= 0) continue;

                int numCols = rowDef.Ratios.Count;
                int xOffset = screenLeft;

                for (int col = 0; col < numCols; col++)
                {
                    int w = (int)((double)rowDef.Ratios[col] / totalParts * screenW);
                    if (col == numCols - 1)
                        w = screenLeft + screenW - xOffset;

                    _zones.Add(new GridZone
                    {
                        DisplayBounds = new Rect(xOffset + gap, dispTop + gap, w - 2 * gap, rowH - 2 * gap),
                        SnapBounds    = new Rect(xOffset, screenTop, w, screenH),
                        Label         = rowDef.GetColumnLabel(col),
                        Row           = row,
                        Column        = col,
                        TotalColumns  = numCols
                    });
                    xOffset += w;
                }
            }
        }

        // ── Zone rendering ──────────────────────────────────────────────

        private void RenderZones()
        {
            GridCanvas.Children.Clear();
            _zoneBorders.Clear();

            // Darkened background
            var bg = new Rectangle
            {
                Width  = Width,
                Height = Height,
                Fill   = new SolidColorBrush(OverlayBg)
            };
            GridCanvas.Children.Add(bg);

            // Snap preview rectangle (hidden until a zone is highlighted)
            _snapPreview = new Rectangle
            {
                Fill            = new SolidColorBrush(SnapPreviewFill),
                Stroke          = new SolidColorBrush(SnapPreviewStroke),
                StrokeThickness = 3,
                Visibility      = Visibility.Collapsed,
                RadiusX         = 6,
                RadiusY         = 6
            };
            GridCanvas.Children.Add(_snapPreview);
            Panel.SetZIndex(_snapPreview, 0);

            // Snap preview size label
            _snapLabel = new TextBlock
            {
                Foreground          = new SolidColorBrush(Colors.White),
                FontSize            = 20,
                FontWeight          = FontWeights.Bold,
                Background          = new SolidColorBrush(Color.FromArgb(180, 0, 100, 200)),
                Padding             = new Thickness(14, 8, 14, 8),
                Visibility          = Visibility.Collapsed,
                TextAlignment       = TextAlignment.Center
            };
            GridCanvas.Children.Add(_snapLabel);
            Panel.SetZIndex(_snapLabel, 5);

            // Row separators (subtle horizontal lines between bands)
            int rowCount = _gridConfig.Rows.Count;
            for (int i = 1; i < rowCount; i++)
            {
                double y = (Height / rowCount) * i;
                var line = new Line
                {
                    X1              = 0,
                    Y1              = y,
                    X2              = Width,
                    Y2              = y,
                    Stroke          = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 8, 4 }
                };
                GridCanvas.Children.Add(line);
            }

            // Zone borders
            foreach (var zone in _zones)
            {
                double x = (zone.DisplayBounds.X - _workArea.Left) / _dpiScale;
                double y = (zone.DisplayBounds.Y - _workArea.Top)  / _dpiScale;
                double w = zone.DisplayBounds.Width  / _dpiScale;
                double h = zone.DisplayBounds.Height / _dpiScale;

                // Build label stack
                var stack = new StackPanel
                {
                    VerticalAlignment   = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                stack.Children.Add(new TextBlock
                {
                    Text                = _gridConfig.Rows.Count > zone.Row ? _gridConfig.Rows[zone.Row].Name : "",
                    Foreground          = new SolidColorBrush(LabelSecondary),
                    FontSize            = 13,
                    FontWeight          = FontWeights.Normal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin              = new Thickness(0, 0, 0, 2)
                });

                stack.Children.Add(new TextBlock
                {
                    Text                = zone.Label,
                    Foreground          = new SolidColorBrush(LabelPrimary),
                    FontSize            = 34,
                    FontWeight          = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                stack.Children.Add(new TextBlock
                {
                    Text                = $"{(int)zone.SnapBounds.Width} \u00D7 {(int)zone.SnapBounds.Height}",
                    Foreground          = new SolidColorBrush(LabelDim),
                    FontSize            = 11,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin              = new Thickness(0, 4, 0, 0)
                });

                var border = new Border
                {
                    Width           = w,
                    Height          = h,
                    Background      = new SolidColorBrush(ZoneFill),
                    BorderBrush     = new SolidColorBrush(ZoneBorder),
                    BorderThickness = new Thickness(1),
                    CornerRadius    = new CornerRadius(8),
                    Child           = stack
                };

                Canvas.SetLeft(border, x);
                Canvas.SetTop(border, y);
                Panel.SetZIndex(border, 1);
                GridCanvas.Children.Add(border);
                _zoneBorders[zone] = border;
            }
        }

        // ── Snap preview ────────────────────────────────────────────────

        private void UpdateSnapPreview(GridZone? zone)
        {
            if (_snapPreview == null) return;

            if (zone == null)
            {
                _snapPreview.Visibility = Visibility.Collapsed;
                if (_snapLabel != null) _snapLabel.Visibility = Visibility.Collapsed;
                return;
            }

            double x = (zone.SnapBounds.X - _workArea.Left) / _dpiScale;
            double w = zone.SnapBounds.Width  / _dpiScale;
            double previewMargin = 6;

            Canvas.SetLeft(_snapPreview, x + previewMargin);
            Canvas.SetTop(_snapPreview, previewMargin);
            _snapPreview.Width      = w - 2 * previewMargin;
            _snapPreview.Height     = Height - 2 * previewMargin;
            _snapPreview.Visibility = Visibility.Visible;

            // Show a clear size label centered in the preview
            if (_snapLabel != null)
            {
                int snapW = (int)zone.SnapBounds.Width;
                int snapH = (int)zone.SnapBounds.Height;
                _snapLabel.Text = $"{snapW} × {snapH} px";
                _snapLabel.Visibility = Visibility.Visible;
                _snapLabel.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                double labelW = _snapLabel.DesiredSize.Width;
                double labelH = _snapLabel.DesiredSize.Height;
                Canvas.SetLeft(_snapLabel, x + (w - labelW) / 2);
                Canvas.SetTop(_snapLabel, (Height - labelH) / 2);
            }
        }

        // ── DPI helper ──────────────────────────────────────────────────

        private static double GetDpiScaleForWorkArea(NativeMethods.RECT workArea)
        {
            try
            {
                var pt = new NativeMethods.POINT
                {
                    X = workArea.Left + 1,
                    Y = workArea.Top  + 1
                };
                var hMonitor = NativeMethods.MonitorFromPoint(pt, NativeMethods.MONITOR_DEFAULTTONEAREST);
                int hr = NativeMethods.GetDpiForMonitor(hMonitor, 0 /* MDT_EFFECTIVE_DPI */, out uint dpiX, out _);
                if (hr == 0 && dpiX > 0)
                    return dpiX / 96.0;
            }
            catch
            {
                // Fallback
            }
            return 1.0;
        }
    }
}
