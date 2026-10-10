using SlotGame.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.Reels
{
    [DisallowMultipleComponent]
    public sealed class SymbolView : MonoBehaviour
    {
        private static readonly int IsWinningHash = Animator.StringToHash("IsWinning");

        [SerializeField] private RectTransform content;
        [SerializeField] private Image iconImage;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Animator animator;

        [Tooltip("Alpha of non-winning symbols while a win is being shown.")]
        [Range(0f, 1f)] [SerializeField] private float dimmedAlpha = 0.4f;

        private int _symbolId = int.MinValue;
        private float _motionAlpha = 1f;
        private bool _dimmed;

        public RectTransform RectTransform => (RectTransform)transform;
        public int SymbolId => _symbolId;

        public void SetSymbol(SymbolData data)
        {
            if (data == null || data.Id == _symbolId) return;
            _symbolId = data.Id;
            iconImage.sprite = data.Sprite;
            iconImage.preserveAspect = true;
        }

        public void SetMotion(float speed01, float maxStretch, float minAlpha)
        {
            speed01 = Mathf.Clamp01(speed01);
            content.localScale = new Vector3(1f - maxStretch * 0.3f * speed01, 1f + maxStretch * speed01, 1f);
            _motionAlpha = Mathf.Lerp(1f, minAlpha, speed01);
            ApplyAlpha();
        }

        public void SetWinning(bool winning)
        {
            if (animator != null && animator.isActiveAndEnabled)
                animator.SetBool(IsWinningHash, winning);
        }

        public void SetDimmed(bool dimmed)
        {
            _dimmed = dimmed;
            ApplyAlpha();
        }

        private void ApplyAlpha()
        {
            if (canvasGroup != null)
                canvasGroup.alpha = _motionAlpha * (_dimmed ? dimmedAlpha : 1f);
        }
    }
}
