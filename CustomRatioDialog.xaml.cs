using System;
using System.Collections.Generic;
using System.Windows;

namespace ScreenGrid
{
    public partial class CustomRatioDialog : Window
    {
        public List<int> ResultRatios { get; private set; } = new();
        public string ResultName { get; private set; } = string.Empty;

        public CustomRatioDialog()
        {
            InitializeComponent();
            TxtRatios.Text = "1:1";
            TxtName.Text = "Custom";
        }

        private void OnAdd(object sender, RoutedEventArgs e)
        {
            var parts = TxtRatios.Text
                .Replace(",", ":")
                .Replace(" ", "")
                .Split(':', StringSplitOptions.RemoveEmptyEntries);

            var parsed = new List<int>();
            foreach (var p in parts)
            {
                if (int.TryParse(p, out int v) && v > 0)
                    parsed.Add(v);
            }

            if (parsed.Count == 0)
            {
                MessageBox.Show("Enter at least one positive number.\nExample: 2:1 or 3:2:1",
                    "Invalid", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ResultRatios = parsed;
            ResultName = TxtName.Text.Trim();
            DialogResult = true;
            Close();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
