using System;
using System.Collections.Generic;
using System.Linq;

namespace SlotGame.Core
{
    public sealed class SlotRules
    {
        public IReadOnlyList<SymbolRule> Symbols { get; }
        public int ReelCount { get; }

        public int VisibleRows { get; }

        public int WildWinMultiplier { get; }

        public int ScattersForFreeSpins { get; }

        public int FreeSpinsAwarded { get; }

        public int ScatterPayMultiplier { get; }

        public int FreeSpinWinMultiplier { get; }

        public int PaylineRow => VisibleRows / 2;

        private readonly Dictionary<int, SymbolRule> _byId;

        public SlotRules(
            IEnumerable<SymbolRule> symbols,
            int reelCount = 3,
            int visibleRows = 3,
            int wildWinMultiplier = 2,
            int scattersForFreeSpins = 3,
            int freeSpinsAwarded = 8,
            int scatterPayMultiplier = 2,
            int freeSpinWinMultiplier = 2)
        {
            Symbols = symbols?.ToList() ?? throw new ArgumentNullException(nameof(symbols));
            if (Symbols.Count == 0) throw new ArgumentException("At least one symbol is required.", nameof(symbols));
            if (reelCount < 2) throw new ArgumentOutOfRangeException(nameof(reelCount));
            if (visibleRows < 1 || visibleRows % 2 == 0)
                throw new ArgumentOutOfRangeException(nameof(visibleRows), "Use an odd row count so a middle payline exists.");
            if (Symbols.All(s => s.ReelWeight == 0))
                throw new ArgumentException("At least one symbol needs a reel weight above zero.", nameof(symbols));

            _byId = Symbols.ToDictionary(s => s.Id);
            ReelCount = reelCount;
            VisibleRows = visibleRows;
            WildWinMultiplier = Math.Max(1, wildWinMultiplier);
            ScattersForFreeSpins = Math.Max(1, scattersForFreeSpins);
            FreeSpinsAwarded = Math.Max(0, freeSpinsAwarded);
            ScatterPayMultiplier = Math.Max(0, scatterPayMultiplier);
            FreeSpinWinMultiplier = Math.Max(1, freeSpinWinMultiplier);
        }

        public SymbolRule GetSymbol(int id)
        {
            if (!_byId.TryGetValue(id, out var rule))
                throw new KeyNotFoundException($"Unknown symbol id {id}.");
            return rule;
        }
    }
}
