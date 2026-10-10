using System;
using NUnit.Framework;
using SlotGame.Core;

namespace SlotGame.Tests
{
    public class ReelStripAndRandomTests
    {
        [Test]
        public void Strip_ContainsEachSymbolByWeight()
        {
            var rules = TestRules.Create();
            var strip = ReelStrip.Build(rules.Symbols, new SeededRandomProvider(42), 3);

            Assert.AreEqual(31, strip.Length);
            foreach (var symbol in rules.Symbols)
                Assert.AreEqual(symbol.ReelWeight, strip.CountOf(symbol.Id), symbol.Name);
        }

        [Test]
        public void Strip_KeepsScattersApart()
        {
            var rules = TestRules.Create();
            for (int seed = 0; seed < 200; seed++)
            {
                var strip = ReelStrip.Build(rules.Symbols, new SeededRandomProvider(seed), 3);
                for (int i = 0; i < strip.Length; i++)
                {
                    if (strip.GetSymbolAt(i) != TestRules.Scatter) continue;
                    Assert.AreNotEqual(TestRules.Scatter, strip.GetSymbolAt(i + 1));
                    Assert.AreNotEqual(TestRules.Scatter, strip.GetSymbolAt(i + 2));
                }
            }
        }

        [Test]
        public void Strip_WrapsNegativeAndOverflowingIndices()
        {
            var strip = new ReelStrip(new[] { 7, 8, 9 });
            Assert.AreEqual(9, strip.GetSymbolAt(-1));
            Assert.AreEqual(7, strip.GetSymbolAt(3));
            Assert.AreEqual(8, strip.GetSymbolAt(-5));
        }

        [Test]
        public void SecureRandom_StaysInRangeAndCoversIt()
        {
            using (var random = new SecureRandomProvider())
            {
                var seen = new bool[6];
                for (int i = 0; i < 6000; i++)
                {
                    int value = random.Next(6);
                    Assert.That(value, Is.InRange(0, 5));
                    seen[value] = true;
                }
                Assert.That(seen, Is.All.True);
            }
        }

        [Test]
        public void SecureRandom_IsRoughlyUniform()
        {
            using (var random = new SecureRandomProvider())
            {
                const int buckets = 10, samples = 100000;
                var counts = new int[buckets];
                for (int i = 0; i < samples; i++) counts[random.Next(buckets)]++;

                double expected = samples / (double)buckets, chi = 0;
                foreach (int c in counts) chi += (c - expected) * (c - expected) / expected;
                Assert.Less(chi, 27.88);
            }
        }

        [Test]
        public void Simulation_RtpIsWithinTargetBand()
        {
            var report = SlotMathSimulator.Run(TestRules.Create(), 300000, new SeededRandomProvider(11));
            Assert.That(report.Rtp, Is.InRange(0.90, 1.02), report.ToString());
        }

        [Test]
        public void Wallet_RejectsOverdraft()
        {
            var wallet = new CreditWallet(10, new[] { 5, 10, 20 });
            Assert.IsFalse(wallet.TryDebit(11));
            Assert.AreEqual(10, wallet.Balance);
            Assert.Throws<ArgumentOutOfRangeException>(() => wallet.Credit(-1));
        }

        [Test]
        public void Wallet_BetIsClampedToLevels()
        {
            var wallet = new CreditWallet(100, new[] { 5, 10, 20 });
            wallet.DecreaseBet();
            Assert.AreEqual(5, wallet.CurrentBet);
            wallet.IncreaseBet(); wallet.IncreaseBet(); wallet.IncreaseBet();
            Assert.AreEqual(20, wallet.CurrentBet);
        }
    }
}
