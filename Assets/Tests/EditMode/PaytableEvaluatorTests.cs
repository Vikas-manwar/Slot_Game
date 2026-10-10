using NUnit.Framework;
using SlotGame.Core;
using static SlotGame.Tests.TestRules;

namespace SlotGame.Tests
{
    public class PaytableEvaluatorTests
    {
        private PaytableEvaluator _evaluator;

        [SetUp]
        public void SetUp() => _evaluator = new PaytableEvaluator(Create());

        [Test]
        public void ThreeMatchingSymbolsOnPayline_PaysSymbolMultiplier()
        {
            var outcome = Window(new[,] { { Bell, Bar, Cherry }, { Seven, Seven, Seven }, { Bar, Bell, Bell } });
            var win = _evaluator.Evaluate(outcome, bet: 10, isFreeSpin: false);

            Assert.AreEqual(350, win.LineWin);
            Assert.AreEqual(Seven, win.WinningSymbolId);
            Assert.AreEqual(3, win.WinningCells.Count);
        }

        [Test]
        public void MatchOutsidePayline_DoesNotPay()
        {
            var outcome = Window(new[,] { { Seven, Seven, Seven }, { Bell, Bar, Cherry }, { Bar, Bar, Bar } });
            var win = _evaluator.Evaluate(outcome, 10, false);

            Assert.AreEqual(0, win.TotalWin);
            Assert.IsFalse(win.IsWin);
        }

        [Test]
        public void MixedSymbols_DoNotPay()
        {
            var outcome = Window(new[,] { { Bar, Bar, Bar }, { Bell, Bell, Cherry }, { Bar, Bar, Bar } });
            Assert.AreEqual(0, _evaluator.Evaluate(outcome, 10, false).LineWin);
        }

        [Test]
        public void OneWild_SubstitutesAndDoublesWin()
        {
            var outcome = Window(new[,] { { Bar, Bar, Bar }, { Bell, Wild, Bell }, { Bar, Bar, Bar } });
            var win = _evaluator.Evaluate(outcome, 10, false);

            Assert.AreEqual(10 * 6 * 2, win.LineWin);
            Assert.AreEqual(1, win.WildsUsed);
            Assert.AreEqual(Bell, win.WinningSymbolId);
        }

        [Test]
        public void TwoWilds_QuadrupleWin()
        {
            var outcome = Window(new[,] { { Bar, Bar, Bar }, { Wild, Cherry, Wild }, { Bar, Bar, Bar } });
            Assert.AreEqual(10 * 3 * 4, _evaluator.Evaluate(outcome, 10, false).LineWin);
        }

        [Test]
        public void ThreeWilds_PayTopPrizeWithoutExtraMultiplier()
        {
            var outcome = Window(new[,] { { Bar, Bar, Bar }, { Wild, Wild, Wild }, { Bar, Bar, Bar } });
            var win = _evaluator.Evaluate(outcome, 10, false);

            Assert.AreEqual(1000, win.LineWin);
            Assert.AreEqual(Wild, win.WinningSymbolId);
        }

        [Test]
        public void ScatterOnPayline_BreaksLine()
        {
            var outcome = Window(new[,] { { Bar, Bar, Bar }, { Bell, Scatter, Bell }, { Bar, Bar, Bar } });
            Assert.AreEqual(0, _evaluator.Evaluate(outcome, 10, false).LineWin);
        }

        [Test]
        public void ThreeScattersAnywhere_AwardFreeSpinsAndScatterPay()
        {
            var outcome = Window(new[,] { { Scatter, Bar, Bell }, { Bell, Cherry, Bar }, { Bar, Bar, Scatter } });
            outcome.Window[1, 0] = Scatter;
            var win = _evaluator.Evaluate(outcome, 10, false);

            Assert.AreEqual(3, win.ScatterCount);
            Assert.AreEqual(10, win.FreeSpinsAwarded);
            Assert.AreEqual(20, win.ScatterWin);
            Assert.IsTrue(win.IsWin);
        }

        [Test]
        public void TwoScatters_AwardNothing()
        {
            var outcome = Window(new[,] { { Scatter, Bar, Bell }, { Bell, Cherry, Bar }, { Bar, Bar, Scatter } });
            var win = _evaluator.Evaluate(outcome, 10, false);

            Assert.AreEqual(0, win.FreeSpinsAwarded);
            Assert.AreEqual(0, win.TotalWin);
        }

        [Test]
        public void FreeSpin_DoublesLineWin()
        {
            var outcome = Window(new[,] { { Bar, Bar, Bar }, { Cherry, Cherry, Cherry }, { Bar, Bell, Bar } });
            Assert.AreEqual(10 * 3 * 2, _evaluator.Evaluate(outcome, 10, isFreeSpin: true).LineWin);
        }

        [Test]
        public void WinTier_ReflectsWinToBetRatio()
        {
            var small = _evaluator.Evaluate(Window(new[,] { { Bar, Bar, Bar }, { Cherry, Cherry, Cherry }, { Bar, Bell, Bar } }), 10, false);
            var mega = _evaluator.Evaluate(Window(new[,] { { Bar, Bar, Bar }, { Seven, Seven, Seven }, { Bar, Bell, Bar } }), 10, false);

            Assert.AreEqual(WinTier.Small, small.Tier);
            Assert.AreEqual(WinTier.Mega, mega.Tier);
        }
    }
}
