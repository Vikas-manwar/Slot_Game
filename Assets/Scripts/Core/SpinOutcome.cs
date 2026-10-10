using System;

namespace SlotGame.Core
{
    public sealed class SpinOutcome
    {
        public int[] StopIndices { get; }

        public int[,] Window { get; }

        public int ReelCount => Window.GetLength(0);
        public int RowCount => Window.GetLength(1);

        public SpinOutcome(int[] stopIndices, int[,] window)
        {
            StopIndices = stopIndices ?? throw new ArgumentNullException(nameof(stopIndices));
            Window = window ?? throw new ArgumentNullException(nameof(window));
        }

        public int GetSymbol(int reel, int row) => Window[reel, row];

        public static SpinOutcome FromStops(ReelStrip[] strips, int[] stops, int visibleRows)
        {
            int paylineRow = visibleRows / 2;
            var window = new int[strips.Length, visibleRows];
            for (int reel = 0; reel < strips.Length; reel++)
                for (int row = 0; row < visibleRows; row++)
                    window[reel, row] = strips[reel].GetSymbolAt(stops[reel] + (paylineRow - row));

            return new SpinOutcome((int[])stops.Clone(), window);
        }
    }
}
