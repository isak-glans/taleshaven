using System.Text.RegularExpressions;
using Taleshaven.Core.Media;

namespace Taleshaven.Infrastructure.Media;

/// <summary>
/// Lagrar bilder som filer i en mapp utanför wwwroot. De serveras via en egen endpoint med rätt innehållstyp.
/// Nycklarna är slumpade och kontrolleras strikt, så att en nyckel aldrig kan peka ut en annan fil.
/// </summary>
internal sealed partial class LocalImageStore(string rootPath) : IImageStore
{
    private string PortraitDirectory => Path.Combine(rootPath, "portraits");

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
