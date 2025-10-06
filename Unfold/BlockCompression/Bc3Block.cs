using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Unfold.ExtendedBitOperations;

namespace Unfold.BlockCompression;

[StructLayout(LayoutKind.Sequential)]
public struct Bc3Block(Bc4Block alphaBlock, Bc1Block colorBlock) : IBcBlock, IEquatable<Bc3Block>
{
    public Bc4Block AlphaBlock = alphaBlock;
    public Bc1Block ColorBlock = colorBlock;

    public ulong this[BitRange range]
    {
        readonly get => ExtractBits128(new(ColorBlock.Data, AlphaBlock.Data), range);
        set => ReplaceBits128(ref Unsafe.As<Bc3Block, UInt128>(ref this), range, value);
    }

    public void Mirror()
    {
        AlphaBlock.Mirror();
        ColorBlock.Mirror();
    }

    public void InvertRed()
        => ColorBlock.InvertRed(true);

    public readonly override bool Equals([NotNullWhen(true)] object? obj)
        => obj is Bc3Block other && Equals(other);

    public readonly override int GetHashCode()
        => HashCode.Combine(AlphaBlock.Data, ColorBlock.Data);

    public readonly bool Equals(Bc3Block other)
        => AlphaBlock.Equals(other.AlphaBlock) && ColorBlock.Equals(other.ColorBlock);

    public readonly override string ToString()
        => $"[Bc3Block {FieldsToString()}]";

    public readonly string FieldsToString()
        => $"Alpha={{{AlphaBlock.FieldsToString()}}} Color={{{ColorBlock.FieldsToString()}}}";

    public static bool operator ==(Bc3Block left, Bc3Block right)
        => left.Equals(right);

    public static bool operator !=(Bc3Block left, Bc3Block right)
        => !left.Equals(right);

    public static explicit operator UInt128(Bc3Block block)
        => new(block.ColorBlock.Data, block.AlphaBlock.Data);

    public static explicit operator Bc3Block(UInt128 block)
        => Unsafe.BitCast<UInt128, Bc3Block>(block);
}
