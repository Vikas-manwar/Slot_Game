using System;

namespace SlotGame.Core
{
    public sealed class SymbolRule
    {
        public int Id { get; }
        public string Name { get; }
        public SymbolKind Kind { get; }

        public int ThreeOfAKindMultiplier { get; }

        public int ReelWeight { get; }

        public SymbolRule(int id, string name, SymbolKind kind, int threeOfAKindMultiplier, int reelWeight)
        {
            if (reelWeight < 0) throw new ArgumentOutOfRangeException(nameof(reelWeight));
            if (threeOfAKindMultiplier < 0) throw new ArgumentOutOfRangeException(nameof(threeOfAKindMultiplier));

            Id = id;
            Name = name;
            Kind = kind;
            ThreeOfAKindMultiplier = threeOfAKindMultiplier;
            ReelWeight = reelWeight;
        }

        public override string ToString() => $"{Name}#{Id}";
    }
}
