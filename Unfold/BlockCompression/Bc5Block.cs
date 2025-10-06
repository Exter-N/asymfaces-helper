using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Unfold.ExtendedBitOperations;

namespace Unfold.BlockCompression;

[StructLayout(LayoutKind.Sequential)]
public struct Bc5Block(Bc4Block redBlock, Bc4Block greenBlock) : IBcBlock
{
    public Bc4Block RedBlock = redBlock;
    public Bc4Block GreenBlock = greenBlock;

    public ulong this[BitRange range]
    {
        readonly get => ExtractBits128(new(GreenBlock.Data, RedBlock.Data), range);
        set => ReplaceBits128(ref Unsafe.As<Bc5Block, UInt128>(ref this), range, value);
    }

    public void Mirror()
    {
        RedBlock.Mirror();
        GreenBlock.Mirror();
    }

    public void InvertRed()
        => RedBlock.InvertRed();

    public readonly override bool Equals([NotNullWhen(true)] object? obj)
        => obj is Bc5Block other && Equals(other);

    public readonly override int GetHashCode()
        => HashCode.Combine(RedBlock.Data, GreenBlock.Data);

    public readonly bool Equals(Bc5Block other)
        => RedBlock.Equals(other.RedBlock) && GreenBlock.Equals(other.GreenBlock);

    public readonly override string ToString()
        => $"[Bc5Block {FieldsToString()}]";

    public readonly string FieldsToString()
        => $"Red={{{RedBlock.FieldsToString()}}} Green={{{GreenBlock.FieldsToString()}}}";

    public static bool operator ==(Bc5Block left, Bc5Block right)
        => left.Equals(right);

    public static bool operator !=(Bc5Block left, Bc5Block right)
        => !left.Equals(right);

    public static explicit operator UInt128(Bc5Block block)
        => new(block.GreenBlock.Data, block.RedBlock.Data);

    public static explicit operator Bc5Block(UInt128 block)
        => Unsafe.BitCast<UInt128, Bc5Block>(block);
}
