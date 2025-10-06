using System.Numerics;

namespace Unfold;

public readonly struct BitRange(int start, int end)
{
    public readonly int Start = start >= 0 ? start : throw new ArgumentOutOfRangeException(nameof(start));
    public readonly int End = end >= start ? end : throw new ArgumentOutOfRangeException(nameof(end));

    public int Length
        => End - Start;

    public unsafe T GetMask<T>() where T : unmanaged, IBinaryInteger<T>
    {
        var length = Length;
        if (length > (sizeof(T) << 3))
            throw new InvalidOperationException($"Cannot get {typeof(T)} mask for bit range {this}");

        return (T.One << length) - T.One;
    }

    public override string ToString()
        => $"{Start}..{End}";

    public static BitRange operator +(BitRange lhs, BitRange rhs)
        => lhs.End == rhs.Start
            ? new(lhs.Start, rhs.End)
            : throw new ArgumentException("Ranges are not contiguous");
}
