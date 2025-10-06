using Unfold.BlockCompression;

namespace Unfold.Tests;

public sealed class Bc4Tests
{
    [TestCase(0x0000000000000000UL, 0x0000000000000000UL)]
    [TestCase(0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL)]
    [TestCase(0x0123456789ABCDEFUL, 0x480A291CB774CDEFUL)]
    [TestCase(0xFEDCBA9876543210UL, 0xB7F5D6E3488B3210UL)]
    public void TestMirror(ulong original, ulong expected)
    {
        var block = new Bc4Block(original);
        Console.WriteLine($"Original: {block} ({block.Data:X16})");
        block.Mirror();
        Console.WriteLine($"Mirrored: {block} ({block.Data:X16})");
        Assert.That(block, Is.EqualTo(new Bc4Block(expected)));
    }

    [TestCase(0x0000000000000000UL, 0x249249249249FFFFUL)]
    [TestCase(0xFFFFFFFFFFFFFFFFUL, 0xDB6DB6DB6DB60000UL)]
    [TestCase(0x0123456789ABCDEFUL, 0x27F10CC11AE61032UL)]
    [TestCase(0xFEDCBA9876543210UL, 0xD92F757CE82BEFCDUL)]
    public void TestInvertRed(ulong original, ulong expected)
    {
        var block = new Bc4Block(original);
        Console.WriteLine($"Original: {block} ({block.Data:X16})");
        block.InvertRed();
        Console.WriteLine($"Inverted: {block} ({block.Data:X16})");
        Assert.That(block, Is.EqualTo(new Bc4Block(expected)));
    }
}
