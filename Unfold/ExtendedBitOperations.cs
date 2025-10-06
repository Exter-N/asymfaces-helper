using System.Numerics;

namespace Unfold;

public static class ExtendedBitOperations
{
    public static T InsertBit<T>(T value, int position) where T : IBinaryInteger<T>
    {
        var lowMask = (T.One << position) - T.One;
        return ((value & ~lowMask) << 1) | (value & lowMask);
    }

    public static T RemoveBit<T>(T value, int position) where T : IBinaryInteger<T>
    {
        var lowMask = (T.One << position) - T.One;
        return ((value >> 1) & ~lowMask) | (value & lowMask);
    }

    public static uint Widen1To2(ushort bits)
    {
        var bitsW = ((bits & 0xFF00u) << 8) | (bits & 0xFFu);
        bitsW = ((bitsW & 0xF000F0u) << 4) | (bitsW & 0xF000Fu);
        bitsW = ((bitsW & 0xC0C0C0Cu) << 2) | (bitsW & 0x3030303u);
        bitsW = ((bitsW & 0x22222222u) << 1) | (bitsW & 0x11111111u);
        return (bitsW << 1) | bitsW;
    }

    public static ulong Widen2To4(uint bits)
    {
        var bitsW = ((bits & 0xFFFF0000UL) << 16) | (bits & 0xFFFFUL);
        bitsW = ((bitsW & 0xFF000000FF00UL) << 8) | (bitsW & 0xFF000000FFUL);
        bitsW = ((bitsW & 0xF000F000F000F0UL) << 4) | (bitsW & 0xF000F000F000FUL);
        bitsW = ((bitsW & 0xC0C0C0C0C0C0C0CUL) << 2) | (bitsW & 0x303030303030303UL);
        return (bitsW << 2) | bitsW;
    }

    public static ulong Widen3To4(ulong bits)
    {
        bits = ((bits & 0xFFFFFF000000UL) << 8) | (bits & 0xFFFFFFUL);
        bits = ((bits & 0xFFF00000FFF000UL) << 4) | (bits & 0xFFF00000FFFUL);
        bits = ((bits & 0xFC00FC00FC00FC0UL) << 2) | (bits & 0x3F003F003F003FUL);
        bits = ((bits & 0x3838383838383838UL) << 1) | (bits & 0x707070707070707UL);
        return (bits << 1) | ((bits & 0x4444444444444444UL) >> 2);
    }

    public static uint Narrow4To2(ulong bits)
    {
        bits = ((bits & 0xC0C0C0C0C0C0C0C0UL) >> 4) | ((bits & 0xC0C0C0C0C0C0C0CUL) >> 2);
        bits = ((bits & 0xF000F000F000F00UL) >> 4) | (bits & 0xF000F000F000FUL);
        bits = ((bits & 0xFF000000FF0000UL) >> 8) | (bits & 0xFF000000FFUL);
        return unchecked((uint)((bits & 0xFFFF00000000UL) >> 16) | (uint)(bits & 0xFFFFUL));
    }

    public static ulong Narrow4To3(ulong bits)
    {
        bits = ((bits & 0xE0E0E0E0E0E0E0E0UL) >> 2) | ((bits & 0xE0E0E0E0E0E0E0EUL) >> 1);
        bits = ((bits & 0x3F003F003F003F00UL) >> 2) | (bits & 0x3F003F003F003FUL);
        bits = ((bits & 0xFFF00000FFF0000UL) >> 4) | (bits & 0xFFF00000FFFUL);
        return ((bits & 0xFFFFFF00000000UL) >> 8) | (bits & 0xFFFFFFUL);
    }

    public static ulong MirrorIndices(ulong indices)
    {
        indices = ((indices & 0xFF00FF00FF00FF00UL) >> 8) | ((indices & 0x00FF00FF00FF00FFUL) << 8);
        indices = ((indices & 0xF0F0F0F0F0F0F0F0UL) >> 4) | ((indices & 0x0F0F0F0F0F0F0F0FUL) << 4);
        return indices;
    }

    public static T ExtractBits<T>(T bits, BitRange range) where T : unmanaged, IBinaryInteger<T>
        => (bits >> range.Start) & range.GetMask<T>();

    public static T ReplaceBits<T>(T bits, BitRange range, T value) where T : unmanaged, IBinaryInteger<T>
    {
        var mask = range.GetMask<T>();
        if (!T.IsZero(value & ~mask))
            throw new ArgumentOutOfRangeException(nameof(value));
        return (bits & ~(mask << range.Start)) | (value << range.Start);
    }

    public static ulong ExtractBits128(UInt128 bits, BitRange range)
        => unchecked((ulong)(bits >> range.Start)) & range.GetMask<ulong>();

    public static void ReplaceBits128(ref UInt128 bits, BitRange range, ulong value)
        => bits = ReplaceBits(bits, range, value);
}
