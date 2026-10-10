using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SlotGame.UI
{
    public sealed class SlotHUD : MonoBehaviour
    {
        [Header("Readouts")]
        [SerializeField] private Text balanceText;
        [SerializeField] private Text betText;
        [SerializeField] private Text winText;
        [SerializeField] private Text messageText;
        [SerializeField] private GameObject freeSpinsBadge;
        [SerializeField] private Text freeSpinsText;

        [Header("Buttons")]
        [SerializeField] private Button spinButton;
        [SerializeField] private Text spinButtonLabel;
        [SerializeField] private Button betDownButton;
        [SerializeField] private Button betUpButton;
        [SerializeField] private Button paytableButton;
        [SerializeField] private Button muteButton;
        [SerializeField] private Text muteButtonLabel;

        public event Action SpinPressed;
        public event Action BetDownPressed;
        public event Action BetUpPressed;
        public event Action PaytablePressed;
        public event Action MutePressed;

        private void Awake()
        {
            Bind(spinButton, () => SpinPressed?.Invoke());
            Bind(betDownButton, () => BetDownPressed?.Invoke());
            Bind(betUpButton, () => BetUpPressed?.Invoke());
            Bind(paytableButton, () => PaytablePressed?.Invoke());
            if (muteButton != null) Bind(muteButton, () => MutePressed?.Invoke());
        }

        private static void Bind(Button button, System.Action action)
        {
            button.onClick.AddListener(() =>
            {
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                action();
            });
        }

        public void SetBalance(int credits) => balanceText.text = FormatCredits(credits);
        public void SetBet(int bet) => betText.text = FormatCredits(bet);
        public void SetWin(int win) => winText.text = win > 0 ? FormatCredits(win) : "0";
        public void SetMessage(string message) => messageText.text = message;

        public void SetFreeSpins(int remaining)
        {
            freeSpinsBadge.SetActive(remaining > 0);
            freeSpinsText.text = $"FREE SPINS: {remaining}";
        }

        public void SetBetControls(bool canChangeBet, bool canBetDown, bool canBetUp)
        {
            betDownButton.interactable = canChangeBet && canBetDown;
            betUpButton.interactable = canChangeBet && canBetUp;
        }

        public void SetSpinButton(bool interactable, string label)
        {
            spinButton.interactable = interactable;
            spinButtonLabel.text = label;
        }

        public void SetPaytableInteractable(bool interactable) => paytableButton.interactable = interactable;

        public void SetMuted(bool muted)
        {
            if (muteButtonLabel != null) muteButtonLabel.text = muted ? "SOUND: OFF" : "SOUND: ON";
        }

        public static string FormatCredits(int credits) => credits.ToString("N0");
    }
}
