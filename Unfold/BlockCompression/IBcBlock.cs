namespace Unfold.BlockCompression;

public interface IBcBlock
{
    ulong this[BitRange bitRange] { get; set; }

    void Mirror();
    void InvertRed();

    string FieldsToString();
}
