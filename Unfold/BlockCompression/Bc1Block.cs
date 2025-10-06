using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using static Unfold.ExtendedBitOperations;

namespace Unfold.BlockCompression;

[StructLayout(LayoutKind.Explicit, Size = 8)]
public struct Bc1Block(ulong data) : IBcBlock, IEquatable<Bc1Block>
{
    [FieldOffset(0)] public ushort Color0;
    [FieldOffset(2)] public ushort Color1;
    [FieldOffset(4)] public uint Indices;

    [FieldOffset(0)] public ulong Data = data;

    public ulong this[BitRange range]
    {
        readonly get => ExtractBits(Data, range);
        set => Data = ReplaceBits(Data, range, value);
    }

    public void Mirror()
    {
        Indices = ((Indices & 0x33333333u) << 2) | ((Indices & 0xCCCCCCCCu) >> 2);
        Indices = ((Indices & 0x0F0F0F0Fu) << 4) | ((Indices & 0xF0F0F0F0u) >> 4);
    }

    void IBcBlock.InvertRed()
        => InvertRed();

    public void InvertRed(bool forceFourColor = false)
    {
        var wasFourColor = Color0 > Color1;
        Color0 ^= 0xF800;
        Color1 ^= 0xF800;
        if ((Color0 > Color1) ^ wasFourColor)
        {
            (Color0, Color1) = (Color1, Color0);
            if (wasFourColor || forceFourColor)
                Indices ^= 0x55555555; // c0 <-> c1, c2 <-> c3
            else
                Indices ^= (~Indices & 0xAAAAAAAA) >> 1; // c0 <-> c1, leave c2 and c3 alone
        }
    }

    public readonly override bool Equals([NotNullWhen(true)] object? obj)
        => obj is Bc1Block other && Equals(other);

    public readonly override int GetHashCode()
        => HashCode.Combine(Data);

    public readonly bool Equals(Bc1Block other)
        => Data == other.Data;

    public readonly override string ToString()
        => $"[Bc1Block {FieldsToString()}]";

    public readonly string FieldsToString()
        => $"Color0={Color0:X4} Color1={Color1:X4} Indices={Indices:X8}";

    public static bool operator ==(Bc1Block left, Bc1Block right)
        => left.Equals(right);

    public static bool operator !=(Bc1Block left, Bc1Block right)
        => !left.Equals(right);

    public static explicit operator ulong(Bc1Block block)
        => block.Data;

    public static explicit operator Bc1Block(ulong block)
        => new(block);
}
