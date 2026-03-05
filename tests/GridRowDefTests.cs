using Xunit;

namespace ScreenGrid.Tests
{
    public class GridRowDefTests
    {
        // ── GetColumnLabel ──────────────────────────────────────────

        [Fact]
        public void GetColumnLabel_EqualRatios_ReturnsSlashFormat()
        {
            var row = new GridRowDef { Name = "THIRDS", Ratios = [1, 1, 1] };

            Assert.Equal("1/3", row.GetColumnLabel(0));
            Assert.Equal("2/3", row.GetColumnLabel(1));
            Assert.Equal("3/3", row.GetColumnLabel(2));
        }

        [Fact]
        public void GetColumnLabel_Halves_ReturnsSlashFormat()
        {
            var row = new GridRowDef { Name = "HALVES", Ratios = [1, 1] };

            Assert.Equal("1/2", row.GetColumnLabel(0));
            Assert.Equal("2/2", row.GetColumnLabel(1));
        }

        [Fact]
        public void GetColumnLabel_UnequalRatios_ReturnsRatioValue()
        {
            var row = new GridRowDef { Name = "4:3", Ratios = [4, 3] };

            Assert.Equal("4", row.GetColumnLabel(0));
            Assert.Equal("3", row.GetColumnLabel(1));
        }

        [Fact]
        public void GetColumnLabel_CustomRatios_ReturnsRatioValues()
        {
            var row = new GridRowDef { Name = "Custom", Ratios = [3, 2, 1] };

            Assert.Equal("3", row.GetColumnLabel(0));
            Assert.Equal("2", row.GetColumnLabel(1));
            Assert.Equal("1", row.GetColumnLabel(2));
        }

        [Fact]
        public void GetColumnLabel_SingleColumn_ReturnsOneOfOne()
        {
            var row = new GridRowDef { Name = "Full", Ratios = [1] };

            Assert.Equal("1/1", row.GetColumnLabel(0));
        }

        [Fact]
        public void GetColumnLabel_EqualNonOneRatios_ReturnsSlashFormat()
        {
            // [3, 3, 3] is still equal — should show 1/3 style, not "3"
            var row = new GridRowDef { Name = "Triple3s", Ratios = [3, 3, 3] };

            Assert.Equal("1/3", row.GetColumnLabel(0));
            Assert.Equal("2/3", row.GetColumnLabel(1));
            Assert.Equal("3/3", row.GetColumnLabel(2));
        }

        // ── Defaults ────────────────────────────────────────────────

        [Fact]
        public void Default_Name_IsEmpty()
        {
            var row = new GridRowDef();
            Assert.Equal(string.Empty, row.Name);
        }

        [Fact]
        public void Default_Ratios_IsEmptyList()
        {
            var row = new GridRowDef();
            Assert.NotNull(row.Ratios);
            Assert.Empty(row.Ratios);
        }
    }
}
