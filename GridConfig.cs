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

        /// <summary>
        /// Height ratios for vertical subdivision within each column.
        /// Null or single entry = full screen height (default).
        /// E.g. [1,1] = top/bottom halves, [1,1,1] = top/mid/bottom thirds.
        /// </summary>
        public List<int>? HeightRatios { get; set; }

        /// <summary>Whether this row has vertical subdivisions.</summary>
        [JsonIgnore]
        public bool HasHeightSplit => HeightRatios != null && HeightRatios.Count > 1;

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

        /// <summary>Creates the display label for a vertical sub-row (e.g. "Top", "Mid", "Bot").</summary>
        public string GetHeightLabel(int vRowIndex)
        {
            if (HeightRatios == null || HeightRatios.Count <= 1)
                return string.Empty;

            bool allEqual = true;
            for (int i = 1; i < HeightRatios.Count; i++)
                if (HeightRatios[i] != HeightRatios[0]) { allEqual = false; break; }

            if (allEqual)
            {
                if (HeightRatios.Count == 2)
                    return vRowIndex == 0 ? "Top" : "Bot";
                if (HeightRatios.Count == 3)
                    return vRowIndex == 0 ? "Top" : vRowIndex == 1 ? "Mid" : "Bot";
                return $"V{vRowIndex + 1}/{HeightRatios.Count}";
            }

            return HeightRatios[vRowIndex].ToString();
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
                    new() { Name = "HALVES",      Ratios = new List<int> { 1, 1 } },
                    new() { Name = "THIRDS",      Ratios = new List<int> { 1, 1, 1 } },
                    new() { Name = "4:3 LEFT",    Ratios = new List<int> { 4, 3 } },
                    new() { Name = "4:3 CENTER",  Ratios = new List<int> { 3, 4, 3 } },
                    new() { Name = "4:3 RIGHT",   Ratios = new List<int> { 3, 4 } },
                    new() { Name = "QUARTERS",    Ratios = new List<int> { 1, 1, 1, 1 } },
                    new() { Name = "FIFTHS",      Ratios = new List<int> { 1, 1, 1, 1, 1 } },
                    new() { Name = "TOP / BOTTOM", Ratios = new List<int> { 1 },       HeightRatios = new List<int> { 1, 1 } },
                    new() { Name = "HEIGHT ⅓",    Ratios = new List<int> { 1 },       HeightRatios = new List<int> { 1, 1, 1 } },
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
