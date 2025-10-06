using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Unfold;

public static class Utility
{
    public static string GetGender(ushort raceCode)
        => (raceCode / 100) % 2 == 1 ? "male" : "female";

    public static string GetDisplayGender(ushort raceCode)
        => ToTitleCase(GetGender(raceCode));

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

    public static string GetDisplayRace(ushort raceCode)
        => GetRace(raceCode) switch
        {
            "miqote" => "Miqo'te",
            "aura" => "Au Ra",
            var race => ToTitleCase(race),
        };

    public static string ToTitleCase(string str)
        => $"{char.ToUpper(str[0])}{str[1..]}";

    public static IEnumerable<ushort> AllRaceCodes()
    {
        for (ushort raceCode = 101; raceCode < 1900; raceCode += 100)
            yield return raceCode;
    }

    public static unsafe Span<T> Read<T>(Span<byte> buffer, int count, ref int position) where T : unmanaged
    {
        var length = sizeof(T) * count;
        var span = MemoryMarshal.Cast<byte, T>(buffer.Slice(position, length));
        position += length;

        return span;
    }

    public static bool TryFind<T>(Span<T> span, Predicate<T> predicate, [MaybeNullWhen(false)] out T result)
    {
        foreach (var element in span)
        {
            if (predicate(element))
            {
                result = element;
                return true;
            }
        }

        result = default;
        return false;
    }

    public static TValue GetOrCreate<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key)
        where TKey : notnull where TValue : new()
    {
        if (dictionary.TryGetValue(key, out var value))
            return value;

        value = new();
        dictionary.Add(key, value);
        return value;
    }
}
