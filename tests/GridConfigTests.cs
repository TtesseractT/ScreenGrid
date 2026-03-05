using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ScreenGrid.Tests
{
    public class GridConfigTests : IDisposable
    {
        private readonly string _tempDir;

        public GridConfigTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "ScreenGrid_Tests_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }

        // ── CreateDefault ───────────────────────────────────────────

        [Fact]
        public void CreateDefault_ReturnsNonNull()
        {
            var config = GridConfig.CreateDefault();
            Assert.NotNull(config);
        }

        [Fact]
        public void CreateDefault_NameIsDefault()
        {
            var config = GridConfig.CreateDefault();
            Assert.Equal("Default", config.Name);
        }

        [Fact]
        public void CreateDefault_HasFiveRows()
        {
            var config = GridConfig.CreateDefault();
            Assert.Equal(5, config.Rows.Count);
        }

        [Fact]
        public void CreateDefault_ContainsExpectedRowNames()
        {
            var config = GridConfig.CreateDefault();
            var names = new List<string>();
            foreach (var r in config.Rows) names.Add(r.Name);

            Assert.Contains("HALVES", names);
            Assert.Contains("THIRDS", names);
            Assert.Contains("4 : 3", names);
            Assert.Contains("QUARTERS", names);
            Assert.Contains("FIFTHS", names);
        }

        [Fact]
        public void CreateDefault_HalvesRow_HasTwoEqualRatios()
        {
            var config = GridConfig.CreateDefault();
            var halves = config.Rows[0];
            Assert.Equal("HALVES", halves.Name);
            Assert.Equal(new List<int> { 1, 1 }, halves.Ratios);
        }

        [Fact]
        public void CreateDefault_ThirdsRow_HasThreeEqualRatios()
        {
            var config = GridConfig.CreateDefault();
            var thirds = config.Rows[1];
            Assert.Equal("THIRDS", thirds.Name);
            Assert.Equal(new List<int> { 1, 1, 1 }, thirds.Ratios);
        }

        [Fact]
        public void CreateDefault_FourThreeRow_HasCorrectRatios()
        {
            var config = GridConfig.CreateDefault();
            var row43 = config.Rows[2];
            Assert.Equal("4 : 3", row43.Name);
            Assert.Equal(new List<int> { 4, 3 }, row43.Ratios);
        }

        [Fact]
        public void CreateDefault_QuartersRow_HasFourEqualRatios()
        {
            var config = GridConfig.CreateDefault();
            var quarters = config.Rows[3];
            Assert.Equal("QUARTERS", quarters.Name);
            Assert.Equal(new List<int> { 1, 1, 1, 1 }, quarters.Ratios);
        }

        [Fact]
        public void CreateDefault_FifthsRow_HasFiveEqualRatios()
        {
            var config = GridConfig.CreateDefault();
            var fifths = config.Rows[4];
            Assert.Equal("FIFTHS", fifths.Name);
            Assert.Equal(new List<int> { 1, 1, 1, 1, 1 }, fifths.Ratios);
        }

        // ── Save / Load roundtrip ───────────────────────────────────

        [Fact]
        public void SaveAndLoad_RoundTrips_Name()
        {
            var config = new GridConfig { Name = "Test Layout" };
            config.Rows.Add(new GridRowDef { Name = "Halves", Ratios = [1, 1] });

            string path = Path.Combine(_tempDir, "test.screengrid");
            config.SaveToFile(path);

            var loaded = GridConfig.LoadFromFile(path);
            Assert.Equal("Test Layout", loaded.Name);
        }

        [Fact]
        public void SaveAndLoad_RoundTrips_Rows()
        {
            var config = new GridConfig { Name = "Multi" };
            config.Rows.Add(new GridRowDef { Name = "A", Ratios = [1, 1, 1] });
            config.Rows.Add(new GridRowDef { Name = "B", Ratios = [4, 3] });
            config.Rows.Add(new GridRowDef { Name = "C", Ratios = [2, 1] });

            string path = Path.Combine(_tempDir, "multi.screengrid");
            config.SaveToFile(path);

            var loaded = GridConfig.LoadFromFile(path);
            Assert.Equal(3, loaded.Rows.Count);
            Assert.Equal("A", loaded.Rows[0].Name);
            Assert.Equal(new List<int> { 1, 1, 1 }, loaded.Rows[0].Ratios);
            Assert.Equal("B", loaded.Rows[1].Name);
            Assert.Equal(new List<int> { 4, 3 }, loaded.Rows[1].Ratios);
            Assert.Equal("C", loaded.Rows[2].Name);
            Assert.Equal(new List<int> { 2, 1 }, loaded.Rows[2].Ratios);
        }

        [Fact]
        public void SaveAndLoad_DefaultConfig_RoundTrips()
        {
            var config = GridConfig.CreateDefault();
            string path = Path.Combine(_tempDir, "default.screengrid");
            config.SaveToFile(path);

            var loaded = GridConfig.LoadFromFile(path);
            Assert.Equal(config.Name, loaded.Name);
            Assert.Equal(config.Rows.Count, loaded.Rows.Count);

            for (int i = 0; i < config.Rows.Count; i++)
            {
                Assert.Equal(config.Rows[i].Name, loaded.Rows[i].Name);
                Assert.Equal(config.Rows[i].Ratios, loaded.Rows[i].Ratios);
            }
        }

        [Fact]
        public void SaveAndLoad_EmptyRows_RoundTrips()
        {
            var config = new GridConfig { Name = "Empty" };
            string path = Path.Combine(_tempDir, "empty.screengrid");
            config.SaveToFile(path);

            var loaded = GridConfig.LoadFromFile(path);
            Assert.Equal("Empty", loaded.Name);
            Assert.Empty(loaded.Rows);
        }

        [Fact]
        public void SaveToFile_CreatesFile()
        {
            var config = GridConfig.CreateDefault();
            string path = Path.Combine(_tempDir, "out.screengrid");

            Assert.False(File.Exists(path));
            config.SaveToFile(path);
            Assert.True(File.Exists(path));
        }

        [Fact]
        public void SaveToFile_WritesValidJson()
        {
            var config = GridConfig.CreateDefault();
            string path = Path.Combine(_tempDir, "json.screengrid");
            config.SaveToFile(path);

            string json = File.ReadAllText(path);
            Assert.StartsWith("{", json.TrimStart());

            // Should contain camelCase property names
            Assert.Contains("\"name\"", json);
            Assert.Contains("\"rows\"", json);
            Assert.Contains("\"ratios\"", json);
        }

        // ── Load error handling ─────────────────────────────────────

        [Fact]
        public void LoadFromFile_NonexistentFile_Throws()
        {
            string path = Path.Combine(_tempDir, "nope.screengrid");
            Assert.Throws<FileNotFoundException>(() => GridConfig.LoadFromFile(path));
        }

        [Fact]
        public void LoadFromFile_InvalidJson_Throws()
        {
            string path = Path.Combine(_tempDir, "bad.screengrid");
            File.WriteAllText(path, "not json at all {{{");

            Assert.ThrowsAny<Exception>(() => GridConfig.LoadFromFile(path));
        }

        [Fact]
        public void LoadFromFile_EmptyJsonObject_ReturnsDefaults()
        {
            string path = Path.Combine(_tempDir, "minimal.screengrid");
            File.WriteAllText(path, "{}");

            var loaded = GridConfig.LoadFromFile(path);
            Assert.NotNull(loaded);
            // Default values kick in
            Assert.Equal("Untitled Grid", loaded.Name);
            Assert.NotNull(loaded.Rows);
        }

        // ── Constants ───────────────────────────────────────────────

        [Fact]
        public void FileExtension_IsScreengrid()
        {
            Assert.Equal(".screengrid", GridConfig.FileExtension);
        }

        [Fact]
        public void FileFilter_ContainsScreengrid()
        {
            Assert.Contains(".screengrid", GridConfig.FileFilter);
        }

        // ── New config defaults ─────────────────────────────────────

        [Fact]
        public void NewConfig_DefaultName_IsUntitled()
        {
            var config = new GridConfig();
            Assert.Equal("Untitled Grid", config.Name);
        }

        [Fact]
        public void NewConfig_Rows_IsEmptyList()
        {
            var config = new GridConfig();
            Assert.NotNull(config.Rows);
            Assert.Empty(config.Rows);
        }
    }
}
