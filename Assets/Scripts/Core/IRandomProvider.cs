namespace SlotGame.Core
{
    public interface IRandomProvider
    {
        int Next(int maxExclusive);
    }
}
