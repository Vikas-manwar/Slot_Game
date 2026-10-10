using System.Collections.Generic;
using System.Linq;
using SlotGame.Core;
using UnityEngine;

namespace SlotGame.Data
{
    [CreateAssetMenu(fileName = "SlotConfig", menuName = "Slot Game/Slot Config", order = 1)]
    public sealed class SlotConfig : ScriptableObject
    {
        [Header("Symbols")]
        [SerializeField] private SymbolData[] symbols = new SymbolData[0];

        [Header("Layout")]
        [Min(2)] [SerializeField] private int reelCount = 3;
        [Tooltip("Visible rows per reel (odd). The middle row is the payline.")]
        [Min(1)] [SerializeField] private int visibleRows = 3;

        [Header("Economy")]
        [Min(0)] [SerializeField] private int startingCredits = 1000;
        [SerializeField] private int[] betLevels = { 5, 10, 25, 50, 100 };
        [Min(0)] [SerializeField] private int defaultBetIndex = 1;

        [Header("Bonus Features")]
        [Tooltip("Line win is multiplied by this for each wild that substitutes.")]
        [Min(1)] [SerializeField] private int wildWinMultiplier = 2;
        [Min(1)] [SerializeField] private int scattersForFreeSpins = 3;
        [Min(0)] [SerializeField] private int freeSpinsAwarded = 10;
        [Tooltip("Bet multiplier paid instantly when free spins trigger.")]
        [Min(0)] [SerializeField] private int scatterPayMultiplier = 2;
        [Tooltip("All line wins during free spins are multiplied by this.")]
        [Min(1)] [SerializeField] private int freeSpinWinMultiplier = 2;

        private Dictionary<int, SymbolData> _lookup;

        public IReadOnlyList<SymbolData> Symbols => symbols;
        public int ReelCount => reelCount;
        public int VisibleRows => visibleRows;
        public int StartingCredits => startingCredits;
        public int[] BetLevels => betLevels;
        public int DefaultBetIndex => defaultBetIndex;
        public int WildWinMultiplier => wildWinMultiplier;
        public int ScattersForFreeSpins => scattersForFreeSpins;
        public int FreeSpinsAwarded => freeSpinsAwarded;
        public int ScatterPayMultiplier => scatterPayMultiplier;
        public int FreeSpinWinMultiplier => freeSpinWinMultiplier;

        public SlotRules BuildRules() => new SlotRules(
            symbols.Where(s => s != null).Select(s => s.ToRule()),
            reelCount, visibleRows, wildWinMultiplier, scattersForFreeSpins,
            freeSpinsAwarded, scatterPayMultiplier, freeSpinWinMultiplier);

        public SymbolData GetSymbol(int id)
        {
            if (_lookup == null)
                _lookup = symbols.Where(s => s != null).ToDictionary(s => s.Id);
            return _lookup.TryGetValue(id, out var data) ? data : null;
        }

        private void OnValidate()
        {
            _lookup = null;

            if (visibleRows % 2 == 0) visibleRows++;
            if (betLevels == null || betLevels.Length == 0) betLevels = new[] { 10 };
            defaultBetIndex = Mathf.Clamp(defaultBetIndex, 0, betLevels.Length - 1);

            var duplicate = symbols.Where(s => s != null).GroupBy(s => s.Id).FirstOrDefault(g => g.Count() > 1);
            if (duplicate != null)
                Debug.LogWarning($"[SlotConfig] Duplicate symbol id {duplicate.Key}; ids must be unique.", this);
        }

#if UNITY_EDITOR
        public void EditorInitialize(SymbolData[] newSymbols)
        {
            symbols = newSymbols;
            _lookup = null;
        }
#endif
    }
}
