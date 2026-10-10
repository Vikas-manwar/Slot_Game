using System.Collections.Generic;

namespace SlotGame.Core
{
    public enum WinTier
    {
        None = 0,
        Small = 1,
        Big = 2,
        Mega = 3
    }

    public readonly struct CellPosition
    {
        public readonly int Reel;
        public readonly int Row;

        public CellPosition(int reel, int row)
        {
            Reel = reel;
            Row = row;
        }
    }

    public sealed class WinResult
    {
        public int Bet { get; internal set; }

        public int LineWin { get; internal set; }

        public int ScatterWin { get; internal set; }

        public int TotalWin => LineWin + ScatterWin;

        public int WinningSymbolId { get; internal set; } = -1;

        public int WildsUsed { get; internal set; }

        public int AppliedMultiplier { get; internal set; } = 1;

        public int ScatterCount { get; internal set; }
        public int FreeSpinsAwarded { get; internal set; }

        public bool IsLineWin => LineWin > 0;
        public bool IsWin => TotalWin > 0 || FreeSpinsAwarded > 0;

        public List<CellPosition> WinningCells { get; } = new List<CellPosition>();

        public WinTier Tier
        {
            get
            {
                if (TotalWin <= 0 || Bet <= 0) return WinTier.None;
                int ratio = TotalWin / Bet;
                if (ratio >= 20) return WinTier.Mega;
                if (ratio >= 5) return WinTier.Big;
                return WinTier.Small;
            }
        }
    }
}
