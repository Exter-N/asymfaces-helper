using System.IO.Compression;
using System.Text.Json;
using Unfold;
using static Unfold.Utility;

SwitchToDirectory();

var masksPerRace = new Dictionary<ushort, int>();
var redirectionsPerRace = new Dictionary<ushort, Dictionary<string, string>>();

Console.WriteLine("Preparing...");

foreach (var raceCode in AllRaceCodes())
{
    masksPerRace.Add(raceCode, 0);
    redirectionsPerRace.Add(raceCode, new());

    DeleteDirectory($"asymfaces_vanilla_{GetGender(raceCode)}_{GetRace(raceCode)}");
    DeleteDirectory($"asymfaces_vanilla_textures_{GetGender(raceCode)}_{GetRace(raceCode)}");

    Directory.CreateDirectory($"asymfaces_vanilla_{GetGender(raceCode)}_{GetRace(raceCode)}");
    Directory.CreateDirectory($"asymfaces_vanilla_textures_{GetGender(raceCode)}_{GetRace(raceCode)}");
}

var allPaths = Directory.GetFiles("chara");

foreach (var path in allPaths)
{
    if (Path.GetExtension(path).ToLowerInvariant() is not ".tex")
        continue;
    var (textureRaceCode, _, textureSuffix) = TextureHandler.ParseTextureName(Path.GetFileNameWithoutExtension(path));
    if (!string.Equals(textureSuffix, "mask", StringComparison.OrdinalIgnoreCase))
        continue;
    ++masksPerRace[textureRaceCode];
}

Console.WriteLine("Processing resources...");

foreach (var path in allPaths)
{
    switch (Path.GetExtension(path).ToLowerInvariant())
    {
        case ".mdl":
            // This case intentionally left blank.
            break;
        case ".mtrl":
            MaterialHandler.Process(path, redirectionsPerRace, masksPerRace);
            break;
        case ".tex":
            TextureHandler.Process(path, masksPerRace);
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

foreach (var raceCode in AllRaceCodes())
{
    Console.WriteLine($"Packing for {GetDisplayRace(raceCode)} {GetDisplayGender(raceCode)}...");
    File.WriteAllText($"asymfaces_vanilla_{GetGender(raceCode)}_{GetRace(raceCode)}/meta.json",
        JsonSerializer.Serialize(new ModMeta
        {
            Name = $"AsymFaces - Vanilla-like Textures - {GetDisplayRace(raceCode)} {GetDisplayGender(raceCode)}",
            Author = "Spiswel, Nylfae",
            Description =
                $"A vanilla-like texture and material pack for AsymFaces, for {GetDisplayRace(raceCode)} {GetDisplayGender(raceCode).ToLowerInvariant()}s.",
            Version = "2.0",
            Website = "https://heliosphere.app/user/spiswel",
            ModTags =
            [
                "asymfaces",
                GetGender(raceCode),
                GetRace(raceCode),
            ],
        }, jsonSerializerOptions));

    File.WriteAllText($"asymfaces_vanilla_{GetGender(raceCode)}_{GetRace(raceCode)}/default_mod.json",
        JsonSerializer.Serialize(new DefaultMod
        {
            Files = redirectionsPerRace[raceCode],
        }, jsonSerializerOptions));

    File.Delete($"asymfaces_vanilla_{GetGender(raceCode)}_{GetRace(raceCode)}.pmp");
    File.Delete($"asymfaces_vanilla_textures_{GetGender(raceCode)}_{GetRace(raceCode)}.zip");
    ZipFile.CreateFromDirectory($"asymfaces_vanilla_{GetGender(raceCode)}_{GetRace(raceCode)}",
        $"asymfaces_vanilla_{GetGender(raceCode)}_{GetRace(raceCode)}.pmp", CompressionLevel.SmallestSize, false);
    ZipFile.CreateFromDirectory($"asymfaces_vanilla_textures_{GetGender(raceCode)}_{GetRace(raceCode)}",
        $"asymfaces_vanilla_textures_{GetGender(raceCode)}_{GetRace(raceCode)}.zip",
        CompressionLevel.SmallestSize, false);
#if !DEBUG
    DeleteDirectory($"asymfaces_vanilla_{GetGender(raceCode)}_{GetRace(raceCode)}");
    DeleteDirectory($"asymfaces_vanilla_textures_{GetGender(raceCode)}_{GetRace(raceCode)}");
#endif
}

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

void DeleteDirectory(string path)
{
    try
    {
        Directory.Delete(path, true);
    }
    catch (DirectoryNotFoundException)
    {
        // This block intentionally left blank.
    }
}
