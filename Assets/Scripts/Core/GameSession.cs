using System;

namespace SlotGame.Core
{
    public sealed class GameSession
    {
        private readonly IRandomProvider _random;
        private readonly PaytableEvaluator _evaluator;
        private SpinRound _pendingRound;
        private int _freeSpinBet;

        public SlotRules Rules { get; }
        public CreditWallet Wallet { get; }
        public ReelStrip[] Strips { get; }

        public int FreeSpinsRemaining { get; private set; }
        public bool IsInFreeSpins => FreeSpinsRemaining > 0;
        public bool HasPendingRound => _pendingRound != null;

        public bool CanSpin => !HasPendingRound && (IsInFreeSpins || Wallet.CanAffordBet);

        public event Action<int> FreeSpinsChanged;

        public GameSession(SlotRules rules, CreditWallet wallet, IRandomProvider random)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _evaluator = new PaytableEvaluator(rules);

            Strips = new ReelStrip[rules.ReelCount];
            for (int i = 0; i < Strips.Length; i++)
                Strips[i] = ReelStrip.Build(rules.Symbols, _random, rules.VisibleRows);
        }

        public bool TryBeginSpin(out SpinRound round)
        {
            round = null;
            if (HasPendingRound) return false;

            bool isFree = IsInFreeSpins;
            int bet;

            if (isFree)
            {
                bet = _freeSpinBet;
                FreeSpinsRemaining--;
                FreeSpinsChanged?.Invoke(FreeSpinsRemaining);
            }
            else
            {
                bet = Wallet.CurrentBet;
                if (!Wallet.TryDebit(bet)) return false;
            }

            var stops = new int[Strips.Length];
            for (int reel = 0; reel < Strips.Length; reel++)
                stops[reel] = _random.Next(Strips[reel].Length);

            var outcome = SpinOutcome.FromStops(Strips, stops, Rules.VisibleRows);
            var win = _evaluator.Evaluate(outcome, bet, isFree);

            round = new SpinRound(bet, isFree, outcome, win);
            _pendingRound = round;
            return true;
        }

        public void Settle(SpinRound round)
        {
            if (round == null) throw new ArgumentNullException(nameof(round));
            if (!ReferenceEquals(round, _pendingRound))
                throw new InvalidOperationException("Only the pending round can be settled.");

            Wallet.Credit(round.Win.TotalWin);

            if (round.Win.FreeSpinsAwarded > 0)
            {
                if (!IsInFreeSpins) _freeSpinBet = round.Bet;
                FreeSpinsRemaining += round.Win.FreeSpinsAwarded;
                FreeSpinsChanged?.Invoke(FreeSpinsRemaining);
            }

            round.IsSettled = true;
            _pendingRound = null;
        }
    }
}
