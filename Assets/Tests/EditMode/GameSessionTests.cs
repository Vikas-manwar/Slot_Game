using System;
using NUnit.Framework;
using SlotGame.Core;

namespace SlotGame.Tests
{
    public class GameSessionTests
    {
        [Test]
        public void BeginSpin_DebitsBetBeforeSettlement()
        {
            var wallet = new CreditWallet(100, new[] { 10 });
            var session = new GameSession(TestRules.Create(), wallet, new SeededRandomProvider(1));

            Assert.IsTrue(session.TryBeginSpin(out var round));
            Assert.AreEqual(90, wallet.Balance);

            session.Settle(round);
            Assert.AreEqual(90 + round.Win.TotalWin, wallet.Balance);
        }

        [Test]
        public void CannotSpinWithoutCredits()
        {
            var wallet = new CreditWallet(5, new[] { 10 });
            var session = new GameSession(TestRules.Create(), wallet, new SeededRandomProvider(1));

            Assert.IsFalse(session.CanSpin);
            Assert.IsFalse(session.TryBeginSpin(out _));
            Assert.AreEqual(5, wallet.Balance);
        }

        [Test]
        public void CannotStartSecondSpinBeforeSettling()
        {
            var session = new GameSession(TestRules.Create(), new CreditWallet(100, new[] { 1 }), new SeededRandomProvider(1));

            Assert.IsTrue(session.TryBeginSpin(out _));
            Assert.IsFalse(session.TryBeginSpin(out _));
        }

        [Test]
        public void SettlingWrongRound_Throws()
        {
            var session = new GameSession(TestRules.Create(), new CreditWallet(100, new[] { 1 }), new SeededRandomProvider(1));
            session.TryBeginSpin(out var round);
            session.Settle(round);

            Assert.Throws<InvalidOperationException>(() => session.Settle(round));
        }

        [Test]
        public void OutcomeWindow_MatchesReelStrips()
        {
            var session = new GameSession(TestRules.Create(), new CreditWallet(100, new[] { 1 }), new SeededRandomProvider(7));
            session.TryBeginSpin(out var round);

            for (int reel = 0; reel < 3; reel++)
            {
                int stop = round.Outcome.StopIndices[reel];
                var strip = session.Strips[reel];
                Assert.AreEqual(strip.GetSymbolAt(stop + 1), round.Outcome.GetSymbol(reel, 0));
                Assert.AreEqual(strip.GetSymbolAt(stop), round.Outcome.GetSymbol(reel, 1));
                Assert.AreEqual(strip.GetSymbolAt(stop - 1), round.Outcome.GetSymbol(reel, 2));
            }
        }

        [Test]
        public void FreeSpins_DoNotCostCredits()
        {
            var wallet = new CreditWallet(1000, new[] { 10 });
            var session = new GameSession(TestRules.Create(), wallet, new SeededRandomProvider(3));

            for (int i = 0; i < 10000 && !session.IsInFreeSpins; i++)
            {
                if (wallet.Balance < 10) wallet.Credit(1000);
                session.TryBeginSpin(out var r);
                session.Settle(r);
            }
            Assert.IsTrue(session.IsInFreeSpins, "Feature should trigger within 10k spins.");

            int before = wallet.Balance;
            session.TryBeginSpin(out var free);
            Assert.IsTrue(free.IsFreeSpin);
            Assert.AreEqual(before, wallet.Balance);
        }
    }
}
