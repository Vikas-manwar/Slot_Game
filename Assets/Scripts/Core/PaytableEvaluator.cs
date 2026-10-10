using System;

namespace SlotGame.Core
{
    public sealed class PaytableEvaluator
    {
        private readonly SlotRules _rules;

        public PaytableEvaluator(SlotRules rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        public WinResult Evaluate(SpinOutcome outcome, int bet, bool isFreeSpin)
        {
            if (outcome == null) throw new ArgumentNullException(nameof(outcome));
            if (bet < 0) throw new ArgumentOutOfRangeException(nameof(bet));

            var result = new WinResult { Bet = bet };
            EvaluatePayline(outcome, bet, isFreeSpin, result);
            EvaluateScatters(outcome, bet, result);
            return result;
        }

        private void EvaluatePayline(SpinOutcome outcome, int bet, bool isFreeSpin, WinResult result)
        {
            int row = _rules.PaylineRow;
            int targetId = -1;
            int wildCount = 0;
            int firstWildId = -1;

            for (int reel = 0; reel < outcome.ReelCount; reel++)
            {
                var symbol = _rules.GetSymbol(outcome.GetSymbol(reel, row));

                switch (symbol.Kind)
                {
                    case SymbolKind.Scatter:
                        return;

                    case SymbolKind.Wild:
                        wildCount++;
                        if (firstWildId < 0) firstWildId = symbol.Id;
                        break;

                    default:
                        if (targetId < 0) targetId = symbol.Id;
                        else if (targetId != symbol.Id) return;
                        break;
                }
            }

            int basePay;
            int multiplier = 1;

            if (targetId < 0)
            {
                targetId = firstWildId;
                basePay = _rules.GetSymbol(firstWildId).ThreeOfAKindMultiplier;
            }
            else
            {
                basePay = _rules.GetSymbol(targetId).ThreeOfAKindMultiplier;
                for (int i = 0; i < wildCount; i++) multiplier *= _rules.WildWinMultiplier;
                result.WildsUsed = wildCount;
            }

            if (isFreeSpin) multiplier *= _rules.FreeSpinWinMultiplier;
            if (basePay <= 0) return;

            result.WinningSymbolId = targetId;
            result.AppliedMultiplier = multiplier;
            result.LineWin = checked(bet * basePay * multiplier);

            for (int reel = 0; reel < outcome.ReelCount; reel++)
                result.WinningCells.Add(new CellPosition(reel, row));
        }

        private void EvaluateScatters(SpinOutcome outcome, int bet, WinResult result)
        {
            int count = 0;
            for (int reel = 0; reel < outcome.ReelCount; reel++)
                for (int row = 0; row < outcome.RowCount; row++)
                    if (_rules.GetSymbol(outcome.GetSymbol(reel, row)).Kind == SymbolKind.Scatter)
                        count++;

            result.ScatterCount = count;
            if (count < _rules.ScattersForFreeSpins) return;

            result.FreeSpinsAwarded = _rules.FreeSpinsAwarded;
            result.ScatterWin = checked(bet * _rules.ScatterPayMultiplier);

            for (int reel = 0; reel < outcome.ReelCount; reel++)
                for (int row = 0; row < outcome.RowCount; row++)
                    if (_rules.GetSymbol(outcome.GetSymbol(reel, row)).Kind == SymbolKind.Scatter)
                        result.WinningCells.Add(new CellPosition(reel, row));
        }
    }
}
