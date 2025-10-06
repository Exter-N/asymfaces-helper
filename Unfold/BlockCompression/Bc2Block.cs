using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Unfold.ExtendedBitOperations;

namespace Unfold.BlockCompression;

[StructLayout(LayoutKind.Sequential)]
public struct Bc2Block(ulong alpha, Bc1Block colorBlock) : IBcBlock, IEquatable<Bc2Block>
{
    public ulong Alpha = alpha;
    public Bc1Block ColorBlock = colorBlock;

    public ulong this[BitRange range]
    {
        readonly get => ExtractBits128((UInt128)this, range);
        set => ReplaceBits128(ref Unsafe.As<Bc2Block, UInt128>(ref this), range, value);
    }

    public void Mirror()
    {
        Alpha = MirrorIndices(Alpha);
        ColorBlock.Mirror();
    }

    public void InvertRed()
        => ColorBlock.InvertRed(true);

    public readonly override bool Equals([NotNullWhen(true)] object? obj)
        => obj is Bc2Block other && Equals(other);

    public readonly override int GetHashCode()
        => HashCode.Combine(Alpha, ColorBlock.Data);

    public readonly bool Equals(Bc2Block other)
        => Alpha == other.Alpha && ColorBlock.Equals(other.ColorBlock);

    public readonly override string ToString()
        => $"[Bc2Block {FieldsToString()}]";

    public readonly string FieldsToString()
        => $"Alpha={Alpha:X16} Color={{{ColorBlock.FieldsToString()}}}";

    public static bool operator ==(Bc2Block left, Bc2Block right)
        => left.Equals(right);

    public static bool operator !=(Bc2Block left, Bc2Block right)
        => !left.Equals(right);

    public static explicit operator UInt128(Bc2Block block)
        => new(block.ColorBlock.Data, block.Alpha);

    public static explicit operator Bc2Block(UInt128 block)
        => Unsafe.BitCast<UInt128, Bc2Block>(block);
}
