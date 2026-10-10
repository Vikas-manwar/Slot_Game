using System;

namespace SlotGame.Core
{
    public sealed class CreditWallet
    {
        private readonly int[] _betLevels;

        public int Balance { get; private set; }
        public int BetIndex { get; private set; }
        public int CurrentBet => _betLevels[BetIndex];
        public int BetLevelCount => _betLevels.Length;
        public bool CanAffordBet => Balance >= CurrentBet;

        public event Action<int> BalanceChanged;
        public event Action<int> BetChanged;

        public CreditWallet(int startingBalance, int[] betLevels, int startingBetIndex = 0)
        {
            if (startingBalance < 0) throw new ArgumentOutOfRangeException(nameof(startingBalance));
            if (betLevels == null || betLevels.Length == 0)
                throw new ArgumentException("At least one bet level is required.", nameof(betLevels));
            foreach (int level in betLevels)
                if (level <= 0) throw new ArgumentException("Bet levels must be positive.", nameof(betLevels));

            Balance = startingBalance;
            _betLevels = (int[])betLevels.Clone();
            BetIndex = Math.Max(0, Math.Min(startingBetIndex, _betLevels.Length - 1));
        }

        public void IncreaseBet() => SetBetIndex(BetIndex + 1);
        public void DecreaseBet() => SetBetIndex(BetIndex - 1);

        public void SetBetIndex(int index)
        {
            int clamped = Math.Max(0, Math.Min(index, _betLevels.Length - 1));
            if (clamped == BetIndex) return;
            BetIndex = clamped;
            BetChanged?.Invoke(CurrentBet);
        }

        public bool TryDebit(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (Balance < amount) return false;
            Balance -= amount;
            BalanceChanged?.Invoke(Balance);
            return true;
        }

        public void Credit(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0) return;
            Balance = checked(Balance + amount);
            BalanceChanged?.Invoke(Balance);
        }
    }
}
