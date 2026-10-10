using System;
using System.Collections.Generic;

namespace SlotGame.Core
{
    public sealed class ReelStrip
    {
        private readonly int[] _symbolIds;

        public int Length => _symbolIds.Length;

        public ReelStrip(int[] symbolIds)
        {
            if (symbolIds == null || symbolIds.Length == 0)
                throw new ArgumentException("A reel strip needs at least one symbol.", nameof(symbolIds));
            _symbolIds = (int[])symbolIds.Clone();
        }

        public int GetSymbolAt(int stopIndex) => _symbolIds[Wrap(stopIndex)];

        public int Wrap(int index)
        {
            int wrapped = index % _symbolIds.Length;
            return wrapped < 0 ? wrapped + _symbolIds.Length : wrapped;
        }

        public int CountOf(int symbolId)
        {
            int count = 0;
            foreach (int id in _symbolIds)
                if (id == symbolId) count++;
            return count;
        }

        public static ReelStrip Build(IEnumerable<SymbolRule> symbols, IRandomProvider random, int minScatterSpacing = 0)
        {
            if (symbols == null) throw new ArgumentNullException(nameof(symbols));
            if (random == null) throw new ArgumentNullException(nameof(random));

            var ids = new List<int>();
            var scatterIds = new HashSet<int>();
            foreach (var symbol in symbols)
            {
                if (symbol.Kind == SymbolKind.Scatter) scatterIds.Add(symbol.Id);
                for (int i = 0; i < symbol.ReelWeight; i++)
                    ids.Add(symbol.Id);
            }

            var array = ids.ToArray();

            const int maxAttempts = 1000;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                Shuffle(array, random);
                if (HasValidSpacing(array, scatterIds, minScatterSpacing))
                    return new ReelStrip(array);
            }

            throw new InvalidOperationException(
                "Could not space scatters on the reel strip; lower the scatter weight or the spacing.");
        }

        private static void Shuffle(int[] array, IRandomProvider random)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
        }

        private static bool HasValidSpacing(int[] strip, HashSet<int> scatterIds, int spacing)
        {
            if (spacing <= 1 || scatterIds.Count == 0) return true;

            for (int i = 0; i < strip.Length; i++)
            {
                if (!scatterIds.Contains(strip[i])) continue;
                for (int offset = 1; offset < spacing; offset++)
                    if (scatterIds.Contains(strip[(i + offset) % strip.Length]))
                        return false;
            }
            return true;
        }
    }
}
