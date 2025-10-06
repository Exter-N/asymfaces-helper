using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using static Unfold.Utility;

namespace Unfold;

public static partial class MaterialHandler
{
    public static void Process(string path, Dictionary<ushort, Dictionary<string, string>> redirectionsPerRace, Dictionary<ushort, int> masksPerRace)
    {
        var (materialRaceCode, materialFace, materialSuffix) = ParseMaterialName(Path.GetFileNameWithoutExtension(path));
        if (materialSuffix is "fac_a")
            materialSuffix = "fac_asym";
        var redirections = redirectionsPerRace[materialRaceCode];
        redirections.TryAdd(
            $"chara/human/c{materialRaceCode:D4}/obj/face/f{materialFace:D4}/material/mt_c{materialRaceCode:D4}f{materialFace:D4}_{materialSuffix}.mtrl",
            $"materials\\mt_c{materialRaceCode:D4}f{materialFace:D4}_{materialSuffix}.mtrl");

        var originalMaterial = File.ReadAllBytes(path);
        var position = 0;

        ref var header = ref Read<Header>(originalMaterial, 1, ref position)[0];
        var textures = Read<Texture>(originalMaterial, header.TextureCount, ref position);
        var attributeSets = Read<AttributeSet>(originalMaterial, header.UvSetCount + header.ColorSetCount, ref position);
        var stringsPosition = position;
        var strings = DecodeStrings(Read<byte>(originalMaterial, header.StringTableSize, ref position));
        var tailPosition = position;
        var additionalData = Read<byte>(originalMaterial, header.AdditionalDataSize, ref position);
        var dataSet = Read<byte>(originalMaterial, header.DataSetSize, ref position);
        ref var shaderHeader = ref Read<ShaderHeader>(originalMaterial, 1, ref position)[0];
        var shaderKeys = Read<ShaderKey>(originalMaterial, shaderHeader.ShaderKeyCount, ref position);
        var constants = Read<Constant>(originalMaterial, shaderHeader.ConstantCount, ref position);
        var samplers = Read<Sampler>(originalMaterial, shaderHeader.SamplerCount, ref position);
        var valueList = Read<byte>(originalMaterial, shaderHeader.ShaderValueListSize, ref position);

        if (!TryFind(constants, c => c.NameCrc == 0x2E60B071u, out var tileScaleConstant))
            throw new Exception($"Cannot find g_TileScale constant in {path}");

        foreach (var offset in strings.Keys.ToList())
        {
            if (!TextureHandler.TryParseTexturePath(strings[offset], out var textureRaceCode, out var textureFace,
                    out var textureSuffix))
                continue;
            if (textureRaceCode != materialRaceCode)
                throw new Exception(
                    $"Race code mismatch, material: {GetGender(materialRaceCode)}_{GetRace(materialRaceCode)}, texture: {GetGender(textureRaceCode)}_{GetRace(textureRaceCode)}");

            strings[offset] = $"chara/asymfaces/{TextureHandler.GenerateAsymTextureName(textureRaceCode, textureFace, textureSuffix, masksPerRace)}.tex";
            redirections.TryAdd(
                $"chara/asymfaces/{TextureHandler.GenerateAsymTextureName(textureRaceCode, textureFace, textureSuffix, masksPerRace)}.tex",
                $"textures\\{TextureHandler.GenerateAsymTextureName(textureRaceCode, textureFace, textureSuffix, masksPerRace)}.tex");
        }

        var (newStrings, mappings) = EncodeStrings(strings);
        header.StringTableSize = (ushort)newStrings.Length;
        header.ShaderPackageNameOffset = mappings[header.ShaderPackageNameOffset];
        foreach (ref var texture in textures)
            texture.PathOffset = mappings[texture.PathOffset];
        foreach (ref var attributeSet in attributeSets)
            attributeSet.NameOffset = mappings[attributeSet.NameOffset];
        header.FileSize = (ushort)(stringsPosition + newStrings.Length + originalMaterial.Length - tailPosition);
        var tileScale = MemoryMarshal.Cast<byte, float>(valueList.Slice(tileScaleConstant.ByteOffset, tileScaleConstant.ByteSize));
        tileScale[0] *= 2.0f;

        Directory.CreateDirectory($"asymfaces_vanilla_{GetGender(materialRaceCode)}_{GetRace(materialRaceCode)}/materials");
        using var newMaterial = File.Create($"asymfaces_vanilla_{GetGender(materialRaceCode)}_{GetRace(materialRaceCode)}/materials/mt_c{materialRaceCode:D4}f{materialFace:D4}_{materialSuffix}.mtrl");
        newMaterial.Write(originalMaterial, 0, stringsPosition);
        newMaterial.Write(newStrings, 0, newStrings.Length);
        newMaterial.Write(originalMaterial, tailPosition, originalMaterial.Length - tailPosition);
    }

    public static (ushort RaceCode, ushort Face, string Suffix) ParseMaterialName(string name)
    {
        var parsedName = MaterialNameRegex().Match(name);
        if (!parsedName.Success)
            throw new Exception($"Cannot parse material name {name}");

        return (ushort.Parse(parsedName.Groups[1].Value), ushort.Parse(parsedName.Groups[2].Value), parsedName.Groups[3].Value);
    }

    [GeneratedRegex(@"^mt_c(\d{4})f(\d{4})_(.*)$")]
    private static partial Regex MaterialNameRegex();

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

    [StructLayout(LayoutKind.Sequential)]
    private struct ShaderHeader
    {
        public ushort ShaderValueListSize;
        public ushort ShaderKeyCount;
        public ushort ConstantCount;
        public ushort SamplerCount;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ShaderKey
    {
        public uint KeyCrc;
        public uint ValueCrc;
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct Constant
    {
        public uint NameCrc;
        public ushort ByteOffset;
        public ushort ByteSize;
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct Sampler {
        public uint NameCrc;
        public uint Flags;
        public byte TextureIndex;
        private byte _padding0;
        private ushort _padding1;
    };
}
