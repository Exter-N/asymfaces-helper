using System.IO.Compression;
using System.Text.Json;
using Unfold;

SwitchToDirectory();

try
{
    Directory.Delete("asymfaces_vanilla", true);
}
catch (DirectoryNotFoundException)
{
    // This block intentionally left blank.
}

try
{
    Directory.Delete("asymfaces_vanilla_textures", true);
}
catch (DirectoryNotFoundException)
{
    // This block intentionally left blank.
}

Directory.CreateDirectory("asymfaces_vanilla");
Directory.CreateDirectory("asymfaces_vanilla_textures");

var redirections = new Dictionary<string, string>();

foreach (var path in Directory.GetFiles("chara"))
{
    switch (Path.GetExtension(path).ToLowerInvariant())
    {
        case ".mdl":
            // This case intentionally left blank.
            break;
        case ".mtrl":
            MaterialHandler.Process(path, redirections);
            break;
        case ".tex":
            TextureHandler.Process(path);
            break;
        case var ext:
            Console.WriteLine($"Unhandled extension {ext}");
            break;
    }
}

var jsonSerializerOptions = new JsonSerializerOptions
{
    WriteIndented = true,
};

File.WriteAllText("asymfaces_vanilla/meta.json", JsonSerializer.Serialize(new ModMeta
{
    Name = "AsymFaces - Vanilla-like Textures",
    Author = "Spiswel, Nylfae",
    Description = "A vanilla-like texture and material pack for AsymFaces.",
    Version = "2.0",
    Website = "https://dindon.org/",
    ModTags = [
        "asymfaces",
    ],
}, jsonSerializerOptions));

File.WriteAllText("asymfaces_vanilla/default_mod.json", JsonSerializer.Serialize(new DefaultMod
{
    Files = redirections,
}, jsonSerializerOptions));

File.Delete("asymfaces_vanilla.pmp");
File.Delete("asymfaces_vanilla_textures.zip");
ZipFile.CreateFromDirectory("asymfaces_vanilla", "asymfaces_vanilla.pmp", CompressionLevel.SmallestSize, false);
ZipFile.CreateFromDirectory("asymfaces_vanilla_textures", "asymfaces_vanilla_textures.zip", CompressionLevel.SmallestSize, false);
#if !DEBUG
Directory.Delete("asymfaces_vanilla", true);
Directory.Delete("asymfaces_vanilla_textures", true);
#endif

return;

void SwitchToDirectory()
{
    var path = Path.GetDirectoryName(typeof(Program).Assembly.Location);
    while (path is not null && !Directory.Exists(Path.Combine(path, "chara")))
    {
        var parent = Path.GetDirectoryName(path);
        if (parent == path)
            throw new Exception("Cannot find chara directory");
        path = parent;
    }

    if (path is null)
        throw new Exception("Cannot find chara directory");

    Environment.CurrentDirectory = path;
}
