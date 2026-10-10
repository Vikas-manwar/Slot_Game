namespace SlotGame.Core
{
    public readonly struct SimulationReport
    {
        public readonly long PaidSpins;
        public readonly long FreeSpins;
        public readonly long TotalStaked;
        public readonly long TotalWon;
        public readonly long WinningSpins;
        public readonly long FreeSpinTriggers;

        public SimulationReport(long paidSpins, long freeSpins, long totalStaked, long totalWon,
            long winningSpins, long freeSpinTriggers)
        {
            PaidSpins = paidSpins;
            FreeSpins = freeSpins;
            TotalStaked = totalStaked;
            TotalWon = totalWon;
            WinningSpins = winningSpins;
            FreeSpinTriggers = freeSpinTriggers;
        }

        public double Rtp => TotalStaked == 0 ? 0 : (double)TotalWon / TotalStaked;

        public double HitFrequency => PaidSpins + FreeSpins == 0 ? 0 : (double)WinningSpins / (PaidSpins + FreeSpins);

        public override string ToString() =>
            $"Paid spins: {PaidSpins:N0}, free spins: {FreeSpins:N0}, RTP: {Rtp:P2}, " +
            $"hit frequency: {HitFrequency:P2}, free-spin triggers: {FreeSpinTriggers:N0}";
    }

    public static class SlotMathSimulator
    {
        public static SimulationReport Run(SlotRules rules, long paidSpins, IRandomProvider random, int bet = 1)
        {
            var wallet = new CreditWallet(int.MaxValue / 2, new[] { bet });
            var session = new GameSession(rules, wallet, random);

            long paid = 0, free = 0, staked = 0, won = 0, hits = 0, triggers = 0;

            while (paid < paidSpins || session.IsInFreeSpins)
            {
                if (!session.TryBeginSpin(out var round)) break;

                if (round.IsFreeSpin) free++;
                else { paid++; staked += round.Bet; }

                if (round.Win.IsWin) hits++;
                if (round.Win.FreeSpinsAwarded > 0) triggers++;
                won += round.Win.TotalWin;

                session.Settle(round);

                if (wallet.Balance < int.MaxValue / 4) wallet.Credit(int.MaxValue / 4);
            }

            return new SimulationReport(paid, free, staked, won, hits, triggers);
        }
    }
}
