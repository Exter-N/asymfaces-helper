namespace Unfold;

public sealed class DefaultMod
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; } = 0;
    public Dictionary<string, string> Files { get; set; } = [];
    public Dictionary<string, string> FileSwaps { get; set; } = [];
    public object[] Manipulations { get; set; } = [];
}
