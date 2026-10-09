using System.Text.RegularExpressions;
using Taleshaven.Core.Media;

namespace Taleshaven.Infrastructure.Media;

/// <summary>
/// Lagrar bilder som filer i en mapp utanför wwwroot. De serveras via en egen endpoint med rätt innehållstyp.
/// Nycklarna är slumpade och kontrolleras strikt, så att en nyckel aldrig kan peka ut en annan fil. Bilderna och
/// bibliotekets manifest ligger i <c>images/</c> (B65); manifestet kan inte nås via en nyckel.
/// </summary>
internal sealed partial class LocalImageStore(string rootPath) : IImageStore
{
    public const string ManifestFileName = "manifest.csv";

    private string PortraitDirectory => Path.Combine(rootPath, "images");

    /// <summary>Mappen hette <c>portraits/</c> före B65; den flyttas en gång om den nya inte finns.</summary>
    public static void MoveLegacyFolder(string rootPath)
    {
        var legacy = Path.Combine(rootPath, "portraits");
        var current = Path.Combine(rootPath, "images");
        if (Directory.Exists(legacy) && !Directory.Exists(current))
            Directory.Move(legacy, current);
    }

    public async Task<string?> ReadManifestAsync(CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(PortraitDirectory, ManifestFileName);
        return File.Exists(path) ? await File.ReadAllTextAsync(path, cancellationToken) : null;
    }

    public async Task WriteManifestAsync(string content, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(PortraitDirectory);
        var path = Path.Combine(PortraitDirectory, ManifestFileName);
        var temporary = path + ".tmp";
        await File.WriteAllTextAsync(temporary, content, cancellationToken);
        File.Move(temporary, path, overwrite: true);
    }

    public async Task<string> SavePortraitAsync(byte[] image, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(PortraitDirectory);
        var key = $"{Guid.NewGuid():N}.webp";
        await File.WriteAllBytesAsync(Path.Combine(PortraitDirectory, key), image, cancellationToken);
        return key;
    }

    public Stream? OpenPortrait(string key)
    {
        if (!IsValidKey(key))
            return null;

        var path = Path.Combine(PortraitDirectory, key);
        return File.Exists(path) ? File.OpenRead(path) : null;
    }

    public void DeletePortrait(string key)
    {
        if (!IsValidKey(key))
            return;

        var path = Path.Combine(PortraitDirectory, key);
        if (File.Exists(path))
            File.Delete(path);
    }

    internal static bool IsValidKey(string? key) => key is not null && KeyPattern().IsMatch(key);

    [GeneratedRegex("^[0-9a-f]{32}\\.webp$")]
    private static partial Regex KeyPattern();
}
