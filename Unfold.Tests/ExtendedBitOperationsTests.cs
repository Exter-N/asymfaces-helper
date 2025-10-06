using static Unfold.ExtendedBitOperations;

namespace Unfold.Tests;

public sealed class ExtendedBitOperationsTests
{
    [Test]
    public void TestWiden1To2()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Widen1To2((ushort)0x0000u), Is.EqualTo(0x00000000u));
            Assert.That(Widen1To2((ushort)0xFFFFu), Is.EqualTo(0xFFFFFFFFu));
            Assert.That(Widen1To2((ushort)0xCDEFu), Is.EqualTo(0xF0F3FCFFu));
            Assert.That(Widen1To2((ushort)0x3210u), Is.EqualTo(0x0F0C0300u));
            Assert.That(Widen1To2((ushort)0x7357u), Is.EqualTo(0x3F0F333Fu));
        });
    }

    [Test]
    public void TestWiden2To4()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Widen2To4(0x00000000u), Is.EqualTo(0x0000000000000000UL));
            Assert.That(Widen2To4(0xFFFFFFFFu), Is.EqualTo(0xFFFFFFFFFFFFFFFFUL));
            Assert.That(Widen2To4(0x89ABCDEFu), Is.EqualTo(0xA0A5AAAFF0F5FAFFUL));
            Assert.That(Widen2To4(0x76543210u), Is.EqualTo(0x5F5A55500F0A0500UL));
            Assert.That(Widen2To4(0x600D7357u), Is.EqualTo(0x5A0000F55F0F555FUL));
        });
    }

    [Test]
    public void TestWiden3To4()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Widen3To4(0xFFFF000000000000UL), Is.EqualTo(0x0000000000000000UL));
            Assert.That(Widen3To4(0x0000000000000000UL), Is.EqualTo(0x0000000000000000UL));
            Assert.That(Widen3To4(0x0000FFFFFFFFFFFFUL), Is.EqualTo(0xFFFFFFFFFFFFFFFFUL));
            Assert.That(Widen3To4(0x0123456789ABCDEFUL), Is.EqualTo(0x424D6D22B4F9DFBFUL));
            Assert.That(Widen3To4(0xFEDCBA9876543210UL), Is.EqualTo(0xBDB292DD4B062040UL));
            Assert.That(Widen3To4(0x0000053977053977UL), Is.EqualTo(0x02469BDF02469BDFUL));
            Assert.That(Widen3To4(0x0000FAC688FAC688UL), Is.EqualTo(0xFDB96420FDB96420UL));
            Assert.That(Widen3To4(0x00000000600D7357UL), Is.EqualTo(0x00000290064F2B4FUL));
        });
    }

    [Test]
    public void TestNarrow4To2()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Narrow4To2(0x0000000000000000UL), Is.EqualTo(0x00000000u));
            Assert.That(Narrow4To2(0xFFFFFFFFFFFFFFFFUL), Is.EqualTo(0xFFFFFFFFu));
            Assert.That(Narrow4To2(0x0123456789ABCDEFUL), Is.EqualTo(0x0055AAFFu));
            Assert.That(Narrow4To2(0xFEDCBA9876543210UL), Is.EqualTo(0xFFAA5500u));
        });
    }

    [Test]
    public void TestNarrow4To3()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Narrow4To3(0x0000000000000000UL), Is.EqualTo(0x000000000000UL));
            Assert.That(Narrow4To3(0xFFFFFFFFFFFFFFFFUL), Is.EqualTo(0xFFFFFFFFFFFFUL));
            Assert.That(Narrow4To3(0x0123456789ABCDEFUL), Is.EqualTo(0x00949B92DDBFUL));
            Assert.That(Narrow4To3(0xFEDCBA9876543210UL), Is.EqualTo(0xFF6B646D2240UL));
        });
    }
}
