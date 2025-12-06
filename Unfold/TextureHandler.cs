using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using BCnEncoder.Decoder;
using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using BCnEncoder.Shared.ImageFiles;
using Unfold.BlockCompression;
using static Unfold.Utility;

namespace Unfold;

public static partial class TextureHandler
{
    private const uint Texture2D = 0x800000;
    private const uint Bc1 = 0x3420;
    private const uint Bc2 = 0x3430;
    private const uint Bc3 = 0x3431;
    private const uint Bc4 = 0x6120;
    private const uint Bc5 = 0x6230;
    private const uint Bc7 = 0x6432;

    private static int _slowBlocks = 0;

    public static void Process(string path, Dictionary<ushort, int> masksPerRace)
    {
        var (textureRaceCode, textureFace, textureSuffix) = ParseTextureName(Path.GetFileNameWithoutExtension(path));
        var isNormal = string.Equals(textureSuffix, "norm", StringComparison.OrdinalIgnoreCase);

        var originalTexture = File.ReadAllBytes(path);
        var position = 0;

        var header = Read<Header>(originalTexture, 1, ref position)[0];
        if (header.Type is not Texture2D)
            throw new NotImplementedException($"Only 2D textures are handled, {path} is 0x{header.Type:X}");
        if (header.Format is not Bc1 and not Bc2 and not Bc3 and not Bc4 and not Bc5 and not Bc7)
            throw new NotImplementedException(
                $"Only BC1/2/3/4/5/7 textures are handled, {path} is 0x{header.Format:X}");

        var newSurfaces = new List<byte[]>();
        for (var i = 0; i < Math.Min(SurfaceOffsets.Length, header.MipCount & 0x7F); ++i)
        {
            if (header.SurfaceOffsets[i] == 0)
                break;
            var (width, height) = CalculateSurfaceDimensions(in header, i);
            if ((width & 3) != 0 || (height & 3) != 0)
                break;

            _slowBlocks = 0;

            newSurfaces.Add(UnfoldSurface(
                originalTexture.AsSpan((int)header.SurfaceOffsets[i], CalculateSurfaceSize(in header, i)), width >> 2,
                header.Format, isNormal));

            if (_slowBlocks > 0)
            {
                Console.WriteLine(
                    $"{GenerateAsymTextureName(textureRaceCode, textureFace, textureSuffix, masksPerRace)}: mip{i}: {_slowBlocks} slow blocks");
            }
        }

        header.Width <<= 1;
        for (var i = 0; i < SurfaceOffsets.Length; ++i)
        {
            if (i >= newSurfaces.Count)
                header.SurfaceOffsets[i] = 0;
            else
            {
                header.SurfaceOffsets[i] = (uint)position;
                position += newSurfaces[i].Length;
            }
        }

        for (var i = 0; i < LodOffsets.Length; ++i)
            header.LodOffsets[i] = (uint)Math.Min(i, newSurfaces.Count - 1);

        header.MipCount = (byte)((header.MipCount & 0x80) | newSurfaces.Count);

        Directory.CreateDirectory($"asymfaces_vanilla_{GetGender(textureRaceCode)}_{GetRace(textureRaceCode)}/textures");
        using (var newTexture =
               File.Create(
                   $"asymfaces_vanilla_{GetGender(textureRaceCode)}_{GetRace(textureRaceCode)}/textures/{GenerateAsymTextureName(textureRaceCode, textureFace, textureSuffix, masksPerRace)}.tex"))
        {
            newTexture.Write(MemoryMarshal.AsBytes(new ReadOnlySpan<Header>(ref header)));
            foreach (var surface in newSurfaces)
                newTexture.Write(surface, 0, surface.Length);
        }

        using (var newTexture =
               File.Create(
                   $"asymfaces_vanilla_textures_{GetGender(textureRaceCode)}_{GetRace(textureRaceCode)}/{GenerateAsymTextureName(textureRaceCode, textureFace, textureSuffix, masksPerRace)}.dds"))
        {
            using var writer = new BinaryWriter(newTexture, Encoding.UTF8, true);
            writer.Write(0x20534444u);

            var ddsHeader = new DdsHeader();
            ddsHeader.dwSize = 124;
            ddsHeader.dwFlags = HeaderFlags.Required;
            ddsHeader.dwWidth = header.Width;
            ddsHeader.dwHeight = header.Height;
            ddsHeader.dwDepth = header.Depth;
            ddsHeader.dwMipMapCount = header.MipCount & 0x7Fu;
            ddsHeader.dwCaps = HeaderCaps.DdscapsTexture | HeaderCaps.DdscapsComplex | HeaderCaps.DdscapsMipmap;
            ddsHeader.ddsPixelFormat = new DdsPixelFormat
            {
                dwSize = 32,
                dwFlags = PixelFormatFlags.DdpfFourcc,
                dwFourCc = ToDdsPixelFormat(header.Format),
            };
            writer.Write(MemoryMarshal.AsBytes(new ReadOnlySpan<DdsHeader>(ref ddsHeader)));

            if (ddsHeader.ddsPixelFormat.dwFourCc == DdsPixelFormat.Dx10)
            {
                var dx10Header = new DdsHeaderDx10();
                dx10Header.arraySize = Math.Max(header.ArraySize, (byte)1);
                dx10Header.dxgiFormat = ToDxgiFormat(header.Format);
                dx10Header.resourceDimension = D3D10ResourceDimension.D3D10ResourceDimensionTexture2D;
                writer.Write(MemoryMarshal.AsBytes(new ReadOnlySpan<DdsHeaderDx10>(ref dx10Header)));
            }

            foreach (var surface in newSurfaces)
                newTexture.Write(surface, 0, surface.Length);
        }
    }

    public static (ushort RaceCode, ushort Face, string Suffix) ParseTextureName(string name)
    {
        var parsedName = TextureNameRegex().Match(name);
        if (!parsedName.Success)
            throw new Exception($"Cannot parse material name {name}");

        return (ushort.Parse(parsedName.Groups[1].Value), ushort.Parse(parsedName.Groups[2].Value),
            parsedName.Groups[3].Value);
    }

    [GeneratedRegex(@"^c(\d{4})f(\d{4})_fac_(.*)$", RegexOptions.Singleline)]
    private static partial Regex TextureNameRegex();

    public static bool TryParseTexturePath(string path, out ushort raceCode, out ushort face, out string suffix)
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

    public static string GenerateAsymTextureName(ushort raceCode, ushort face, string suffix, Dictionary<ushort, int> masksPerRace)
        => string.Equals(suffix, "mask", StringComparison.OrdinalIgnoreCase) && masksPerRace[raceCode] == 1
            ? $"{GetGender(raceCode)}_{GetRace(raceCode)}_{suffix}"
            : $"{GetGender(raceCode)}_{GetRace(raceCode)}_f{face:D3}_{suffix}";

    private static (int Width, int Height) CalculateSurfaceDimensions(in Header header, int mip)
        => ((header.Width + (1 << mip) - 1) >> mip, (header.Height + (1 << mip) - 1) >> mip);

    private static int CalculateSurfaceSize(in Header header, int mip)
    {
        var type = (header.Format & 0xF000) >> 12;
        var bpp = 1 << (int)((header.Format & 0xF0) >> 4);
        var (width, height) = CalculateSurfaceDimensions(in header, mip);
        if (type is 3 or 6)
        {
            width = (width + 3) & ~3;
            height = (height + 3) & ~3;
        }

        var bits = width * height * bpp;
        return (bits + 7) >> 3;
    }

    private static byte[] UnfoldSurface(ReadOnlySpan<byte> surface, int width, uint format, bool isNormal)
    {
        var bpb = 16 << (int)((format & 0xF0) >> 4);
        var pitch = (width * bpb + 7) >> 3;
        if (surface.Length % pitch != 0)
            throw new ArgumentException(
                $"Surface length ({surface.Length}) not a multiple of pitch ({pitch}) with width {width} and format 0x{format:X}");

        var newSurface = new byte[surface.Length << 1];
        for (var i = 0; i < surface.Length; i += pitch)
        {
            var sourceRow = surface.Slice(i, pitch);
            MirrorTo(sourceRow, newSurface.AsSpan(i << 1, pitch), format, isNormal);
            sourceRow.CopyTo(newSurface.AsSpan((i << 1) + pitch, pitch));
        }

        return newSurface;
    }

    private static void MirrorTo(ReadOnlySpan<byte> source, Span<byte> destination, uint format, bool isNormal)
    {
        switch (format)
        {
            case Bc1:
                MirrorTo<Bc1Block>(source, destination, isNormal);
                break;
            case Bc2:
                MirrorTo<Bc2Block>(source, destination, isNormal);
                break;
            case Bc3:
                MirrorTo<Bc3Block>(source, destination, isNormal);
                break;
            case Bc4:
                MirrorTo<Bc4Block>(source, destination, isNormal);
                break;
            case Bc5:
                MirrorTo<Bc5Block>(source, destination, isNormal);
                break;
            case Bc7:
                MirrorTo<Bc7Block>(source, destination, isNormal);
                break;
            default:
                throw new NotImplementedException(
                    $"{nameof(MirrorTo)} not implemented for texture format 0x{format:X}");
        }
    }

    private static void MirrorTo<TBcBlock>(ReadOnlySpan<byte> source, Span<byte> destination, bool isNormal)
        where TBcBlock : unmanaged, IBcBlock
    {
        if (source.Length != destination.Length)
            throw new ArgumentException($"Length mismatch, source: {source.Length}, destination: {destination.Length}");

        var typedSource = MemoryMarshal.Cast<byte, TBcBlock>(source);
        var typedDestination = MemoryMarshal.Cast<byte, TBcBlock>(destination);
        for (var i = 0; i < typedDestination.Length; ++i)
        {
            var block = typedSource[typedSource.Length - 1 - i];
            try
            {
                block.Mirror();
            }
            catch
            {
                if (typeof(TBcBlock) != typeof(Bc7Block))
                    throw;
                MirrorBc7Slow(MemoryMarshal.AsBytes(new Span<TBcBlock>(ref block)));
                ++_slowBlocks;
            }

            if (isNormal)
                block.InvertRed();
            typedDestination[i] = block;
        }
    }

    private static void MirrorBc7Slow(Span<byte> block)
    {
        var decoder = new BcDecoder();
        var uncompressed = decoder.DecodeBlock(block, CompressionFormat.Bc7);
        var uncompressedSpan = uncompressed.Span;
        for (var row = 0; row < 4; ++row)
        {
            (uncompressedSpan[row, 0], uncompressedSpan[row, 3]) = (uncompressedSpan[row, 3], uncompressedSpan[row, 0]);
            (uncompressedSpan[row, 1], uncompressedSpan[row, 2]) = (uncompressedSpan[row, 2], uncompressedSpan[row, 1]);
        }

        var encoder = new BcEncoder
        {
            OutputOptions =
            {
                Format = CompressionFormat.Bc7,
                Quality = CompressionQuality.BestQuality,
            },
        };
        encoder.EncodeBlock(uncompressed.Span).AsSpan().CopyTo(block);
    }

    private static uint ToDdsPixelFormat(uint format)
        => format switch
        {
            Bc1 => DdsPixelFormat.Dxt1,
            Bc2 => DdsPixelFormat.Dxt3,
            Bc3 => DdsPixelFormat.Dxt5,
            Bc4 => DdsPixelFormat.Bc4U,
            Bc5 => DdsPixelFormat.Ati2,
            Bc7 => DdsPixelFormat.Dx10,
            _ => throw new NotImplementedException(
                $"{nameof(ToDdsPixelFormat)} not implemented for texture format 0x{format:X}"),
        };

    private static DxgiFormat ToDxgiFormat(uint format)
        => format switch
        {
            Bc1 => DxgiFormat.DxgiFormatBc1Unorm,
            Bc2 => DxgiFormat.DxgiFormatBc2Unorm,
            Bc3 => DxgiFormat.DxgiFormatBc3Unorm,
            Bc4 => DxgiFormat.DxgiFormatBc4Unorm,
            Bc5 => DxgiFormat.DxgiFormatBc5Unorm,
            Bc7 => DxgiFormat.DxgiFormatBc7Unorm,
            _ => throw new NotImplementedException(
                $"{nameof(ToDxgiFormat)} not implemented for texture format 0x{format:X}"),
        };

    [StructLayout(LayoutKind.Sequential)]
    private struct Header
    {
        public uint Type;
        public uint Format;
        public ushort Width;
        public ushort Height;
        public ushort Depth;
        public byte MipCount;
        public byte ArraySize;
        public LodOffsets LodOffsets;
        public SurfaceOffsets SurfaceOffsets;
    }

    [InlineArray(Length)]
    [StructLayout(LayoutKind.Sequential)]
    private struct LodOffsets
    {
        public const int Length = 3;

        private uint _element0;
    }

    [InlineArray(Length)]
    [StructLayout(LayoutKind.Sequential)]
    private struct SurfaceOffsets
    {
        public const int Length = 13;

        private uint _element0;
    }
}
