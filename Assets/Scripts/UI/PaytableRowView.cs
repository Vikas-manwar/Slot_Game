using SlotGame.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.UI
{
    public sealed class PaytableRowView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Text nameText;
        [SerializeField] private Text payoutText;

        public void Bind(SymbolData symbol, string payout)
        {
            iconImage.sprite = symbol.Sprite;
            iconImage.preserveAspect = true;
            nameText.text = symbol.DisplayName.ToUpperInvariant();
            payoutText.text = payout;
        }
    }
}
