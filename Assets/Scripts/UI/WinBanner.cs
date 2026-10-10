using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.UI
{
    public sealed class WinBanner : MonoBehaviour
    {
        private static readonly int VisibleHash = Animator.StringToHash("Visible");

        [SerializeField] private Animator animator;
        [SerializeField] private Text titleText;
        [SerializeField] private Text amountText;

        public void Show(string title, string amount)
        {
            titleText.text = title;
            amountText.text = amount;
            animator.SetBool(VisibleHash, true);
        }

        public void SetAmount(string amount) => amountText.text = amount;

        public void Hide() => animator.SetBool(VisibleHash, false);
    }
}
