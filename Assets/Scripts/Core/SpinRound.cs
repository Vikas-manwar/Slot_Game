namespace SlotGame.Core
{
    public sealed class SpinRound
    {
        public int Bet { get; }
        public bool IsFreeSpin { get; }
        public SpinOutcome Outcome { get; }
        public WinResult Win { get; }
        public bool IsSettled { get; internal set; }

        internal SpinRound(int bet, bool isFreeSpin, SpinOutcome outcome, WinResult win)
        {
            Bet = bet;
            IsFreeSpin = isFreeSpin;
            Outcome = outcome;
            Win = win;
        }
    }
}
