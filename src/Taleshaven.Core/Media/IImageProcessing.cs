namespace Taleshaven.Core.Media;

public static class ImageLimits
{
    /// <summary>Största filen som tas emot vid uppladdning.</summary>
    public const long MaxUploadBytes = 5 * 1024 * 1024;

    /// <summary>Största bredd/höjd på originalbilden, som skydd mot bilder som blir enorma när de packas upp.</summary>
    public const int MaxSourceDimension = 8000;

    /// <summary>Porträtten lagras kvadratiska i den här storleken.</summary>
    public const int PortraitSize = 256;
}

/// <summary>
/// Gör om en uppladdad bild till ett säkert, litet porträtt: beskuren kvadrat, omskalad, EXIF borttaget och omkodad.
/// Kastar <see cref="CampaignRuleException"/> om filen inte är en bild som kan läsas.
/// </summary>
public interface IImageProcessor
{
    byte[] CreatePortrait(Stream source);
}

/// <summary>Lagrar färdigbehandlade bilder. Nycklarna genereras här och kan inte styras av användaren.</summary>
public interface IImageStore
{
    Task<string> SavePortraitAsync(byte[] image, CancellationToken cancellationToken = default);

    /// <summary>Öppnar bilden, eller null om nyckeln är ogiltig eller bilden saknas.</summary>
    Stream? OpenPortrait(string key);

    void DeletePortrait(string key);

    const string PortraitContentType = "image/webp";

    static string PortraitUrl(string key) => $"media/portraits/{key}";
}
