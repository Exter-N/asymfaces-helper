using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using static Unfold.ExtendedBitOperations;

namespace Unfold.BlockCompression;

[StructLayout(LayoutKind.Explicit, Size = 8)]
public struct Bc4Block(ulong data) : IBcBlock, IEquatable<Bc4Block>
{
    [FieldOffset(0)] public byte Value0;
    [FieldOffset(1)] public byte Value1;

    [FieldOffset(0)] public ulong Data = data;

    public ulong this[BitRange range]
    {
        readonly get => ExtractBits(Data, range);
        set => Data = ReplaceBits(Data, range, value);
    }

    public void Mirror()
    {
        Data = ((Data & 0x1C71C71C71C70000UL) << 3) | ((Data & 0xE38E38E38E380000UL) >> 3) | (Data & 0xFFFFUL);
        Data = ((Data & 0x03F03F03F03F0000UL) << 6) | ((Data & 0xFC0FC0FC0FC00000UL) >> 6) | (Data & 0xFFFFUL);
    }

    public void InvertRed()
    {
        var wasFullInterp = Value0 > Value1;
        (Value0, Value1) = ((byte)~Value1, (byte)~Value0);
        Data ^= 0x2492492492490000UL; // v0 <-> v1, v2 <-> v3, v4 <-> v5, v6 <-> v7
        if (wasFullInterp)
            Data ^= (Data & 0x4924924924920000UL) << 1; // v2 <-> v6, v3 <-> v7, leave v0, v1, v4, v5 alone
        else
        {
            var mask = ((Data & 0x9249249249240000UL) >> 1) ^ (Data & 0x4924924924920000UL);
            Data ^= (mask << 1) | mask; // v2 <-> v4, v3 <-> v5, leave v0, v1, v6, v7 alone
        }
    }

    public readonly override bool Equals([NotNullWhen(true)] object? obj)
        => obj is Bc4Block other && Equals(other);

    public readonly override int GetHashCode()
        => HashCode.Combine(Data);

    public readonly bool Equals(Bc4Block other)
        => Data == other.Data;

    public readonly override string ToString()
        => $"[Bc2Block {FieldsToString()}]";

    public readonly string FieldsToString()
        => $"Value0={Value0:X2} Value1={Value1:X2} Indices={Data >> 16:X12}";

    public static bool operator ==(Bc4Block left, Bc4Block right)
        => left.Equals(right);

    public static bool operator !=(Bc4Block left, Bc4Block right)
        => !left.Equals(right);

    public static explicit operator ulong(Bc4Block block)
        => block.Data;

    public static explicit operator Bc4Block(ulong block)
        => new(block);
}
