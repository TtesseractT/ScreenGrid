using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ScreenGrid
{
    public partial class GridEditorWindow : Window
    {
        private GridConfig _config;
        private int _previewRowIndex = -1;

        /// <summary>Raised when the user clicks Apply. The handler receives the new config.</summary>
        public event Action<GridConfig>? Applied;

        public GridEditorWindow(GridConfig config)
        {
            InitializeComponent();
            _config = CloneConfig(config);
            TxtGridName.Text = _config.Name;
            RebuildRowsUI();
        }

        // ── Row panel rendering ─────────────────────────────────────────

        private void RebuildRowsUI()
        {
            RowsPanel.Children.Clear();

            for (int i = 0; i < _config.Rows.Count; i++)
            {
                var row = _config.Rows[i];
                int rowIndex = i;

                var rowPanel = new Border
                {
                    Background      = new SolidColorBrush(Color.FromArgb(255, 34, 34, 58)),
                    BorderBrush     = new SolidColorBrush(Color.FromArgb(255, 60, 60, 90)),
                    BorderThickness = new Thickness(1),
                    CornerRadius    = new CornerRadius(5),
                    Margin          = new Thickness(0, 0, 0, 6),
                    Padding         = new Thickness(10, 10, 10, 10)
                };

                var rowStack = new StackPanel();

                var header = new Grid();
                header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

                var lblNum = new TextBlock
                {
                    Text              = $"#{i + 1}",
                    Foreground        = new SolidColorBrush(Color.FromArgb(255, 150, 150, 200)),
                    FontSize          = 13,
                    FontWeight        = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin            = new Thickness(0, 0, 10, 0)
                };
                Grid.SetColumn(lblNum, 0);
                header.Children.Add(lblNum);

                var lblTitle = new TextBlock
                {
                    Text = "Row",
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 180, 180, 220)),
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 4)
                };
                Grid.SetColumn(lblTitle, 1);
                header.Children.Add(lblTitle);

                var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
                var btnPreview = CreateSmallButton("Preview", "Show this row on the right", 64);
                btnPreview.Click += (_, _) =>
                {
                    _previewRowIndex = rowIndex;
                    RenderOrientationPreview();
                };
                actions.Children.Add(btnPreview);

                var btnUp = CreateSmallButton("↑", "Move row up", 34);
                btnUp.Click += (_, _) => { MoveRow(rowIndex, -1); };
                btnUp.IsEnabled = i > 0;
                actions.Children.Add(btnUp);

                var btnDown = CreateSmallButton("↓", "Move row down", 34);
                btnDown.Click += (_, _) => { MoveRow(rowIndex, 1); };
                btnDown.IsEnabled = i < _config.Rows.Count - 1;
                actions.Children.Add(btnDown);

                var btnCopy = CreateSmallButton("Copy", "Duplicate row", 54);
                btnCopy.Click += (_, _) =>
                {
                    DuplicateRow(rowIndex);
                };
                actions.Children.Add(btnCopy);

                var btnDel = CreateSmallButton("Delete", "Remove row", 66);
                btnDel.Click += (_, _) =>
                {
                    _config.Rows.RemoveAt(rowIndex);
                    RebuildRowsUI();
                };
                actions.Children.Add(btnDel);

                Grid.SetColumn(actions, 2);
                header.Children.Add(actions);
                rowStack.Children.Add(header);

                var body = new Grid { Margin = new Thickness(0, 4, 0, 0) };
                body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var txtName = new TextBox
                {
                    Text         = row.Name,
                    Height       = 30,
                    FontSize     = 13,
                    Background   = new SolidColorBrush(Color.FromArgb(255, 42, 42, 62)),
                    Foreground   = Brushes.White,
                    BorderBrush  = new SolidColorBrush(Color.FromArgb(255, 68, 68, 102)),
                    Padding      = new Thickness(5, 3, 5, 3),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Margin       = new Thickness(0, 0, 8, 0)
                };
                txtName.TextChanged += (_, _) => row.Name = txtName.Text;
                txtName.TextChanged += (_, _) => { if (_previewRowIndex == rowIndex) RenderOrientationPreview(); };

                var namePanel = new StackPanel();
                namePanel.Children.Add(new TextBlock
                {
                    Text = "Name",
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 180, 180, 220)),
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 4)
                });
                namePanel.Children.Add(txtName);
                Grid.SetColumn(namePanel, 0);
                body.Children.Add(namePanel);

                var widthPanel = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
                widthPanel.Children.Add(new TextBlock
                {
                    Text = "Columns (W)",
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 180, 180, 220)),
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 4),
                    ToolTip = "Column width ratios (examples: 1:1, 1:1:1, 4:3)"
                });

                var txtRatios = new TextBox
                {
                    Text         = string.Join(" : ", row.Ratios),
                    Height       = 30,
                    FontSize     = 13,
                    Background   = new SolidColorBrush(Color.FromArgb(255, 42, 42, 62)),
                    Foreground   = Brushes.White,
                    BorderBrush  = new SolidColorBrush(Color.FromArgb(255, 68, 68, 102)),
                    Padding      = new Thickness(5, 3, 5, 3),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    ToolTip      = "Use : between values. Example: 1:1:1"
                };
                txtRatios.LostFocus += (_, _) => ParseRatios(txtRatios, row);
                txtRatios.LostFocus += (_, _) => { if (_previewRowIndex == rowIndex) RenderOrientationPreview(); };
                widthPanel.Children.Add(txtRatios);
                Grid.SetColumn(widthPanel, 1);
                body.Children.Add(widthPanel);

                var heightPanel = new StackPanel();
                heightPanel.Children.Add(new TextBlock
                {
                    Text = "Height (H, optional)",
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 180, 180, 220)),
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 4),
                    ToolTip = "Vertical split ratios. Empty = full height"
                });

                var txtHeight = new TextBox
                {
                    Text         = row.HasHeightSplit ? string.Join(" : ", row.HeightRatios!) : "",
                    Height       = 30,
                    FontSize     = 13,
                    Background   = new SolidColorBrush(Color.FromArgb(255, 42, 42, 62)),
                    Foreground   = Brushes.White,
                    BorderBrush  = new SolidColorBrush(Color.FromArgb(255, 68, 68, 102)),
                    Padding      = new Thickness(5, 3, 5, 3),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    ToolTip      = "Examples: 1:1 (top/bottom), 1:1:1 (thirds), empty (full height)"
                };
                txtHeight.LostFocus += (_, _) => ParseHeightRatios(txtHeight, row);
                txtHeight.LostFocus += (_, _) => { if (_previewRowIndex == rowIndex) RenderOrientationPreview(); };
                heightPanel.Children.Add(txtHeight);

                var heightQuick = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                var btnFullHeight = CreateSmallButton("Full", "No height split", 54);
                btnFullHeight.Click += (_, _) =>
                {
                    row.HeightRatios = null;
                    txtHeight.Text = "";
                    txtHeight.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 68, 68, 102));
                    if (_previewRowIndex == rowIndex) RenderOrientationPreview();
                };
                heightQuick.Children.Add(btnFullHeight);

                var btnTopBottom = CreateSmallButton("1:1", "Top/Bottom split", 50);
                btnTopBottom.Click += (_, _) =>
                {
                    row.HeightRatios = new List<int> { 1, 1 };
                    txtHeight.Text = "1 : 1";
                    txtHeight.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 68, 68, 102));
                    if (_previewRowIndex == rowIndex) RenderOrientationPreview();
                };
                heightQuick.Children.Add(btnTopBottom);

                var btnThirds = CreateSmallButton("1:1:1", "Three vertical splits", 64);
                btnThirds.Click += (_, _) =>
                {
                    row.HeightRatios = new List<int> { 1, 1, 1 };
                    txtHeight.Text = "1 : 1 : 1";
                    txtHeight.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 68, 68, 102));
                    if (_previewRowIndex == rowIndex) RenderOrientationPreview();
                };
                heightQuick.Children.Add(btnThirds);
                heightPanel.Children.Add(heightQuick);

                Grid.SetColumn(heightPanel, 2);
                body.Children.Add(heightPanel);

                rowStack.Children.Add(body);

                rowPanel.Child = rowStack;
                RowsPanel.Children.Add(rowPanel);
            }

            if (_config.Rows.Count == 0)
            {
                RowsPanel.Children.Add(new TextBlock
                {
                    Text       = "No rows yet. Add one using the buttons below.",
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 120, 120, 150)),
                    FontSize   = 14,
                    Margin     = new Thickness(8, 16, 8, 16),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
            }

            if (_config.Rows.Count == 0)
                _previewRowIndex = -1;
            else if (_previewRowIndex < 0 || _previewRowIndex >= _config.Rows.Count)
                _previewRowIndex = 0;

            RenderOrientationPreview();
        }

        private Button CreateSmallButton(string text, string tooltip, double width = 30)
        {
            return new Button
            {
                Content    = text,
                Width      = width,
                Height     = 28,
                FontSize   = 12,
                Margin     = new Thickness(3, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromArgb(255, 45, 45, 68)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 68, 68, 102)),
                Cursor     = System.Windows.Input.Cursors.Hand,
                ToolTip    = tooltip
            };
        }

        private void MoveRow(int index, int direction)
        {
            int newIndex = index + direction;
            if (newIndex < 0 || newIndex >= _config.Rows.Count) return;
            var tmp = _config.Rows[index];
            _config.Rows[index] = _config.Rows[newIndex];
            _config.Rows[newIndex] = tmp;

            if (_previewRowIndex == index) _previewRowIndex = newIndex;
            else if (_previewRowIndex == newIndex) _previewRowIndex = index;

            RebuildRowsUI();
        }

        private void DuplicateRow(int index)
        {
            if (index < 0 || index >= _config.Rows.Count) return;

            var source = _config.Rows[index];
            var copy = new GridRowDef
            {
                Name = source.Name + " Copy",
                Ratios = new List<int>(source.Ratios),
                HeightRatios = source.HeightRatios != null ? new List<int>(source.HeightRatios) : null
            };

            _config.Rows.Insert(index + 1, copy);
            _previewRowIndex = index + 1;
            RebuildRowsUI();
        }

        private void RenderOrientationPreview()
        {
            if (PreviewCanvas == null || TxtPreviewName == null)
                return;

            PreviewCanvas.Children.Clear();

            if (_previewRowIndex < 0 || _previewRowIndex >= _config.Rows.Count)
            {
                TxtPreviewName.Text = "Select a row to preview";
                return;
            }

            var row = _config.Rows[_previewRowIndex];
            TxtPreviewName.Text = row.Name;

            double width = Math.Max(240, PreviewCanvas.ActualWidth > 1 ? PreviewCanvas.ActualWidth - 4 : 280);
            double height = Math.Max(180, PreviewCanvas.ActualHeight > 1 ? PreviewCanvas.ActualHeight - 4 : 220);
            const double pad = 6;

            var frame = new Border
            {
                Width = width,
                Height = height,
                BorderBrush = new SolidColorBrush(Color.FromArgb(130, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255)),
                CornerRadius = new CornerRadius(4)
            };
            Canvas.SetLeft(frame, 2);
            Canvas.SetTop(frame, 2);
            PreviewCanvas.Children.Add(frame);

            var heights = row.HasHeightSplit ? row.HeightRatios! : new List<int> { 1 };
            int totalCols = Math.Max(1, row.Ratios.Sum());
            int totalHeights = Math.Max(1, heights.Sum());

            var palette = new[]
            {
                Color.FromArgb(210, 80, 160, 255),
                Color.FromArgb(210, 86, 201, 156),
                Color.FromArgb(210, 245, 166, 35),
                Color.FromArgb(210, 190, 120, 255),
                Color.FromArgb(210, 244, 98, 146),
                Color.FromArgb(210, 96, 190, 255)
            };

            double x = 2 + pad;
            for (int c = 0; c < row.Ratios.Count; c++)
            {
                double colW = (width - pad * 2) * row.Ratios[c] / totalCols;
                double y = 2 + pad;
                for (int r = 0; r < heights.Count; r++)
                {
                    double cellH = (height - pad * 2) * heights[r] / totalHeights;
                    var rect = new Rectangle
                    {
                        Width = Math.Max(8, colW - 2),
                        Height = Math.Max(8, cellH - 2),
                        RadiusX = 3,
                        RadiusY = 3,
                        Fill = new SolidColorBrush(palette[(c + r) % palette.Length]),
                        Stroke = new SolidColorBrush(Color.FromArgb(140, 15, 15, 30)),
                        StrokeThickness = 1
                    };

                    Canvas.SetLeft(rect, x + 1);
                    Canvas.SetTop(rect, y + 1);
                    PreviewCanvas.Children.Add(rect);
                    y += cellH;
                }
                x += colW;
            }
        }

        private static void ParseRatios(TextBox txt, GridRowDef row)
        {
            var parts = txt.Text
                .Replace(",", ":")
                .Replace(" ", "")
                .Split(':', StringSplitOptions.RemoveEmptyEntries);

            var parsed = new List<int>();
            foreach (var p in parts)
            {
                if (int.TryParse(p, out int v) && v > 0)
                    parsed.Add(v);
            }

            if (parsed.Count >= 1)
            {
                row.Ratios = parsed;
                txt.Text = string.Join(" : ", parsed);
                txt.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 68, 68, 102));
            }
            else
            {
                txt.BorderBrush = Brushes.Red;
            }
        }

        private static void ParseHeightRatios(TextBox txt, GridRowDef row)
        {
            var text = txt.Text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                row.HeightRatios = null;
                txt.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 68, 68, 102));
                return;
            }

            var parts = text
                .Replace(",", ":")
                .Replace(" ", "")
                .Split(':', StringSplitOptions.RemoveEmptyEntries);

            var parsed = new List<int>();
            foreach (var p in parts)
            {
                if (int.TryParse(p, out int v) && v > 0)
                    parsed.Add(v);
            }

            if (parsed.Count >= 1)
            {
                row.HeightRatios = parsed.Count == 1 ? null : parsed;
                txt.Text = parsed.Count == 1 ? "" : string.Join(" : ", parsed);
                txt.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 68, 68, 102));
            }
            else
            {
                txt.BorderBrush = Brushes.Red;
            }
        }

        // ── Preset buttons ──────────────────────────────────────────────

        private void AddRowDef(string name, int[]? heightRatios, params int[] ratios)
        {
            _config.Rows.Add(new GridRowDef
            {
                Name = name,
                Ratios = ratios.ToList(),
                HeightRatios = heightRatios?.ToList()
            });
            RebuildRowsUI();
        }

        private void OnAddRow(object sender, RoutedEventArgs e)         => AddRowDef("Custom", null, 1, 1);
        private void OnPresetHalves(object sender, RoutedEventArgs e)   => AddRowDef("HALVES", null, 1, 1);
        private void OnPresetHalvesHalf(object sender, RoutedEventArgs e) => AddRowDef("HALVES ½H", new[] { 1, 1 }, 1, 1);
        private void OnPresetThirds(object sender, RoutedEventArgs e)   => AddRowDef("THIRDS", null, 1, 1, 1);
        private void OnPresetThirdsHalf(object sender, RoutedEventArgs e) => AddRowDef("THIRDS ½H", new[] { 1, 1 }, 1, 1, 1);
        private void OnPreset43Left(object sender, RoutedEventArgs e)   => AddRowDef("4:3 LEFT", null, 4, 3);
        private void OnPreset43Center(object sender, RoutedEventArgs e) => AddRowDef("4:3 CENTER", null, 3, 4, 3);
        private void OnPreset43Right(object sender, RoutedEventArgs e)  => AddRowDef("4:3 RIGHT", null, 3, 4);
        private void OnPresetQuarters(object sender, RoutedEventArgs e) => AddRowDef("QUARTERS", null, 1, 1, 1, 1);
        private void OnPresetQuartersHalf(object sender, RoutedEventArgs e) => AddRowDef("QUARTERS ½H", new[] { 1, 1 }, 1, 1, 1, 1);
        private void OnPresetFifths(object sender, RoutedEventArgs e)   => AddRowDef("FIFTHS", null, 1, 1, 1, 1, 1);
        private void OnPresetFifthsHalf(object sender, RoutedEventArgs e) => AddRowDef("FIFTHS ½H", new[] { 1, 1 }, 1, 1, 1, 1, 1);

        private void OnPresetCustom(object sender, RoutedEventArgs e)
        {
            var dlg = new CustomRatioDialog();
            dlg.Owner = this;
            if (dlg.ShowDialog() == true && dlg.ResultRatios.Count > 0)
            {
                string name = dlg.ResultName;
                if (string.IsNullOrWhiteSpace(name))
                    name = string.Join(":", dlg.ResultRatios);
                _config.Rows.Add(new GridRowDef { Name = name, Ratios = dlg.ResultRatios });
                RebuildRowsUI();
            }
        }

        // ── Action buttons ──────────────────────────────────────────────

        private void OnReset(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Reset to the default grid layout?\nAny unsaved changes will be lost.",
                "Reset", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _config = GridConfig.CreateDefault();
                TxtGridName.Text = _config.Name;
                RebuildRowsUI();
            }
        }

        private void OnSaveToFile(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter           = GridConfig.FileFilter,
                DefaultExt       = GridConfig.FileExtension,
                FileName         = SanitizeName(_config.Name),
                InitialDirectory = GridConfig.GetAppDataFolder()
            };
            if (dlg.ShowDialog() == true)
            {
                _config.Name = TxtGridName.Text;
                _config.SaveToFile(dlg.FileName);
                MessageBox.Show($"Grid saved to:\n{dlg.FileName}", "Saved",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnApply(object sender, RoutedEventArgs e)
        {
            if (_config.Rows.Count == 0)
            {
                MessageBox.Show("Add at least one row before applying.", "Empty Grid",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _config.Name = TxtGridName.Text;
            _config.SaveAsActive();
            Applied?.Invoke(_config);
            Close();
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private static GridConfig CloneConfig(GridConfig src)
        {
            var clone = new GridConfig { Name = src.Name };
            foreach (var row in src.Rows)
                clone.Rows.Add(new GridRowDef
                {
                    Name = row.Name,
                    Ratios = new List<int>(row.Ratios),
                    HeightRatios = row.HeightRatios != null ? new List<int>(row.HeightRatios) : null
                });
            return clone;
        }

        private static string SanitizeName(string name)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
