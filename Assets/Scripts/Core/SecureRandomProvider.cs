using System;
using System.Security.Cryptography;

namespace SlotGame.Core
{
    public sealed class SecureRandomProvider : IRandomProvider, IDisposable
    {
        private readonly RandomNumberGenerator _crypto;
        private readonly Random _fallback;
        private readonly byte[] _buffer = new byte[4];

        public bool IsUsingFallback => _crypto == null;

        public SecureRandomProvider()
        {
            try
            {
                _crypto = RandomNumberGenerator.Create();
                _crypto.GetBytes(_buffer);
            }
            catch (Exception)
            {
                _crypto = null;
                _fallback = new Random(Guid.NewGuid().GetHashCode());
            }
        }

        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Range must be positive.");

            if (_crypto == null)
                return _fallback.Next(maxExclusive);

            uint range = (uint)maxExclusive;
            uint limit = uint.MaxValue - (uint.MaxValue % range);
            uint value;
            do
            {
                _crypto.GetBytes(_buffer);
                value = BitConverter.ToUInt32(_buffer, 0);
            } while (value >= limit);

            return (int)(value % range);
        }

        public void Dispose()
        {
            _crypto?.Dispose();
        }
    }
}
