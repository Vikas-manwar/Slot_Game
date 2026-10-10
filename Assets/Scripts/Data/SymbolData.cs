using SlotGame.Core;
using UnityEngine;

namespace SlotGame.Data
{
    [CreateAssetMenu(fileName = "Symbol_New", menuName = "Slot Game/Symbol", order = 0)]
    public sealed class SymbolData : ScriptableObject
    {
        [Tooltip("Unique id used by the maths. Must not repeat within a SlotConfig.")]
        [SerializeField] private int id;

        [SerializeField] private string displayName = "Symbol";
        [SerializeField] private Sprite sprite;
        [SerializeField] private SymbolKind kind = SymbolKind.Regular;

        [Tooltip("Bet multiplier for three of this symbol on the payline. Ignored for scatters.")]
        [Min(0)] [SerializeField] private int threeOfAKindMultiplier = 5;

        [Tooltip("Stops this symbol occupies on each virtual reel. Higher = more frequent.")]
        [Min(0)] [SerializeField] private int reelWeight = 5;

        [Tooltip("Short rule text shown in the paytable for special symbols.")]
        [TextArea] [SerializeField] private string paytableNote;

        public int Id => id;
        public string DisplayName => displayName;
        public Sprite Sprite => sprite;
        public SymbolKind Kind => kind;
        public int ThreeOfAKindMultiplier => threeOfAKindMultiplier;
        public int ReelWeight => reelWeight;
        public string PaytableNote => paytableNote;

        public SymbolRule ToRule() => new SymbolRule(id, displayName, kind, threeOfAKindMultiplier, reelWeight);

#if UNITY_EDITOR
        public void EditorInitialize(int newId, string newName, Sprite newSprite, SymbolKind newKind,
            int multiplier, int weight, string note)
        {
            id = newId;
            displayName = newName;
            sprite = newSprite;
            kind = newKind;
            threeOfAKindMultiplier = multiplier;
            reelWeight = weight;
            paytableNote = note;
        }
#endif
    }
}
