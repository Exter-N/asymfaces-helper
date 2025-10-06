using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using static Unfold.Utility;

namespace Unfold;

public static partial class MaterialHandler
{
    public static void Process(string path, Dictionary<string, string> redirections)
    {
        var (materialRaceCode, materialFace) = ParseMaterialName(Path.GetFileNameWithoutExtension(path));
        redirections.TryAdd(
            $"chara/human/c{materialRaceCode:D4}/obj/face/f{materialFace:D4}/material/mt_c{materialRaceCode:D4}f{materialFace:D4}_fac_asym.mtrl",
            $"materials\\mt_c{materialRaceCode:D4}f{materialFace:D4}_fac_asym.mtrl");

        var originalMaterial = File.ReadAllBytes(path);
        var position = 0;

        ref var header = ref Read<Header>(originalMaterial, 1, ref position)[0];
        var textures = Read<Texture>(originalMaterial, header.TextureCount, ref position);
        var attributeSets = Read<AttributeSet>(originalMaterial, header.UvSetCount + header.ColorSetCount, ref position);
        var stringsPosition = position;
        var strings = DecodeStrings(Read<byte>(originalMaterial, header.StringTableSize, ref position));

        foreach (var offset in strings.Keys.ToList())
        {
            if (!TryParseTexturePath(strings[offset], out var textureRaceCode, out var textureFace, out var textureSuffix))
                continue;

            strings[offset] = $"chara/asymfaces/{GetGender(materialRaceCode)}_{GetRace(materialRaceCode)}_{materialFace:D4}_{textureSuffix}.tex";
            redirections.TryAdd(
                $"chara/asymfaces/{GetGender(materialRaceCode)}_{GetRace(materialRaceCode)}_{materialFace:D4}_{textureSuffix}.tex",
                $"textures\\{GetGender(textureRaceCode)}_{GetRace(textureRaceCode)}_{textureFace:D4}_{textureSuffix}.tex");
        }

        var (newStrings, mappings) = EncodeStrings(strings);
        header.StringTableSize = (ushort)newStrings.Length;
        header.ShaderPackageNameOffset = mappings[header.ShaderPackageNameOffset];
        foreach (ref var texture in textures)
            texture.PathOffset = mappings[texture.PathOffset];
        foreach (ref var attributeSet in attributeSets)
            attributeSet.NameOffset = mappings[attributeSet.NameOffset];

        Directory.CreateDirectory("asymfaces_vanilla/materials");
        using var newMaterial = File.Create($"asymfaces_vanilla/materials/mt_c{materialRaceCode:D4}f{materialFace:D4}_fac_asym.mtrl");
        newMaterial.Write(originalMaterial, 0, stringsPosition);
        newMaterial.Write(newStrings, 0, newStrings.Length);
        newMaterial.Write(originalMaterial, position, originalMaterial.Length - position);
    }

    private static (ushort RaceCode, ushort Face) ParseMaterialName(string name)
    {
        var parsedName = MaterialNameRegex().Match(name);
        if (!parsedName.Success)
            throw new Exception($"Cannot parse material name {name}");

        return (ushort.Parse(parsedName.Groups[1].Value), ushort.Parse(parsedName.Groups[2].Value));
    }

    [GeneratedRegex(@"mt_c(\d{4})f(\d{4})_fac_a")]
    private static partial Regex MaterialNameRegex();

    private static bool TryParseTexturePath(string path, out ushort raceCode, out ushort face, out string suffix)
    {
        var parsedPath = TexturePathRegex().Match(path);
        if (!parsedPath.Success)
        {
            raceCode = 0;
            face = 0;
            suffix = string.Empty;
            return false;
        }

        raceCode = ushort.Parse(parsedPath.Groups[1].Value);
        face = ushort.Parse(parsedPath.Groups[2].Value);
        suffix = parsedPath.Groups[3].Value;
        return true;
    }

    [GeneratedRegex(@"chara/human/c(\d{4})/obj/face/f(\d{4})/texture/c\1f\2_fac_([^.]*)\.tex", RegexOptions.Singleline)]
    private static partial Regex TexturePathRegex();

    private static Dictionary<ushort, string> DecodeStrings(ReadOnlySpan<byte> buffer)
    {
        var strings = new Dictionary<ushort, string>();
        var i = 0;
        while (buffer.Length > 0)
        {
            var zero = buffer.IndexOf((byte)0);
            if (zero >= 0)
            {
                if (zero > 0)
                    strings.Add((ushort)i, Encoding.UTF8.GetString(buffer[..zero]));
                buffer = buffer[(zero + 1)..];
                i += zero + 1;
            }
            else
            {
                strings.Add((ushort)i, Encoding.UTF8.GetString(buffer));
                break;
            }
        }

        return strings;
    }

    private static (byte[] Buffer, Dictionary<ushort, ushort> Mappings) EncodeStrings(
        Dictionary<ushort, string> strings)
    {
        using var stream = new MemoryStream();
        var mappings = new Dictionary<ushort, ushort>();
        var keys = strings.Keys.ToArray();
        Array.Sort(keys);
        foreach (var key in keys)
        {
            mappings.Add(key, (ushort)stream.Position);
            var bytes = Encoding.UTF8.GetBytes(strings[key]);
            stream.Write(bytes, 0, bytes.Length);
            stream.WriteByte(0);
        }

        while ((stream.Length & 3) != 0)
            stream.WriteByte(0);

        return (stream.ToArray(), mappings);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Header
    {
        public uint Version;
        public ushort FileSize;
        public ushort DataSetSize;
        public ushort StringTableSize;
        public ushort ShaderPackageNameOffset;
        public byte TextureCount;
        public byte UvSetCount;
        public byte ColorSetCount;
        public byte AdditionalDataSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Texture
    {
        public ushort PathOffset;
        public ushort Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AttributeSet
    {
        public ushort NameOffset;
        public ushort Index;
    }
}
