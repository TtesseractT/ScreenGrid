using System.Windows;

namespace ScreenGrid
{
    /// <summary>
    /// Represents a single snap zone on the overlay grid.
    /// DisplayBounds = position on the overlay (physical pixels, within a row band).
    /// SnapBounds    = where the window will be placed (physical pixels, full work-area height).
    /// </summary>
    public class GridZone
    {
        /// <summary>Where this zone is drawn on the overlay (physical screen pixels).</summary>
        public Rect DisplayBounds { get; set; }

        /// <summary>Where a window snaps to (physical screen pixels, full height).</summary>
        public Rect SnapBounds { get; set; }

        /// <summary>Display label, e.g. "1/3".</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Row index: 0 = thirds, 1 = quarters, 2 = fifths.</summary>
        public int Row { get; set; }

        /// <summary>Column index within the row.</summary>
        public int Column { get; set; }

        /// <summary>Total columns in this row's grid.</summary>
        public int TotalColumns { get; set; }

        /// <summary>Whether this zone is currently highlighted.</summary>
        public bool IsHighlighted { get; set; }
    }
}
