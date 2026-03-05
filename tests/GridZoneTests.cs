using System.Windows;
using Xunit;

namespace ScreenGrid.Tests
{
    public class GridZoneTests
    {
        [Fact]
        public void Defaults_LabelIsEmpty()
        {
            var zone = new GridZone();
            Assert.Equal(string.Empty, zone.Label);
        }

        [Fact]
        public void Defaults_IsHighlighted_IsFalse()
        {
            var zone = new GridZone();
            Assert.False(zone.IsHighlighted);
        }

        [Fact]
        public void Defaults_DisplayBounds_IsZero()
        {
            var zone = new GridZone();
            Assert.Equal(new Rect(0, 0, 0, 0), zone.DisplayBounds);
        }

        [Fact]
        public void Defaults_SnapBounds_IsZero()
        {
            var zone = new GridZone();
            Assert.Equal(new Rect(0, 0, 0, 0), zone.SnapBounds);
        }

        [Fact]
        public void Properties_CanBeSetAndRead()
        {
            var zone = new GridZone
            {
                DisplayBounds = new Rect(0, 0, 100, 200),
                SnapBounds    = new Rect(0, 0, 100, 1440),
                Label         = "1/3",
                Row           = 0,
                Column        = 0,
                TotalColumns  = 3,
                IsHighlighted = true
            };

            Assert.Equal(new Rect(0, 0, 100, 200), zone.DisplayBounds);
            Assert.Equal(new Rect(0, 0, 100, 1440), zone.SnapBounds);
            Assert.Equal("1/3", zone.Label);
            Assert.Equal(0, zone.Row);
            Assert.Equal(0, zone.Column);
            Assert.Equal(3, zone.TotalColumns);
            Assert.True(zone.IsHighlighted);
        }

        [Fact]
        public void SnapBounds_WidthMatchesExpected_ForThirds()
        {
            // Simulate a 5120px wide screen split into thirds
            int screenW = 5120;
            int colW = screenW / 3;

            var zone = new GridZone
            {
                SnapBounds = new Rect(0, 0, colW, 1440)
            };

            Assert.Equal(1706, zone.SnapBounds.Width); // 5120/3 = 1706.67 → int 1706
        }

        [Fact]
        public void SnapBounds_WidthMatchesExpected_ForQuarters()
        {
            int screenW = 5120;
            int colW = screenW / 4;

            var zone = new GridZone
            {
                SnapBounds = new Rect(0, 0, colW, 1440)
            };

            Assert.Equal(1280, zone.SnapBounds.Width);
        }

        [Fact]
        public void SnapBounds_WidthMatchesExpected_ForFifths()
        {
            int screenW = 5120;
            int colW = screenW / 5;

            var zone = new GridZone
            {
                SnapBounds = new Rect(0, 0, colW, 1440)
            };

            Assert.Equal(1024, zone.SnapBounds.Width);
        }

        [Fact]
        public void SnapBounds_4to3Ratio_ProducesCorrectWidths()
        {
            int screenW = 5120;
            int w4 = (int)(4.0 / 7 * screenW); // 4/(4+3) * 5120 = 2925.7 → 2925
            int w3 = screenW - w4;              // 2195

            var zone4 = new GridZone { SnapBounds = new Rect(0, 0, w4, 1440) };
            var zone3 = new GridZone { SnapBounds = new Rect(w4, 0, w3, 1440) };

            Assert.Equal(2925, zone4.SnapBounds.Width);
            Assert.Equal(2195, zone3.SnapBounds.Width);
            Assert.Equal(screenW, (int)(zone4.SnapBounds.Width + zone3.SnapBounds.Width));
        }
    }
}
