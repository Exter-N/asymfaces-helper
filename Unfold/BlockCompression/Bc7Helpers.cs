using static Unfold.ExtendedBitOperations;

namespace Unfold.BlockCompression;

internal static class Bc7Helpers
{
    // 00..15: Partition pattern (1bpt)
    // 16..19: Anchor texel for 2nd subset
    // 20..25: Mirrored pattern index
    // 26..27: Mirrored subset order / validity
    private static readonly uint[] Partitions2 =
    [
        0x40F_CCCCu, 0x42F_8888u, 0x41F_EEEEu, 0x57F_ECC8u, 0x19F_C880u, 0x58F_FEECu, 0x53F_FEC8u, 0x15F_EC80u,
        0x16F_C800u, 0x54F_FFECu, 0x10F_FE80u, 0x12F_E800u, 0x51F_FFE8u, 0x0DF_FF00u, 0x0EF_FFF0u, 0x0FF_F000u,
        0x0AF_F710u, 0x4C2_008Eu, 0x0B8_7100u, 0x462_08CEu, 0x492_008Cu, 0x078_7310u, 0x088_3100u, 0x43F_8CCEu,
        0x452_088Cu, 0x048_3110u, 0x1A2_6666u, 0x5F2_366Cu, 0x5E8_17E8u, 0x1D8_0FF0u, 0x5C2_718Eu, 0x5B2_399Cu,
        0x60F_AAAAu, 0x21F_F0F0u, 0x626_5A5Au, 0x638_33CCu, 0x642_3C3Cu, 0x658_55AAu, 0x26F_9696u, 0x67F_A55Au,
        0x692_73CEu, 0x688_13C8u, 0x6B2_324Cu, 0x6A2_3BDCu, 0x2C2_6996u, 0x6DF_C33Cu, 0x2EF_9966u, 0x2F6_0660u,
        0x316_0272u, 0x302_04E4u, 0x336_4E40u, 0x328_2720u, 0x36F_C936u, 0x77F_936Cu, 0x342_39C6u, 0x752_639Cu,
        0x39F_9336u, 0x38F_9CC6u, 0x7BF_817Eu, 0x7AF_E718u, 0xFFF_CCF0u, 0xFF2_0FCCu, 0x3F2_7744u, 0x3EF_EE22u,
    ];

    // 00..31: Partition pattern (2bpt)
    // 32..35: Anchor texel for 2nd subset
    // 36..39: Anchor texel for 3rd subset
    // 40..45: Mirrored pattern index
    // 46..48: Mirrored subset order / validity
    private static readonly ulong[] Partitions3 =
    [
        0x040F3_AA685050UL, 0x0C383_6A5A5040UL, 0x1028F_5A5A4200UL, 0x1413F_5450A0A8UL,
        0x104F8_A5A50000UL, 0x047F3_A0A05050UL, 0x0863F_5555A0A0UL, 0x0458F_5A5A5050UL,
        0x008F8_AA550000UL, 0x009F8_AA555500UL, 0x00AF6_AAAA5500UL, 0x08DF6_90909090UL,
        0x08CF6_94949494UL, 0x08BF5_A4A4A4A4UL, 0x04FF3_A9A59450UL, 0x04E83_2A0A4250UL,
        0x051F3_A5945040UL, 0x05083_0A425054UL, 0x112F8_A5A5A500UL, 0x0933F_55A0A0A0UL,
        0x055F3_A8A85454UL, 0x05483_6A6A4040UL, 0x017F6_A4A45000UL, 0x0168A_1A1A0500UL,
        0x09935_0050A4A4UL, 0x098F8_AAA59090UL, 0x01A68_14696914UL, 0x01BA6_69691400UL,
        0x15EF8_A08585A0UL, 0x01DF5_AA821414UL, 0x0DCAF_50A4A450UL, 0x0208F_6A5A0200UL,
        0x01FF8_A9A58000UL, 0x1FF3F_5090A0A8UL, 0x1FFF3_A8A09050UL, 0x123A5_24242424UL,
        0x024A6_00AA5500UL, 0x1268A_24924924UL, 0x12598_24499224UL, 0x068AF_50A50A50UL,
        0x0676F_500AA550UL, 0x069F3_AAAA4444UL, 0x12A8F_66660000UL, 0x0ACF5_A5A0A5A0UL,
        0x0AB3F_50A050A0UL, 0x02D6F_69286928UL, 0x06E6F_44AAAA44UL, 0x12F8F_66666600UL,
        0x070F3_AA444444UL, 0x0B23F_54A854A8UL, 0x0B1F5_95809580UL, 0x033F5_96969600UL,
        0x0B5F5_A85454A8UL, 0x0B4F8_80959580UL, 0x036F5_AA141414UL, 0x037FA_96960000UL,
        0x038F5_AAAA1414UL, 0x0BAFA_A05050A0UL, 0x0B9F8_A0A5A5A0UL, 0x03BFD_96000000UL,
        0x0BD3F_40804080UL, 0x0BCFC_A9A8A9A8UL, 0x07EF3_AAAAAA44UL, 0x1FF83_2A4A5254UL,
    ];

    private static readonly BitRange[] FieldPositions =
    [
        // Mode
        new(0, 1), new(0, 2), new(0, 3), new(0, 4), new(0, 5), new(0, 6), new(0, 7), new(0, 8),

        // Partition
        new(1, 5), new(2, 8), new(3, 9), new(4, 10), default, default, default, new(8, 14),

        // Rotation
        default, default, default, default, new(5, 7), new(6, 8), default, default,

        // Index Selection
        default, default, default, default, new(7, 8), default, default, default,

        // Red
        new(5, 9), new(8, 14), new(9, 14), new(10, 17), new(8, 13), new(8, 15), new(7, 14), new(14, 19),
        new(9, 13), new(14, 20), new(14, 19), new(17, 24), new(13, 18), new(15, 22), new(14, 21), new(19, 24),
        new(13, 17), new(20, 26), new(19, 24), new(24, 31), default, default, default, new(24, 29),
        new(17, 21), new(26, 32), new(24, 29), new(31, 38), default, default, default, new(29, 34),
        new(21, 25), default, new(29, 34), default, default, default, default, default,
        new(25, 29), default, new(34, 39), default, default, default, default, default,
        new(5, 29), new(8, 32), new(9, 39), new(10, 38), new(8, 18), new(8, 22), new(7, 21), new(14, 34),

        // Green
        new(29, 33), new(32, 38), new(39, 44), new(38, 45), new(18, 23), new(22, 29), new(21, 28), new(34, 39),
        new(33, 37), new(38, 44), new(44, 49), new(45, 52), new(23, 28), new(29, 36), new(28, 35), new(39, 44),
        new(37, 41), new(44, 50), new(49, 54), new(52, 59), default, default, default, new(44, 49),
        new(41, 45), new(50, 56), new(54, 59), new(59, 66), default, default, default, new(49, 54),
        new(45, 49), default, new(59, 64), default, default, default, default, default,
        new(49, 53), default, new(64, 69), default, default, default, default, default,
        new(29, 53), new(32, 56), new(39, 69), new(38, 66), new(18, 28), new(22, 36), new(21, 35), new(34, 54),

        // Blue
        new(53, 57), new(56, 62), new(69, 74), new(66, 73), new(28, 33), new(36, 43), new(35, 42), new(54, 59),
        new(57, 61), new(62, 68), new(74, 79), new(73, 80), new(33, 38), new(43, 50), new(42, 49), new(59, 64),
        new(61, 65), new(68, 74), new(79, 84), new(80, 87), default, default, default, new(64, 69),
        new(65, 69), new(74, 80), new(84, 89), new(87, 94), default, default, default, new(69, 74),
        new(69, 73), default, new(89, 94), default, default, default, default, default,
        new(73, 77), default, new(94, 99), default, default, default, default, default,
        new(53, 77), new(56, 80), new(69, 99), new(66, 94), new(28, 38), new(36, 50), new(35, 49), new(54, 74),

        // Alpha
        default, default, default, default, new(38, 44), new(50, 58), new(49, 56), new(74, 79),
        default, default, default, default, new(44, 50), new(58, 66), new(56, 63), new(79, 84),
        default, default, default, default, default, default, default, new(84, 89),
        default, default, default, default, default, default, default, new(89, 94),
        default, default, default, default, new(38, 50), new(50, 66), new(49, 63), new(74, 94),

        // Endpoint P
        new(77, 78), default, default, new(94, 95), default, default, new(63, 64), new(94, 95),
        new(78, 79), default, default, new(95, 96), default, default, new(64, 65), new(95, 96),
        new(79, 80), default, default, new(96, 97), default, default, default, new(96, 97),
        new(80, 81), default, default, new(97, 98), default, default, default, new(97, 98),
        new(81, 82), default, default, default, default, default, default, default,
        new(82, 83), default, default, default, default, default, default, default,
        new(77, 83), default, default, new(94, 98), default, default, new(63, 65), new(94, 98),

        // Shared P
        default, new(80, 81), default, default, default, default, default, default,
        default, new(81, 82), default, default, default, default, default, default,
        default, new(80, 82), default, default, default, default, default, default,

        // Indices
        new(83, 128), new(82, 128), new(99, 128), new(98, 128), new(50, 81), new(66, 97), new(65, 128), new(98, 128),
        default, default, default, default, new(81, 128), new(97, 128), default, default,
    ];

    public static int GetSubsetCount(int mode)
        => mode switch
        {
            0 or 2 => 3,
            1 or 3 or 7 => 2,
            _ => 1,
        };

    public static (int, int) GetIndexBitsPerTexel(int mode)
        => mode switch
        {
            0 or 1 => (3, 0),
            2 or 3 or 7 => (2, 0),
            4 => (2, 3),
            5 => (2, 2),
            6 => (4, 0),
            _ => (0, 0),
        };

    public static BitRange GetFieldPosition(int mode, Bc7Field field)
        => (mode & ~7) == 0 && field <= Bc7Field.Indices2
            ? FieldPositions[((int)field << 3) | mode]
            : default;

    public static uint GetPartitionPattern(int mode, int partition)
        => GetSubsetCount(mode) switch
        {
            2 => Widen1To2(unchecked((ushort)Partitions2[partition])) & 0x55555555u,
            3 => unchecked((uint)Partitions3[partition]),
            _ => 0u
        };

    public static ulong ExpandPatternMask(uint pattern)
        => Widen2To4((pattern << 1) | pattern);

    public static (byte, byte) GetPartitionAnchorTexels(int mode, byte partition)
    {
        switch (GetSubsetCount(mode))
        {
            case 2:
                var data2 = Partitions2[partition];
                return (unchecked((byte)((data2 >> 16) & 0xF)), 0);
            case 3:
                var data3 = Partitions3[partition];
                return (unchecked((byte)((data3 >> 32) & 0xF)), unchecked((byte)((data3 >> 36) & 0xF)));
            default:
                return (0, 0);
        }
    }

    public static (byte Partition, byte SubsetOrder) GetPartitionMirror(int mode, byte partition)
    {
        switch (GetSubsetCount(mode))
        {
            case 2:
                var data2 = Partitions2[partition];
                return ((data2 >> 27) & 0x1) == 0x1
                    ? throw new ArgumentException(
                        $"Partition pattern {partition} in two-subset mode has no mirror image")
                    : (unchecked((byte)((data2 >> 20) & 0x3F)), unchecked((byte)((data2 >> 26) & 0x1)));
            case 3:
                var data3 = Partitions3[partition];
                return ((data3 >> 47) & 0x3) == 0x3
                    ? throw new ArgumentException(
                        $"Partition pattern {partition} in three-subset mode has no mirror image")
                    : (unchecked((byte)((data3 >> 40) & 0x3F)), unchecked((byte)((data3 >> 46) & 0x7)));
            default:
                return (0, 0);
        }
    }
}
