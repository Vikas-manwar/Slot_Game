using System.Collections;
using SlotGame.Audio;
using SlotGame.Core;
using SlotGame.Data;
using SlotGame.Reels;
using SlotGame.UI;
using UnityEngine;

namespace SlotGame.Game
{
    public sealed class WinPresenter : MonoBehaviour
    {
        [SerializeField] private SlotHUD hud;
        [SerializeField] private WinBanner banner;
        [SerializeField] private AudioManager audioManager;

        [Header("Timing (seconds)")]
        [SerializeField] private float smallWinCountDuration = 0.7f;
        [SerializeField] private float bigWinCountDuration = 1.8f;
        [SerializeField] private float megaWinCountDuration = 2.8f;
        [SerializeField] private float coinTickInterval = 0.07f;
        [SerializeField] private float bannerHoldTime = 1.2f;

        private ReelController[] _reels;
        private SlotConfig _config;
        private bool _skipRequested;

        public void Initialize(ReelController[] reels, SlotConfig config)
        {
            _reels = reels;
            _config = config;
        }

        public void Skip() => _skipRequested = true;

        public void Clear()
        {
            foreach (var reel in _reels) reel.ClearWinStates();
            banner.Hide();
        }

        public IEnumerator Present(SpinRound round, int balanceBefore, int balanceAfter)
        {
            _skipRequested = false;
            var win = round.Win;

            if (!win.IsWin)
            {
                hud.SetMessage(round.IsFreeSpin ? "NO WIN THIS FREE SPIN" : "SPIN AGAIN!");
                hud.SetBalance(balanceAfter);
                yield break;
            }

            HighlightCells(win);
            hud.SetMessage(DescribeWin(win));
            PlayWinSound(win);

            string bannerTitle = BannerTitleFor(win);
            if (bannerTitle != null) banner.Show(bannerTitle, "0");

            yield return CountUp(win.TotalWin, balanceBefore, balanceAfter, CountDurationFor(win.Tier), bannerTitle != null);

            if (bannerTitle != null)
            {
                yield return WaitOrSkip(bannerHoldTime);
                banner.Hide();
            }
        }

        public IEnumerator PresentFeatureSummary(int featureWin)
        {
            _skipRequested = false;
            audioManager.Play(featureWin > 0 ? SoundEffect.WinBig : SoundEffect.WinSmall);
            banner.Show("FEATURE WIN", SlotHUD.FormatCredits(featureWin));
            hud.SetMessage($"FREE SPINS COMPLETE - TOTAL WIN {SlotHUD.FormatCredits(featureWin)}");
            yield return WaitOrSkip(bannerHoldTime + 0.8f);
            banner.Hide();
        }

        private void HighlightCells(WinResult win)
        {
            foreach (var reel in _reels) reel.SetAllDimmed(true);

            foreach (var cell in win.WinningCells)
            {
                var view = _reels[cell.Reel].GetViewAtRow(cell.Row);
                view.SetDimmed(false);
                view.SetWinning(true);
            }
        }

        private IEnumerator CountUp(int amount, int balanceFrom, int balanceTo, float duration, bool updateBanner)
        {
            float elapsed = 0f, nextTick = 0f;
            while (elapsed < duration && !_skipRequested)
            {
                elapsed += Time.deltaTime;
                float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 2f);
                int shown = Mathf.RoundToInt(amount * t);

                hud.SetWin(shown);
                hud.SetBalance(balanceFrom + Mathf.RoundToInt((balanceTo - balanceFrom) * t));
                if (updateBanner) banner.SetAmount(SlotHUD.FormatCredits(shown));

                if (elapsed >= nextTick)
                {
                    audioManager.Play(SoundEffect.CoinTick, 0.9f + 0.3f * t);
                    nextTick += coinTickInterval;
                }
                yield return null;
            }

            hud.SetWin(amount);
            hud.SetBalance(balanceTo);
            if (updateBanner) banner.SetAmount(SlotHUD.FormatCredits(amount));
        }

        private IEnumerator WaitOrSkip(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds && !_skipRequested)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private void PlayWinSound(WinResult win)
        {
            if (win.FreeSpinsAwarded > 0) audioManager.Play(SoundEffect.FreeSpins);
            if (win.TotalWin <= 0) return;
            audioManager.Play(win.Tier >= WinTier.Big ? SoundEffect.WinBig : SoundEffect.WinSmall);
        }

        private float CountDurationFor(WinTier tier)
        {
            switch (tier)
            {
                case WinTier.Mega: return megaWinCountDuration;
                case WinTier.Big: return bigWinCountDuration;
                default: return smallWinCountDuration;
            }
        }

        private static string BannerTitleFor(WinResult win)
        {
            if (win.FreeSpinsAwarded > 0) return $"{win.FreeSpinsAwarded} FREE SPINS!";
            switch (win.Tier)
            {
                case WinTier.Mega: return "MEGA WIN!";
                case WinTier.Big: return "BIG WIN!";
                default: return null;
            }
        }

        private string DescribeWin(WinResult win)
        {
            var parts = new System.Collections.Generic.List<string>();

            if (win.IsLineWin)
            {
                var symbol = _config.GetSymbol(win.WinningSymbolId);
                string line = $"3 x {symbol.DisplayName.ToUpperInvariant()}";
                if (win.WildsUsed > 0) line += $" WITH {win.WildsUsed} WILD";
                if (win.AppliedMultiplier > 1) line += $" (x{win.AppliedMultiplier})";
                parts.Add($"{line} = {SlotHUD.FormatCredits(win.LineWin)}");
            }

            if (win.FreeSpinsAwarded > 0)
                parts.Add($"{win.ScatterCount} SCATTERS = {SlotHUD.FormatCredits(win.ScatterWin)} + {win.FreeSpinsAwarded} FREE SPINS");

            return string.Join("   |   ", parts);
        }
    }
}
