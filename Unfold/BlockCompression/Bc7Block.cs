using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static Unfold.ExtendedBitOperations;
using static Unfold.BlockCompression.Bc7Helpers;

namespace Unfold.BlockCompression;

[StructLayout(LayoutKind.Explicit, Size = 16)]
public struct Bc7Block(ulong lower, ulong upper) : IBcBlock, IEquatable<Bc7Block>
{
    [FieldOffset(0)] public ulong Lower = lower;
    [FieldOffset(8)] public ulong Upper = upper;

    public ulong this[BitRange range]
    {
        readonly get => ExtractBits128(new UInt128(Upper, Lower), range);
        set => ReplaceBits128(ref Unsafe.As<Bc7Block, UInt128>(ref this), range, value);
    }

    public readonly int Mode
        => BitOperations.TrailingZeroCount(Lower);

    public ulong this[Bc7Field field]
    {
        readonly get => this[GetFieldPosition(Mode, field)];
        set => this[GetFieldPosition(Mode, field)] = value;
    }

    public void Mirror()
    {
        var mode = Mode;
        var partitionPosition = GetFieldPosition(mode, Bc7Field.Partition);
        var partition = unchecked((byte)this[partitionPosition]);
        var (indices0, indices1) = GetIndices();
        var (newPartition, reordering) = GetPartitionMirror(mode, partition);
        this[partitionPosition] = newPartition;
        if ((reordering & 4) != 0)
            SwapSubsets23();
        if ((reordering & 2) != 0)
            SwapSubsets13();
        if ((reordering & 1) != 0)
            SwapSubsets12();
        indices0 = MirrorIndices(indices0);
        indices1 = MirrorIndices(indices1);
        SetIndices(indices0, indices1);
    }

    public void InvertRed()
    {
        var mode = Mode;
        var position = GetFieldPosition(mode,
            this[GetFieldPosition(mode, Bc7Field.Rotation)] == 1UL ? Bc7Field.AlphaAll : Bc7Field.RedAll);
        Unsafe.As<Bc7Block, UInt128>(ref this) ^= position.GetMask<UInt128>() << position.Start;
    }

    public readonly override bool Equals([NotNullWhen(true)] object? obj)
        => obj is Bc7Block other && Equals(other);

    public readonly override int GetHashCode()
        => HashCode.Combine(Lower, Upper);

    public readonly bool Equals(Bc7Block other)
        => Lower == other.Lower && Upper == other.Upper;

    public readonly override string ToString()
        => $"[Bc7Block {FieldsToString()}]";

    public readonly string FieldsToString()
    {
        var mode = Mode;
        var sb = new StringBuilder();
        sb.Append($"Mode={mode}");
        for (var field = Bc7Field.Partition; field <= Bc7Field.Indices2; ++field)
        {
            if (field is Bc7Field.RedAll or Bc7Field.GreenAll or Bc7Field.BlueAll or Bc7Field.AlphaAll
                or Bc7Field.EndpointPAll or Bc7Field.SharedPAll)
                continue;

            var position = GetFieldPosition(mode, field);
            if (position.Length is 0)
                continue;

            sb.Append($" {field}=");
            sb.AppendFormat(this[position].ToString($"X{(position.Length + 3) >> 2}"));
        }

        return sb.ToString();
    }

    private readonly string BitsAsString(BitRange range)
        => BitsAsString(this[range], range.Length);

    private static string BitsAsString(ulong value, int bits)
        => Convert.ToString(unchecked((long)value), 2).PadLeft(bits, '0');

    public readonly (ulong, ulong) GetIndices()
    {
        var mode = Mode;
        var (indexBpt, index2Bpt) = GetIndexBitsPerTexel(mode);
        if (indexBpt == 0)
            return (0UL, 0UL);

        var indicesPosition = GetFieldPosition(mode, Bc7Field.Indices);
        var indices = InsertBit(this[indicesPosition], indexBpt - 1);
        if (index2Bpt > 0)
        {
            indices = Widen2To4(unchecked((uint)indices));

            var indices2Position = GetFieldPosition(mode, Bc7Field.Indices2);
            var indices2 = InsertBit(this[indices2Position], index2Bpt - 1);
            indices2 = index2Bpt == 3
                ? Widen3To4(indices2)
                : Widen2To4(unchecked((uint)indices2));

            return this[GetFieldPosition(mode, Bc7Field.IndexSelection)] == 1
                ? (indices2, indices)
                : (indices, indices2);
        }

        var subsetCount = GetSubsetCount(mode);
        if (subsetCount > 1)
        {
            var partition = unchecked((byte)this[GetFieldPosition(mode, Bc7Field.Partition)]);
            var (anchor1, anchor2) = GetPartitionAnchorTexels(mode, partition);
            if (subsetCount > 2)
            {
                var (anchorL, anchorU) = anchor1 < anchor2 ? (anchor1, anchor2) : (anchor2, anchor1);
                indices = InsertBit(indices, (anchorL + 1) * indexBpt - 1);
                indices = InsertBit(indices, (anchorU + 1) * indexBpt - 1);
            }
            else
            {
                indices = InsertBit(indices, (anchor1 + 1) * indexBpt - 1);
            }
        }

        return (indexBpt switch
        {
            2 => Widen2To4(unchecked((uint)indices)),
            3 => Widen3To4(indices),
            _ => indices,
        }, 0UL);
    }

    public void SetIndices(ulong indices, ulong indices2)
    {
        var mode = Mode;
        var (indexBpt, index2Bpt) = GetIndexBitsPerTexel(mode);
        if (indexBpt == 0)
            return;

        var indexField = Bc7Field.Indices;
        if (index2Bpt > 0)
        {
            var index2Field = Bc7Field.Indices2;
            if (this[GetFieldPosition(mode, Bc7Field.IndexSelection)] == 1)
            {
                (indexBpt, index2Bpt) = (index2Bpt, indexBpt);
                indexField = Bc7Field.Indices2;
                index2Field = Bc7Field.Indices;
            }

            if ((indices2 & 0x8UL) != 0UL)
            {
                indices2 = ~indices2;
                FlipSecondary();
            }

            indices2 = index2Bpt == 3
                ? Narrow4To3(indices2)
                : Narrow4To2(indices2);

            var indices2Position = GetFieldPosition(mode, index2Field);
            this[indices2Position] = RemoveBit(indices2, index2Bpt - 1);
        }

        var partition = unchecked((byte)this[GetFieldPosition(mode, Bc7Field.Partition)]);
        var pattern = GetPartitionPattern(mode, partition);
        var lMask = pattern & 0x55555555u;
        var uMask = (pattern & 0xAAAAAAAAu) >> 1;
        var s1Mask = ExpandPatternMask((lMask | uMask) ^ 0x55555555u);
        var s2Mask = ExpandPatternMask(lMask & ~uMask);
        var s3Mask = ExpandPatternMask(uMask & ~lMask);

        if ((indices & 0x8UL) != 0UL)
        {
            indices ^= s1Mask;
            FlipSubset1();
        }

        var subsetCount = GetSubsetCount(mode);
        var (anchor1, anchor2) = GetPartitionAnchorTexels(mode, partition);

        if (subsetCount > 1 && (indices & (1UL << ((anchor1 << 2) | 3))) != 0UL)
        {
            indices ^= s2Mask;
            FlipSubset2();
        }

        if (subsetCount > 2 && (indices & (1UL << ((anchor2 << 2) | 3))) != 0UL)
        {
            indices ^= s3Mask;
            FlipSubset3();
        }

        indices = indexBpt switch
        {
            2 => Narrow4To2(indices),
            3 => Narrow4To3(indices),
            _ => indices,
        };

        if (subsetCount > 1)
        {
            if (subsetCount > 2)
            {
                var (anchorL, anchorU) = anchor1 < anchor2 ? (anchor1, anchor2) : (anchor2, anchor1);
                indices = RemoveBit(indices, (anchorU + 1) * indexBpt - 1);
                indices = RemoveBit(indices, (anchorL + 1) * indexBpt - 1);
            }
            else
            {
                indices = RemoveBit(indices, (anchor1 + 1) * indexBpt - 1);
            }
        }

        var indicesPosition = GetFieldPosition(mode, indexField);
        this[indicesPosition] = RemoveBit(indices, indexBpt - 1);
    }

    private void SwapSubsets12()
    {
        var mode = Mode;
        if (GetSubsetCount(mode) < 2)
            return;

        var redA = GetFieldPosition(mode, Bc7Field.Red0) + GetFieldPosition(mode, Bc7Field.Red1);
        var redB = GetFieldPosition(mode, Bc7Field.Red2) + GetFieldPosition(mode, Bc7Field.Red3);
        var greenA = GetFieldPosition(mode, Bc7Field.Green0) + GetFieldPosition(mode, Bc7Field.Green1);
        var greenB = GetFieldPosition(mode, Bc7Field.Green2) + GetFieldPosition(mode, Bc7Field.Green3);
        var blueA = GetFieldPosition(mode, Bc7Field.Blue0) + GetFieldPosition(mode, Bc7Field.Blue1);
        var blueB = GetFieldPosition(mode, Bc7Field.Blue2) + GetFieldPosition(mode, Bc7Field.Blue3);
        var alphaA = GetFieldPosition(mode, Bc7Field.Alpha0) + GetFieldPosition(mode, Bc7Field.Alpha1);
        var alphaB = GetFieldPosition(mode, Bc7Field.Alpha2) + GetFieldPosition(mode, Bc7Field.Alpha3);
        var endPA = GetFieldPosition(mode, Bc7Field.EndpointP0) + GetFieldPosition(mode, Bc7Field.EndpointP1);
        var endPB = GetFieldPosition(mode, Bc7Field.EndpointP2) + GetFieldPosition(mode, Bc7Field.EndpointP3);
        var shrPA = GetFieldPosition(mode, Bc7Field.SharedP0);
        var shrPB = GetFieldPosition(mode, Bc7Field.SharedP1);

        (this[redB], this[redA]) = (this[redA], this[redB]);
        (this[greenB], this[greenA]) = (this[greenA], this[greenB]);
        (this[blueB], this[blueA]) = (this[blueA], this[blueB]);
        (this[alphaB], this[alphaA]) = (this[alphaA], this[alphaB]);
        (this[endPB], this[endPA]) = (this[endPA], this[endPB]);
        (this[shrPB], this[shrPA]) = (this[shrPA], this[shrPB]);
    }

    private void SwapSubsets13()
    {
        var mode = Mode;
        if (GetSubsetCount(mode) < 3)
            return;

        var redA = GetFieldPosition(mode, Bc7Field.Red0) + GetFieldPosition(mode, Bc7Field.Red1);
        var redB = GetFieldPosition(mode, Bc7Field.Red4) + GetFieldPosition(mode, Bc7Field.Red5);
        var greenA = GetFieldPosition(mode, Bc7Field.Green0) + GetFieldPosition(mode, Bc7Field.Green1);
        var greenB = GetFieldPosition(mode, Bc7Field.Green4) + GetFieldPosition(mode, Bc7Field.Green5);
        var blueA = GetFieldPosition(mode, Bc7Field.Blue0) + GetFieldPosition(mode, Bc7Field.Blue1);
        var blueB = GetFieldPosition(mode, Bc7Field.Blue4) + GetFieldPosition(mode, Bc7Field.Blue5);
        var endPA = GetFieldPosition(mode, Bc7Field.EndpointP0) + GetFieldPosition(mode, Bc7Field.EndpointP1);
        var endPB = GetFieldPosition(mode, Bc7Field.EndpointP4) + GetFieldPosition(mode, Bc7Field.EndpointP5);

        (this[redB], this[redA]) = (this[redA], this[redB]);
        (this[greenB], this[greenA]) = (this[greenA], this[greenB]);
        (this[blueB], this[blueA]) = (this[blueA], this[blueB]);
        (this[endPB], this[endPA]) = (this[endPA], this[endPB]);
    }

    private void SwapSubsets23()
    {
        var mode = Mode;
        if (GetSubsetCount(mode) < 3)
            return;

        var redA = GetFieldPosition(mode, Bc7Field.Red2) + GetFieldPosition(mode, Bc7Field.Red3);
        var redB = GetFieldPosition(mode, Bc7Field.Red4) + GetFieldPosition(mode, Bc7Field.Red5);
        var greenA = GetFieldPosition(mode, Bc7Field.Green2) + GetFieldPosition(mode, Bc7Field.Green3);
        var greenB = GetFieldPosition(mode, Bc7Field.Green4) + GetFieldPosition(mode, Bc7Field.Green5);
        var blueA = GetFieldPosition(mode, Bc7Field.Blue2) + GetFieldPosition(mode, Bc7Field.Blue3);
        var blueB = GetFieldPosition(mode, Bc7Field.Blue4) + GetFieldPosition(mode, Bc7Field.Blue5);
        var endPA = GetFieldPosition(mode, Bc7Field.EndpointP2) + GetFieldPosition(mode, Bc7Field.EndpointP3);
        var endPB = GetFieldPosition(mode, Bc7Field.EndpointP4) + GetFieldPosition(mode, Bc7Field.EndpointP5);

        (this[redB], this[redA]) = (this[redA], this[redB]);
        (this[greenB], this[greenA]) = (this[greenA], this[greenB]);
        (this[blueB], this[blueA]) = (this[blueA], this[blueB]);
        (this[endPB], this[endPA]) = (this[endPA], this[endPB]);
    }

    private void FlipSubset1()
    {
        var mode = Mode;
        if (GetSubsetCount(mode) < 1)
            return;

        var red0 = GetFieldPosition(mode, Bc7Field.Red0);
        var red1 = GetFieldPosition(mode, Bc7Field.Red1);
        var green0 = GetFieldPosition(mode, Bc7Field.Green0);
        var green1 = GetFieldPosition(mode, Bc7Field.Green1);
        var blue0 = GetFieldPosition(mode, Bc7Field.Blue0);
        var blue1 = GetFieldPosition(mode, Bc7Field.Blue1);
        var endP0 = GetFieldPosition(mode, Bc7Field.EndpointP0);
        var endP1 = GetFieldPosition(mode, Bc7Field.EndpointP1);

        if (GetIndexBitsPerTexel(mode).Item2 == 0)
        {
            var alpha0 = GetFieldPosition(mode, Bc7Field.Alpha0);
            var alpha1 = GetFieldPosition(mode, Bc7Field.Alpha1);

            (this[alpha1], this[alpha0]) = (this[alpha0], this[alpha1]);
        }

        (this[red1], this[red0]) = (this[red0], this[red1]);
        (this[green1], this[green0]) = (this[green0], this[green1]);
        (this[blue1], this[blue0]) = (this[blue0], this[blue1]);
        (this[endP1], this[endP0]) = (this[endP0], this[endP1]);
    }

    private void FlipSubset2()
    {
        var mode = Mode;
        if (GetSubsetCount(mode) < 2)
            return;

        var red0 = GetFieldPosition(mode, Bc7Field.Red2);
        var red1 = GetFieldPosition(mode, Bc7Field.Red3);
        var green0 = GetFieldPosition(mode, Bc7Field.Green2);
        var green1 = GetFieldPosition(mode, Bc7Field.Green3);
        var blue0 = GetFieldPosition(mode, Bc7Field.Blue2);
        var blue1 = GetFieldPosition(mode, Bc7Field.Blue3);
        var alpha0 = GetFieldPosition(mode, Bc7Field.Alpha2);
        var alpha1 = GetFieldPosition(mode, Bc7Field.Alpha3);
        var endP0 = GetFieldPosition(mode, Bc7Field.EndpointP2);
        var endP1 = GetFieldPosition(mode, Bc7Field.EndpointP3);

        (this[red1], this[red0]) = (this[red0], this[red1]);
        (this[green1], this[green0]) = (this[green0], this[green1]);
        (this[blue1], this[blue0]) = (this[blue0], this[blue1]);
        (this[alpha1], this[alpha0]) = (this[alpha0], this[alpha1]);
        (this[endP1], this[endP0]) = (this[endP0], this[endP1]);
    }

    private void FlipSubset3()
    {
        var mode = Mode;
        if (GetSubsetCount(mode) < 3)
            return;

        var red0 = GetFieldPosition(mode, Bc7Field.Red4);
        var red1 = GetFieldPosition(mode, Bc7Field.Red5);
        var green0 = GetFieldPosition(mode, Bc7Field.Green4);
        var green1 = GetFieldPosition(mode, Bc7Field.Green5);
        var blue0 = GetFieldPosition(mode, Bc7Field.Blue4);
        var blue1 = GetFieldPosition(mode, Bc7Field.Blue5);
        var endP0 = GetFieldPosition(mode, Bc7Field.EndpointP4);
        var endP1 = GetFieldPosition(mode, Bc7Field.EndpointP5);

        (this[red1], this[red0]) = (this[red0], this[red1]);
        (this[green1], this[green0]) = (this[green0], this[green1]);
        (this[blue1], this[blue0]) = (this[blue0], this[blue1]);
        (this[endP1], this[endP0]) = (this[endP0], this[endP1]);
    }

    private void FlipSecondary()
    {
        var mode = Mode;
        if (GetIndexBitsPerTexel(mode).Item2 == 0)
            return;

        var alpha0 = GetFieldPosition(mode, Bc7Field.Alpha0);
        var alpha1 = GetFieldPosition(mode, Bc7Field.Alpha1);

        (this[alpha1], this[alpha0]) = (this[alpha0], this[alpha1]);
    }

    public static bool operator ==(Bc7Block left, Bc7Block right)
        => left.Equals(right);

    public static bool operator !=(Bc7Block left, Bc7Block right)
        => !left.Equals(right);

    public static explicit operator UInt128(Bc7Block block)
        => new(block.Upper, block.Lower);

    public static explicit operator Bc7Block(UInt128 block)
        => Unsafe.BitCast<UInt128, Bc7Block>(block);
}
