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
        private int _visiblePairStartRow;

        // ── Screen info (physical pixels) ───────────────────────────────
        private NativeMethods.RECT _workArea;
        private double _dpiScale = 1.0;

        // ── Grid configuration (loaded from GridConfig) ─────────────────────────
        private GridConfig _gridConfig = GridConfig.CreateDefault();

        // ── Colors (computed from appearance settings) ──────────────────
        private Color _overlayBg;
        private Color _zoneFill;
        private Color _zoneBorder;
        private Color _highlightFill;
        private Color _highlightBorder;
        private Color _snapPreviewFill;
        private Color _snapPreviewStroke;
        private static readonly Color LabelPrimary       = Color.FromArgb(230, 255, 255, 255);
        private static readonly Color LabelSecondary     = Color.FromArgb(130, 255, 255, 255);
        private static readonly Color LabelDim           = Color.FromArgb(90, 255, 255, 255);

        private void UpdateColorsFromAppearance()
        {
            var a = _gridConfig.Appearance;
            var (r, g, b) = a.AccentRgb;

            _overlayBg        = Color.FromArgb((byte)Math.Clamp(a.OverlayAlpha, 0, 255), 10, 10, 15);
            _zoneFill         = Color.FromArgb((byte)Math.Clamp(a.ZoneFillAlpha, 0, 255), 255, 255, 255);
            _zoneBorder       = Color.FromArgb((byte)Math.Clamp(a.ZoneBorderAlpha, 0, 255), 255, 255, 255);
            _highlightFill    = Color.FromArgb((byte)Math.Clamp(a.HighlightFillAlpha, 0, 255), r, g, b);
            _highlightBorder  = Color.FromArgb((byte)Math.Clamp(a.HighlightBorderAlpha, 0, 255), r, g, b);
            _snapPreviewFill  = Color.FromArgb((byte)Math.Clamp(a.SnapPreviewFillAlpha, 0, 255), r, g, b);
            _snapPreviewStroke = Color.FromArgb((byte)Math.Clamp(a.SnapPreviewBorderAlpha, 0, 255), r, g, b);
        }

        // Snap preview label
        private TextBlock? _snapLabel;

        // ─────────────────────────────────────────────────────────────────
        public OverlayWindow()
        {
            InitializeComponent();
            UpdateColorsFromAppearance();
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
            _visiblePairStartRow = 0;
            UpdateColorsFromAppearance();
        }

        public void ResetVariantPair()
        {
            _visiblePairStartRow = 0;
        }

        public void ScrollVariantPair(int direction)
        {
            int rowCount = _gridConfig.Rows.Count;
            if (rowCount <= 2) return;

            int maxStart = Math.Max(0, ((rowCount - 1) / 2) * 2);
            int next = _visiblePairStartRow + (direction > 0 ? 2 : -2);
            if (next < 0) next = 0;
            if (next > maxStart) next = maxStart;
            if (next == _visiblePairStartRow) return;

            _visiblePairStartRow = next;
            _highlightedZone = null;
            BuildZones();
            RenderZones();
            UpdateSnapPreview(null);
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
                prev.Background      = new SolidColorBrush(_zoneFill);
                prev.BorderBrush     = new SolidColorBrush(_zoneBorder);
                prev.BorderThickness = new Thickness(1);
                prev.Effect          = null;
                _highlightedZone.IsHighlighted = false;
            }

            // Highlight new
            if (zone != null && _zoneBorders.TryGetValue(zone, out var cur))
            {
                cur.Background      = new SolidColorBrush(_highlightFill);
                cur.BorderBrush     = new SolidColorBrush(_highlightBorder);
                cur.BorderThickness = new Thickness(2);
                cur.Effect = new DropShadowEffect
                {
                    Color       = _highlightBorder,
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

            var rows = GetVisibleRows();
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

                // Height ratios (vertical subdivision)
                var heightRatios = rowDef.HasHeightSplit ? rowDef.HeightRatios! : new List<int> { 1 };
                int totalHeightParts = 0;
                foreach (int hr in heightRatios) totalHeightParts += hr;
                int numVRows = heightRatios.Count;

                int numCols = rowDef.Ratios.Count;
                int xOffset = screenLeft;

                for (int col = 0; col < numCols; col++)
                {
                    int w = (int)((double)rowDef.Ratios[col] / totalParts * screenW);
                    if (col == numCols - 1)
                        w = screenLeft + screenW - xOffset;

                    // Vertical sub-rows within this column
                    int snapYOffset = screenTop;
                    for (int vRow = 0; vRow < numVRows; vRow++)
                    {
                        int snapH = (int)((double)heightRatios[vRow] / totalHeightParts * screenH);
                        if (vRow == numVRows - 1)
                            snapH = screenTop + screenH - snapYOffset;

                        int dispSubH = rowH / numVRows;
                        int dispSubTop = dispTop + vRow * dispSubH;
                        if (vRow == numVRows - 1)
                            dispSubH = dispTop + rowH - dispSubTop;

                        // Label: combine column label with height label
                        string colLabel = rowDef.GetColumnLabel(col);
                        string hLabel = rowDef.GetHeightLabel(vRow);
                        string label = string.IsNullOrEmpty(hLabel) ? colLabel : $"{colLabel} {hLabel}";

                        _zones.Add(new GridZone
                        {
                            DisplayBounds = new Rect(xOffset + gap, dispSubTop + gap, w - 2 * gap, dispSubH - 2 * gap),
                            SnapBounds    = new Rect(xOffset, snapYOffset, w, snapH),
                            Label         = label,
                            Row           = row,
                            Column        = col,
                            TotalColumns  = numCols
                        });

                        snapYOffset += snapH;
                    }

                    xOffset += w;
                }
            }
        }

        private List<GridRowDef> GetVisibleRows()
        {
            var rows = _gridConfig.Rows;
            if (rows.Count <= 2)
                return rows;

            if (_visiblePairStartRow >= rows.Count)
                _visiblePairStartRow = Math.Max(0, ((rows.Count - 1) / 2) * 2);

            int take = Math.Min(2, rows.Count - _visiblePairStartRow);
            return rows.GetRange(_visiblePairStartRow, take);
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
                Fill   = new SolidColorBrush(_overlayBg)
            };
            GridCanvas.Children.Add(bg);

            var visibleRows = GetVisibleRows();
            string pairTitle = visibleRows.Count == 2
                ? $"{visibleRows[0].Name}  +  {visibleRows[1].Name}"
                : visibleRows[0].Name;
            int totalPairs = Math.Max(1, (_gridConfig.Rows.Count + 1) / 2);
            int currentPair = (_visiblePairStartRow / 2) + 1;

            var headerBar = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Width = Width,
                Height = 68
            };

            var headerStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            };

            headerStack.Children.Add(new TextBlock
            {
                Text = pairTitle,
                Foreground = new SolidColorBrush(Color.FromArgb(235, 255, 255, 255)),
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            var dots = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 6, 0, 0)
            };

            for (int i = 1; i <= totalPairs; i++)
            {
                bool isActive = i == currentPair;
                dots.Children.Add(new Ellipse
                {
                    Width = isActive ? 12 : 8,
                    Height = isActive ? 12 : 8,
                    Margin = new Thickness(4, 0, 4, 0),
                    Fill = new SolidColorBrush(isActive
                        ? Color.FromArgb(230, 0, 170, 255)
                        : Color.FromArgb(110, 255, 255, 255)),
                    Stroke = new SolidColorBrush(Color.FromArgb(190, 255, 255, 255)),
                    StrokeThickness = isActive ? 1.2 : 0.8
                });
            }

            headerStack.Children.Add(dots);
            headerBar.Child = headerStack;

            Canvas.SetLeft(headerBar, 0);
            Canvas.SetTop(headerBar, 0);
            Panel.SetZIndex(headerBar, 7);
            GridCanvas.Children.Add(headerBar);

            var scrollHint = new TextBlock
            {
                Text = "Scroll mouse wheel for more variants",
                Foreground = new SolidColorBrush(Color.FromArgb(205, 255, 255, 255)),
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                Background = new SolidColorBrush(Color.FromArgb(120, 0, 0, 0)),
                Padding = new Thickness(10, 5, 10, 5)
            };
            scrollHint.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(scrollHint, Math.Max(10, (Width - scrollHint.DesiredSize.Width) / 2));
            Canvas.SetTop(scrollHint, 74);
            Panel.SetZIndex(scrollHint, 7);
            GridCanvas.Children.Add(scrollHint);

            // Snap preview rectangle (hidden until a zone is highlighted)
            _snapPreview = new Rectangle
            {
                Fill            = new SolidColorBrush(_snapPreviewFill),
                Stroke          = new SolidColorBrush(_snapPreviewStroke),
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
            int rowCount = visibleRows.Count;
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
                    Text                = zone.Label,
                    Foreground          = new SolidColorBrush(LabelPrimary),
                    FontSize            = 34,
                    FontWeight          = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                stack.Children.Add(new TextBlock
                {
                    Text                = visibleRows.Count > zone.Row ? visibleRows[zone.Row].Name : "",
                    Foreground          = new SolidColorBrush(LabelDim),
                    FontSize            = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin              = new Thickness(0, 4, 0, 0)
                });

                var border = new Border
                {
                    Width           = w,
                    Height          = h,
                    Background      = new SolidColorBrush(_zoneFill),
                    BorderBrush     = new SolidColorBrush(_zoneBorder),
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
            double y = (zone.SnapBounds.Y - _workArea.Top)  / _dpiScale;
            double w = zone.SnapBounds.Width  / _dpiScale;
            double h = zone.SnapBounds.Height / _dpiScale;
            double previewMargin = 6;

            Canvas.SetLeft(_snapPreview, x + previewMargin);
            Canvas.SetTop(_snapPreview, y + previewMargin);
            _snapPreview.Width      = w - 2 * previewMargin;
            _snapPreview.Height     = h - 2 * previewMargin;
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
                Canvas.SetTop(_snapLabel, y + (h - labelH) / 2);
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
