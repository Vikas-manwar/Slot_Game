using System;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.UI
{
    public sealed class ConfirmDialog : PopupView
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text messageText;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;

        private Action _onYes;
        private Action _onNo;

        public event Action ButtonPressed;

        protected override void Awake()
        {
            base.Awake();
            yesButton.onClick.AddListener(() => Resolve(_onYes));
            noButton.onClick.AddListener(() => Resolve(_onNo));
        }

        public void Show(string title, string message, Action onYes, Action onNo = null)
        {
            titleText.text = title;
            messageText.text = message;
            _onYes = onYes;
            _onNo = onNo;
            Open();
        }

        private void Resolve(Action callback)
        {
            if (!IsOpen) return;
            ButtonPressed?.Invoke();
            Close();
            _onYes = _onNo = null;
            callback?.Invoke();
        }
    }
}
