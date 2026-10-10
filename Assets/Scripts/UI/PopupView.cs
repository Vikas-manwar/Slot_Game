using UnityEngine;

namespace SlotGame.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class PopupView : MonoBehaviour
    {
        private static readonly int IsOpenHash = Animator.StringToHash("IsOpen");

        [SerializeField] private Animator animator;
        private CanvasGroup _canvasGroup;

        public bool IsOpen { get; private set; }

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            IsOpen = false;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            if (animator == null) _canvasGroup.alpha = 0f;
        }

        public virtual void Open()
        {
            gameObject.SetActive(true);
            ApplyOpenState(true);
        }

        public virtual void Close() => ApplyOpenState(false);

        private void ApplyOpenState(bool open)
        {
            IsOpen = open;
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.blocksRaycasts = open;
            _canvasGroup.interactable = open;

            if (animator != null) animator.SetBool(IsOpenHash, open);
            else _canvasGroup.alpha = open ? 1f : 0f;
        }
    }
}
