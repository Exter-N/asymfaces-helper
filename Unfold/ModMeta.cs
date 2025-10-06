namespace Unfold;

public sealed class ModMeta
{
    public int FileVersion { get; set; } = 3;
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string[] ModTags { get; set; } = [];
}
