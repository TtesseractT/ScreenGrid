using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScreenGrid
{
    /// <summary>
    /// A single row in a grid layout, defined as a list of part-ratios.
    /// For example: [1,1,1] = thirds, [4,3] = 4:3, [1,1] = halves.
    /// </summary>
    public class GridRowDef
    {
        /// <summary>Human-readable name for this row, e.g. "Thirds" or "4:3".</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Column ratios. Equal values = equal columns.
        /// E.g. [1,1,1] = 3 equal columns, [4,3] = 4:3 split, [2,1] = 2:1 split.
        /// </summary>
        public List<int> Ratios { get; set; } = new();

        /// <summary>Creates the display label for a column (e.g. "1/3" or "4").</summary>
        public string GetColumnLabel(int colIndex)
        {
            // If all ratios are equal, show "col/total" style
            bool allEqual = true;
            for (int i = 1; i < Ratios.Count; i++)
                if (Ratios[i] != Ratios[0]) { allEqual = false; break; }

            if (allEqual)
                return $"{colIndex + 1}/{Ratios.Count}";

            return Ratios[colIndex].ToString();
        }
    }

    /// <summary>
    /// A complete grid configuration (set of rows) that can be saved/loaded as JSON.
    /// </summary>
    public class GridConfig
    {
        /// <summary>Name of this grid layout.</summary>
        public string Name { get; set; } = "Untitled Grid";

        /// <summary>The rows that make up this grid.</summary>
        public List<GridRowDef> Rows { get; set; } = new();

        /// <summary>File extension for grid config files.</summary>
        public const string FileExtension = ".screengrid";

        /// <summary>JSON file filter for open/save dialogs.</summary>
        public const string FileFilter = "ScreenGrid Files (*.screengrid)|*.screengrid|JSON Files (*.json)|*.json|All Files (*.*)|*.*";

        // ── Serialization ───────────────────────────────────────────

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public void SaveToFile(string filePath)
        {
            string json = JsonSerializer.Serialize(this, JsonOpts);
            File.WriteAllText(filePath, json);
        }

        public static GridConfig LoadFromFile(string filePath)
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<GridConfig>(json, JsonOpts)
                   ?? throw new InvalidOperationException("Failed to deserialize grid config.");
        }

        // ── Defaults ────────────────────────────────────────────────

        /// <summary>The built-in default grid layout.</summary>
        public static GridConfig CreateDefault()
        {
            return new GridConfig
            {
                Name = "Default",
                Rows = new List<GridRowDef>
                {
                    new() { Name = "HALVES",   Ratios = new List<int> { 1, 1 } },
                    new() { Name = "THIRDS",   Ratios = new List<int> { 1, 1, 1 } },
                    new() { Name = "4 : 3",    Ratios = new List<int> { 4, 3 } },
                    new() { Name = "QUARTERS", Ratios = new List<int> { 1, 1, 1, 1 } },
                    new() { Name = "FIFTHS",   Ratios = new List<int> { 1, 1, 1, 1, 1 } },
                }
            };
        }

        // ── App data directory ──────────────────────────────────────

        /// <summary>Gets the app data folder, creating it if needed.</summary>
        public static string GetAppDataFolder()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ScreenGrid");
            Directory.CreateDirectory(folder);
            return folder;
        }

        /// <summary>Path to the active grid config file.</summary>
        public static string GetActiveConfigPath()
            => Path.Combine(GetAppDataFolder(), "active.screengrid");

        /// <summary>
        /// Loads the active grid config, or returns the default if none exists.
        /// </summary>
        public static GridConfig LoadActive()
        {
            string path = GetActiveConfigPath();
            if (File.Exists(path))
            {
                try { return LoadFromFile(path); }
                catch { /* fall through to default */ }
            }
            return CreateDefault();
        }

        /// <summary>Saves this config as the active layout.</summary>
        public void SaveAsActive()
        {
            SaveToFile(GetActiveConfigPath());
        }
    }
}
