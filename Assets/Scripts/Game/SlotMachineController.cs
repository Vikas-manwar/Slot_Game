using System.Collections;
using SlotGame.Audio;
using SlotGame.Core;
using SlotGame.Data;
using SlotGame.Reels;
using SlotGame.UI;
using UnityEngine;

namespace SlotGame.Game
{
    public sealed class SlotMachineController : MonoBehaviour
    {
        private enum MachineState { Idle, Spinning, PresentingWin }

        [Header("Configuration")]
        [SerializeField] private SlotConfig config;
        [SerializeField] private ReelAnimationProfile animationProfile;

        [Header("Scene References")]
        [SerializeField] private ReelController[] reels = new ReelController[0];
        [SerializeField] private SlotHUD hud;
        [SerializeField] private WinPresenter winPresenter;
        [SerializeField] private LeverView lever;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private ConfirmDialog confirmDialog;
        [SerializeField] private PaytablePanel paytablePanel;

        [Header("Behaviour")]
        [Tooltip("Pause between automatic free spins.")]
        [SerializeField] private float freeSpinAutoDelay = 0.9f;
        [Tooltip("Slow the last reel when the first reels could still make at least this multiplier.")]
        [SerializeField] private int anticipationMinMultiplier = 30;

        [Header("Debug")]
        [Tooltip("Use a fixed seed for reproducible sessions (testing only - never ship enabled).")]
        [SerializeField] private bool useFixedSeed;
        [SerializeField] private int fixedSeed = 12345;

        private GameSession _session;
        private IRandomProvider _random;
        private MachineState _state = MachineState.Idle;
        private bool _quickStopRequested;
        private int _reelsStopped;
        private int _featureWin;

        private void Start()
        {
            _random = useFixedSeed ? (IRandomProvider)new SeededRandomProvider(fixedSeed) : new SecureRandomProvider();

            var wallet = new CreditWallet(config.StartingCredits, config.BetLevels, config.DefaultBetIndex);
            _session = new GameSession(config.BuildRules(), wallet, _random);

            for (int i = 0; i < reels.Length; i++)
            {
                reels[i].Initialize(i, _session.Strips[i], config, _random.Next(_session.Strips[i].Length));
                reels[i].Stopped += OnReelStopped;
            }

            winPresenter.Initialize(reels, config);
            paytablePanel.Build(config);
            WireInput();

            hud.SetBalance(wallet.Balance);
            hud.SetBet(wallet.CurrentBet);
            hud.SetWin(0);
            hud.SetFreeSpins(0);
            hud.SetMuted(audioManager.IsMuted);
            hud.SetMessage("PULL THE LEVER OR PRESS SPIN");
            RefreshControls();
        }

        private void OnDestroy()
        {
            (_random as System.IDisposable)?.Dispose();
        }

        private void WireInput()
        {
            hud.SpinPressed += OnSpinPressed;
            hud.BetUpPressed += () => ChangeBet(+1);
            hud.BetDownPressed += () => ChangeBet(-1);
            hud.PaytablePressed += OpenPaytable;
            hud.MutePressed += ToggleMute;
            lever.Clicked += OnSpinPressed;
            confirmDialog.ButtonPressed += () => audioManager.Play(SoundEffect.ButtonClick);
            paytablePanel.ButtonPressed += () => audioManager.Play(SoundEffect.ButtonClick);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && paytablePanel.IsOpen) paytablePanel.Close();

            if (IsPopupOpen) return;
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                OnSpinPressed();
        }

        private bool IsPopupOpen => confirmDialog.IsOpen || paytablePanel.IsOpen;


        private void OnSpinPressed()
        {
            if (IsPopupOpen) return;

            switch (_state)
            {
                case MachineState.Idle:
                    TryStartSpin();
                    break;
                case MachineState.Spinning:
                    _quickStopRequested = true;
                    break;
                case MachineState.PresentingWin:
                    winPresenter.Skip();
                    break;
            }
        }

        private void ChangeBet(int direction)
        {
            if (_state != MachineState.Idle || _session.IsInFreeSpins) return;

            audioManager.Play(SoundEffect.ButtonClick);
            if (direction > 0) _session.Wallet.IncreaseBet();
            else _session.Wallet.DecreaseBet();

            hud.SetBet(_session.Wallet.CurrentBet);
            hud.SetMessage($"BET {SlotHUD.FormatCredits(_session.Wallet.CurrentBet)}");
            RefreshControls();
        }

        private void OpenPaytable()
        {
            if (_state != MachineState.Idle || IsPopupOpen) return;
            audioManager.Play(SoundEffect.ButtonClick);
            paytablePanel.Open();
        }

        private void ToggleMute()
        {
            audioManager.ToggleMute();
            audioManager.Play(SoundEffect.ButtonClick);
            hud.SetMuted(audioManager.IsMuted);
        }


        private void TryStartSpin()
        {
            if (_session.CanSpin)
            {
                StartCoroutine(SpinRoutine());
                return;
            }

            int lowestBet = config.BetLevels[0];
            if (_session.Wallet.Balance >= lowestBet)
                hud.SetMessage("NOT ENOUGH CREDITS - LOWER YOUR BET");
            else
                ShowOutOfCredits();
        }

        private IEnumerator SpinRoutine()
        {
            winPresenter.Clear();
            SpinRound round;
            if (!_session.TryBeginSpin(out round)) yield break;

            _state = MachineState.Spinning;
            _quickStopRequested = false;
            _reelsStopped = 0;
            RefreshControls();

            hud.SetBalance(_session.Wallet.Balance);
            hud.SetWin(0);
            hud.SetFreeSpins(_session.FreeSpinsRemaining + (round.IsFreeSpin ? 1 : 0));
            hud.SetMessage(round.IsFreeSpin ? $"FREE SPIN  -  {_session.FreeSpinsRemaining} LEFT AFTER THIS" : "GOOD LUCK!");

            lever.PlayPull();
            audioManager.Play(SoundEffect.LeverPull);
            audioManager.StartSpinLoop();

            foreach (var reel in reels)
            {
                reel.StartSpin();
                yield return WaitOrQuickStop(animationProfile.reelStartStagger);
            }

            yield return WaitOrQuickStop(animationProfile.minimumSpinTime);

            bool anticipate = !_quickStopRequested && ShouldAnticipate(round.Outcome);
            for (int i = 0; i < reels.Length; i++)
            {
                bool isLast = i == reels.Length - 1;
                if (isLast && anticipate)
                {
                    reels[i].SetAnticipation(true);
                    hud.SetMessage("HERE IT COMES...");
                    yield return WaitOrQuickStop(animationProfile.anticipationDuration);
                }

                reels[i].RequestStop(round.Outcome.StopIndices[i]);
                if (!isLast) yield return WaitOrQuickStop(animationProfile.reelStopInterval);
            }

            while (_reelsStopped < reels.Length) yield return null;
            audioManager.StopSpinLoop();

            int balanceBefore = _session.Wallet.Balance;
            bool featureStarted = !round.IsFreeSpin && round.Win.FreeSpinsAwarded > 0;
            _session.Settle(round);

            if (featureStarted) _featureWin = 0;
            if (round.IsFreeSpin || featureStarted) _featureWin += round.Win.TotalWin;

            _state = MachineState.PresentingWin;
            RefreshControls();
            yield return winPresenter.Present(round, balanceBefore, _session.Wallet.Balance);
            hud.SetFreeSpins(_session.FreeSpinsRemaining);

            bool featureEnded = round.IsFreeSpin && !_session.IsInFreeSpins;
            if (featureEnded) yield return winPresenter.PresentFeatureSummary(_featureWin);

            _state = MachineState.Idle;
            RefreshControls();

            if (_session.IsInFreeSpins)
            {
                yield return new WaitForSeconds(freeSpinAutoDelay);
                if (_state == MachineState.Idle) StartCoroutine(SpinRoutine());
            }
            else if (!_session.CanSpin && _session.Wallet.Balance < config.BetLevels[0])
            {
                ShowOutOfCredits();
            }
        }

        private void OnReelStopped(int reelIndex)
        {
            _reelsStopped++;
            audioManager.Play(SoundEffect.ReelStop, 0.9f + 0.1f * reelIndex);
        }

        private IEnumerator WaitOrQuickStop(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds && !_quickStopRequested)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private bool ShouldAnticipate(SpinOutcome outcome)
        {
            int lastReel = reels.Length - 1;
            int payline = config.VisibleRows / 2;

            int scatters = 0;
            for (int reel = 0; reel < lastReel; reel++)
                for (int row = 0; row < config.VisibleRows; row++)
                    if (config.GetSymbol(outcome.GetSymbol(reel, row)).Kind == SymbolKind.Scatter)
                        scatters++;
            if (scatters >= config.ScattersForFreeSpins - 1) return true;

            SymbolData candidate = null;
            for (int reel = 0; reel < lastReel; reel++)
            {
                var symbol = config.GetSymbol(outcome.GetSymbol(reel, payline));
                if (symbol.Kind == SymbolKind.Scatter) return false;
                if (symbol.Kind == SymbolKind.Wild) continue;
                if (candidate == null) candidate = symbol;
                else if (candidate.Id != symbol.Id) return false;
            }

            return candidate == null || candidate.ThreeOfAKindMultiplier >= anticipationMinMultiplier;
        }

        private void ShowOutOfCredits()
        {
            int refill = config.StartingCredits;
            confirmDialog.Show(
                "OUT OF CREDITS",
                $"Top up to {SlotHUD.FormatCredits(refill)} credits and keep playing?",
                onYes: () =>
                {
                    var wallet = _session.Wallet;
                    if (wallet.Balance < refill) wallet.Credit(refill - wallet.Balance);
                    hud.SetBalance(wallet.Balance);
                    hud.SetMessage("CREDITS REFILLED - GOOD LUCK!");
                    RefreshControls();
                },
                onNo: () => hud.SetMessage("THANKS FOR PLAYING!"));
        }

        private void RefreshControls()
        {
            var wallet = _session.Wallet;
            bool idle = _state == MachineState.Idle;
            bool inFeature = _session.IsInFreeSpins;

            hud.SetBetControls(idle && !inFeature, wallet.BetIndex > 0, wallet.BetIndex < wallet.BetLevelCount - 1);
            hud.SetPaytableInteractable(idle && !inFeature);
            lever.Interactable = idle && !inFeature;

            switch (_state)
            {
                case MachineState.Spinning:
                    hud.SetSpinButton(true, "STOP");
                    break;
                case MachineState.PresentingWin:
                    hud.SetSpinButton(true, "SKIP");
                    break;
                default:
                    hud.SetSpinButton(!inFeature, inFeature ? "FREE SPINS" : "SPIN");
                    break;
            }
        }
    }
}
