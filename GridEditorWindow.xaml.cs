using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScreenGrid
{
    public partial class GridEditorWindow : Window
    {
        private GridConfig _config;

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
                    Padding         = new Thickness(10, 8, 10, 8)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

                // Row number
                var lblNum = new TextBlock
                {
                    Text              = $"#{i + 1}",
                    Foreground        = new SolidColorBrush(Color.FromArgb(255, 120, 120, 170)),
                    FontSize          = 13,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin            = new Thickness(0, 0, 10, 0)
                };
                Grid.SetColumn(lblNum, 0);
                grid.Children.Add(lblNum);

                // Name input
                var txtName = new TextBox
                {
                    Text         = row.Name,
                    Width        = 120,
                    Height       = 28,
                    FontSize     = 13,
                    Background   = new SolidColorBrush(Color.FromArgb(255, 42, 42, 62)),
                    Foreground   = Brushes.White,
                    BorderBrush  = new SolidColorBrush(Color.FromArgb(255, 68, 68, 102)),
                    Padding      = new Thickness(5, 3, 5, 3),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Margin       = new Thickness(0, 0, 10, 0)
                };
                txtName.TextChanged += (_, _) => row.Name = txtName.Text;
                Grid.SetColumn(txtName, 1);
                grid.Children.Add(txtName);

                // Ratios display + edit
                var ratiosPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                var lblRatios = new TextBlock
                {
                    Text              = "Ratios:",
                    Foreground        = new SolidColorBrush(Color.FromArgb(255, 160, 160, 200)),
                    FontSize          = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin            = new Thickness(0, 0, 6, 0)
                };
                ratiosPanel.Children.Add(lblRatios);

                var txtRatios = new TextBox
                {
                    Text         = string.Join(" : ", row.Ratios),
                    Width        = 160,
                    Height       = 28,
                    FontSize     = 13,
                    Background   = new SolidColorBrush(Color.FromArgb(255, 42, 42, 62)),
                    Foreground   = Brushes.White,
                    BorderBrush  = new SolidColorBrush(Color.FromArgb(255, 68, 68, 102)),
                    Padding      = new Thickness(5, 3, 5, 3),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    ToolTip      = "Column ratios separated by : (e.g. 1:1:1 or 4:3)"
                };
                txtRatios.LostFocus += (_, _) => ParseRatios(txtRatios, row);
                ratiosPanel.Children.Add(txtRatios);

                Grid.SetColumn(ratiosPanel, 2);
                grid.Children.Add(ratiosPanel);

                // Move up
                var btnUp = CreateSmallButton("\u25B2", "Move up");
                btnUp.Click += (_, _) => { MoveRow(rowIndex, -1); };
                btnUp.IsEnabled = i > 0;
                Grid.SetColumn(btnUp, 3);
                grid.Children.Add(btnUp);

                // Move down
                var btnDown = CreateSmallButton("\u25BC", "Move down");
                btnDown.Click += (_, _) => { MoveRow(rowIndex, 1); };
                btnDown.IsEnabled = i < _config.Rows.Count - 1;
                Grid.SetColumn(btnDown, 4);
                grid.Children.Add(btnDown);

                // Delete
                var btnDel = CreateSmallButton("\u2716", "Remove row");
                btnDel.Click += (_, _) =>
                {
                    _config.Rows.RemoveAt(rowIndex);
                    RebuildRowsUI();
                };
                Grid.SetColumn(btnDel, 5);
                grid.Children.Add(btnDel);

                rowPanel.Child = grid;
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
        }

        private Button CreateSmallButton(string text, string tooltip)
        {
            return new Button
            {
                Content    = text,
                Width      = 30,
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
            RebuildRowsUI();
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

        // ── Preset buttons ──────────────────────────────────────────────

        private void AddRowDef(string name, params int[] ratios)
        {
            _config.Rows.Add(new GridRowDef { Name = name, Ratios = ratios.ToList() });
            RebuildRowsUI();
        }

        private void OnAddRow(object sender, RoutedEventArgs e)     => AddRowDef("Custom", 1, 1);
        private void OnPresetHalves(object sender, RoutedEventArgs e)   => AddRowDef("HALVES", 1, 1);
        private void OnPresetThirds(object sender, RoutedEventArgs e)   => AddRowDef("THIRDS", 1, 1, 1);
        private void OnPreset43(object sender, RoutedEventArgs e)       => AddRowDef("4 : 3", 4, 3);
        private void OnPresetQuarters(object sender, RoutedEventArgs e) => AddRowDef("QUARTERS", 1, 1, 1, 1);
        private void OnPresetFifths(object sender, RoutedEventArgs e)   => AddRowDef("FIFTHS", 1, 1, 1, 1, 1);

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
                clone.Rows.Add(new GridRowDef { Name = row.Name, Ratios = new List<int>(row.Ratios) });
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
