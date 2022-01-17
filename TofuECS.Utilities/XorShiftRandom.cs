/*===============================[ XorShiftPlus ]==============================
  ==-------------[ (c) 2018 R. Wildenhaus - Licensed under MIT ]-------------==
  ============================================================================= */

/*
 * Additional modifications for use in TofuECS
 */

namespace Tofunaut.TofuECS.Utilities
{

    /// <summary>
    ///   Generates pseudorandom primitive types with a 64-bit implementation
    ///   of the XorShift algorithm.
    /// </summary>
    public struct XorShiftRandom
    {

        #region Data Members
        
        // Constants
        private const double DOUBLE_UNIT = 1.0 / (int.MaxValue + 1.0);

        // State Fields
        public ulong XState;
        public ulong YState;
        // Buffer for optimized bit generation.
        public ulong BufferState;
        public ulong BufferMaskState;

        #endregion

        #region Constructor

        /// <summary>
        ///   Constructs a new  generator
        ///   with the supplied seed.
        /// </summary>
        /// <param name="seed">
        ///   The seed value.
        /// </param>
        public XorShiftRandom(ulong seed)
        {
            BufferState = default;
            BufferMaskState = default;
            XState = seed << 3;
            YState = seed >> 3;
        }

        public XorShiftRandom(ulong xState, ulong yState, ulong bufferState, ulong bufferMaskState)
        {
            BufferState = bufferState;
            BufferMaskState = bufferMaskState;
            XState = xState;
            YState = yState;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///   Generates a pseudorandom boolean.
        /// </summary>
        /// <returns>
        ///   A pseudorandom boolean.
        /// </returns>
        public bool NextBoolean()
        {
            bool _;
            if (BufferMaskState > 0)
            {
                _ = (BufferState & BufferMaskState) == 0;
                BufferMaskState >>= 1;
                return _;
            }

            ulong temp_x, temp_y;
            temp_x = YState;
            XState ^= XState << 23;
            temp_y = XState ^ YState ^ (XState >> 17) ^ (YState >> 26);

            BufferState = temp_y + YState;
            XState = temp_x;
            YState = temp_y;

            BufferMaskState = 0x8000000000000000;
            return (BufferState & 0xF000000000000000) == 0;
        }

        /// <summary>
        ///   Generates a pseudorandom byte.
        /// </summary>
        /// <returns>
        ///   A pseudorandom byte.
        /// </returns>
        public byte NextByte()
        {
            if (BufferMaskState >= 8)
            {
                byte _ = (byte)BufferState;
                BufferState >>= 8;
                BufferMaskState >>= 8;
                return _;
            }

            ulong temp_x, temp_y;
            temp_x = YState;
            XState ^= XState << 23;
            temp_y = XState ^ YState ^ (XState >> 17) ^ (YState >> 26);

            BufferState = temp_y + YState;
            XState = temp_x;
            YState = temp_y;

            BufferMaskState = 0x8000000000000;
            return (byte)(BufferState >>= 8);
        }

        /// <summary>
        ///   Generates a pseudorandom 16-bit signed integer.
        /// </summary>
        /// <returns>
        ///   A pseudorandom 16-bit signed integer.
        /// </returns>
        public short NextInt16()
        {
            short _;
            ulong temp_x, temp_y;

            temp_x = YState;
            XState ^= XState << 23;
            temp_y = XState ^ YState ^ (XState >> 17) ^ (YState >> 26);

            _ = (short)(temp_y + YState);

            XState = temp_x;
            YState = temp_y;

            return _;
        }

        /// <summary>
        ///   Generates a pseudorandom 16-bit unsigned integer.
        /// </summary>
        /// <returns>
        ///   A pseudorandom 16-bit unsigned integer.
        /// </returns>
        public ushort NextUInt16()
        {
            ushort _;
            ulong temp_x, temp_y;

            temp_x = YState;
            XState ^= XState << 23;
            temp_y = XState ^ YState ^ (XState >> 17) ^ (YState >> 26);

            _ = (ushort)(temp_y + YState);

            XState = temp_x;
            YState = temp_y;

            return _;
        }

        /// <summary>
        ///   Generates a pseudorandom 32-bit signed integer.
        /// </summary>
        /// <returns>
        ///   A pseudorandom 32-bit signed integer.
        /// </returns>
        public int NextInt32()
        {
            int _;
            ulong temp_x, temp_y;

            temp_x = YState;
            XState ^= XState << 23;
            temp_y = XState ^ YState ^ (XState >> 17) ^ (YState >> 26);

            _ = (int)(temp_y + YState);

            XState = temp_x;
            YState = temp_y;

            return _;
        }

        /// <summary>
        ///   Generates a pseudorandom 32-bit unsigned integer.
        /// </summary>
        /// <returns>
        ///   A pseudorandom 32-bit unsigned integer.
        /// </returns>
        public uint NextUInt32()
        {
            uint _;
            ulong temp_x, temp_y;

            temp_x = YState;
            XState ^= XState << 23;
            temp_y = XState ^ YState ^ (XState >> 17) ^ (YState >> 26);

            _ = (uint)(temp_y + YState);

            XState = temp_x;
            YState = temp_y;

            return _;
        }

        /// <summary>
        ///   Generates a pseudorandom 64-bit signed integer.
        /// </summary>
        /// <returns>
        ///   A pseudorandom 64-bit signed integer.
        /// </returns>
        public long NextInt64()
        {
            long _;
            ulong temp_x, temp_y;

            temp_x = YState;
            XState ^= XState << 23;
            temp_y = XState ^ YState ^ (XState >> 17) ^ (YState >> 26);

            _ = (long)(temp_y + YState);

            XState = temp_x;
            YState = temp_y;

            return _;
        }

        /// <summary>
        ///   Generates a pseudorandom 64-bit unsigned integer.
        /// </summary>
        /// <returns>
        ///   A pseudorandom 64-bit unsigned integer.
        /// </returns>
        public ulong NextUInt64()
        {
            ulong _;
            ulong temp_x, temp_y;

            temp_x = YState;
            XState ^= XState << 23;
            temp_y = XState ^ YState ^ (XState >> 17) ^ (YState >> 26);

            _ = (ulong)(temp_y + YState);

            XState = temp_x;
            YState = temp_y;

            return _;
        }

        /// <summary>
        ///   Generates a pseudorandom double between
        ///   0 and 1 non-inclusive.
        /// </summary>
        /// <returns>
        ///   A pseudorandom double.
        /// </returns>
        public double NextDouble()
        {
            double _;
            ulong temp_x, temp_y, temp_z;

            temp_x = YState;
            XState ^= XState << 23;
            temp_y = XState ^ YState ^ (XState >> 17) ^ (YState >> 26);

            temp_z = temp_y + YState;
            _ = DOUBLE_UNIT * (0x7FFFFFFF & temp_z);

            XState = temp_x;
            YState = temp_y;

            return _;
        }

        /// <summary>
        ///   Generates a pseudorandom decimal between
        ///   0 and 1 non-inclusive.
        /// </summary>
        /// <returns>
        ///   A pseudorandom decimal.
        /// </returns>
        public decimal NextDecimal()
        {
            decimal _;
            int l, m, h;
            ulong temp_x, temp_y, temp_z;

            temp_x = YState;
            XState ^= XState << 23;
            temp_y = XState ^ YState ^ (XState >> 17) ^ (YState >> 26);

            temp_z = temp_y + YState;

            h = (int)(temp_z & 0x1FFFFFFF);
            m = (int)(temp_z >> 16);
            l = (int)(temp_z >> 32);

            _ = new decimal(l, m, h, false, 28);

            XState = temp_x;
            YState = temp_y;

            return _;
        }

        /// <summary>
        ///   Fills the supplied buffer with pseudorandom bytes.
        /// </summary>
        /// <param name="buffer">
        ///   The buffer to fill.
        /// </param>
        public unsafe void NextBytes(byte[] buffer)
        {
            // Localize state for stack execution
            ulong x = XState, y = YState, temp_x, temp_y, z;

            fixed (byte* pBuffer = buffer)
            {
                ulong* pIndex = (ulong*)pBuffer;
                ulong* pEnd = (ulong*)(pBuffer + buffer.Length);

                // Fill array in 8-byte chunks
                while (pIndex <= pEnd - 1)
                {
                    temp_x = y;
                    x ^= x << 23;
                    temp_y = x ^ y ^ (x >> 17) ^ (y >> 26);

                    *(pIndex++) = temp_y + y;

                    x = temp_x;
                    y = temp_y;
                }

                // Fill remaining bytes individually to prevent overflow
                if (pIndex < pEnd)
                {
                    temp_x = y;
                    x ^= x << 23;
                    temp_y = x ^ y ^ (x >> 17) ^ (y >> 26);
                    z = temp_y + y;

                    byte* pByte = (byte*)pIndex;
                    while (pByte < pEnd)
                        *(pByte++) = (byte)(z >>= 8);
                }
            }

            // Store modified state in fields.
            XState = x;
            YState = y;
        }

        #endregion

    }

}