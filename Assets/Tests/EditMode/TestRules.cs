using SlotGame.Core;

namespace SlotGame.Tests
{
    internal static class TestRules
    {
        public const int Cherry = 0, Bell = 1, Bar = 2, Seven = 3, Wild = 4, Scatter = 5;

        public static SlotRules Create() => new SlotRules(new[]
        {
            new SymbolRule(Cherry, "Cherry", SymbolKind.Regular, 3, 10),
            new SymbolRule(Bell, "Bell", SymbolKind.Regular, 6, 8),
            new SymbolRule(Bar, "Bar", SymbolKind.Regular, 12, 6),
            new SymbolRule(Seven, "Seven", SymbolKind.Regular, 35, 4),
            new SymbolRule(Wild, "Wild", SymbolKind.Wild, 100, 1),
            new SymbolRule(Scatter, "Scatter", SymbolKind.Scatter, 0, 2),
        }, reelCount: 3, visibleRows: 3, wildWinMultiplier: 2, scattersForFreeSpins: 3,
           freeSpinsAwarded: 10, scatterPayMultiplier: 2, freeSpinWinMultiplier: 2);

        public static SpinOutcome Window(int[,] rowsTopToBottom)
        {
            int rows = rowsTopToBottom.GetLength(0), reels = rowsTopToBottom.GetLength(1);
            var window = new int[reels, rows];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < reels; c++)
                    window[c, r] = rowsTopToBottom[r, c];
            return new SpinOutcome(new int[reels], window);
        }
    }
}
