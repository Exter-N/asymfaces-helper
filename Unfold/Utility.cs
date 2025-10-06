using System.Runtime.InteropServices;

namespace Unfold;

public static class Utility
{
    public static string GetGender(ushort raceCode)
        => (raceCode / 100) % 2 == 1 ? "male" : "female";

    public static string GetRace(ushort raceCode)
        => ((raceCode - 100) / 200) switch
        {
            0 => "midlander",
            1 => "highlander",
            2 => "elezen",
            3 => "miqote",
            4 => "roegadyn",
            5 => "lalafell",
            6 => "aura",
            7 => "hrothgar",
            8 => "viera",
            _ => throw new Exception($"Unsupported race code {raceCode}"),
        };

    public static unsafe Span<T> Read<T>(Span<byte> buffer, int count, ref int position) where T : unmanaged
    {
        var length = sizeof(T) * count;
        var span = MemoryMarshal.Cast<byte, T>(buffer.Slice(position, length));
        position += length;

        return span;
    }
}
