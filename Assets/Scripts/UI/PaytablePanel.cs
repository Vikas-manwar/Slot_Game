using System;
using System.Linq;
using SlotGame.Core;
using SlotGame.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.UI
{
    public sealed class PaytablePanel : PopupView
    {
        [SerializeField] private RectTransform rowContainer;
        [SerializeField] private PaytableRowView rowPrefab;
        [SerializeField] private Text rulesText;
        [SerializeField] private Button closeButton;

        public event Action ButtonPressed;

        private bool _built;

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() =>
            {
                ButtonPressed?.Invoke();
                Close();
            });
        }

        public void Build(SlotConfig config)
        {
            if (_built) return;
            _built = true;

            var ordered = config.Symbols
                .Where(s => s != null)
                .OrderBy(s => s.Kind == SymbolKind.Scatter ? 1 : 0)
                .ThenByDescending(s => s.ThreeOfAKindMultiplier);

            foreach (var symbol in ordered)
            {
                var row = Instantiate(rowPrefab, rowContainer);
                row.Bind(symbol, DescribePayout(symbol, config));
            }

            rulesText.text =
                "3 matching symbols on the centre line win  •  " +
                $"each WILD substitutes and multiplies the win x{config.WildWinMultiplier}  •  " +
                $"{config.FreeSpinWinMultiplier}x wins during free spins";
        }

        private static string DescribePayout(SymbolData symbol, SlotConfig config)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Scatter:
                    return $"{config.ScattersForFreeSpins} ANYWHERE = {config.FreeSpinsAwarded} FREE SPINS + x{config.ScatterPayMultiplier}";
                case SymbolKind.Wild:
                    return $"3 = x{symbol.ThreeOfAKindMultiplier}   SUBSTITUTES";
                default:
                    return $"3 = x{symbol.ThreeOfAKindMultiplier}";
            }
        }
    }
}
