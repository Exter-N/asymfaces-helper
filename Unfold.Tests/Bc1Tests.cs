using Unfold.BlockCompression;

namespace Unfold.Tests;

public sealed class Bc1Tests
{
    [TestCase(0x0000000000000000UL, 0x0000000000000000UL)]
    [TestCase(0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL)]
    [TestCase(0x0123456789ABCDEFUL, 0x40C851D989ABCDEFUL)]
    [TestCase(0xFEDCBA9876543210UL, 0xBF37AE2676543210UL)]
    public void TestMirror(ulong original, ulong expected)
    {
        var block = new Bc1Block(original);
        Console.WriteLine($"Original: {block} ({block.Data:X16})");
        block.Mirror();
        Console.WriteLine($"Mirrored: {block} ({block.Data:X16})");
        Assert.That(block, Is.EqualTo(new Bc1Block(expected)));
    }

    [TestCase(0x0000000000000000UL, 0x00000000F800F800UL)]
    [TestCase(0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFF07FF07FFUL)]
    [TestCase(0x0123456789ABCDEFUL, 0x5476103235EF71ABUL)]
    [TestCase(0xFEDCBA9876543210UL, 0xFECDBA89CA108E54UL)]
    public void TestInvertRed(ulong original, ulong expected)
    {
        var block = new Bc1Block(original);
        Console.WriteLine($"Original: {block} ({block.Data:X16})");
        block.InvertRed();
        Console.WriteLine($"Inverted: {block} ({block.Data:X16})");
        Assert.That(block, Is.EqualTo(new Bc1Block(expected)));
    }
}
