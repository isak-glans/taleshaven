namespace Taleshaven.Core.Portraits;

/// <summary>
/// Sajtens porträttbibliotek (B19, B20). Alla inloggade kan söka; bara administratörer och managers laddar upp,
/// ändrar och tar bort. Regelbrott och saknad behörighet ger <see cref="CampaignRuleException"/>.
/// </summary>
public interface IPortraitService
{
    /// <summary>
    /// Porträtt där varje sökord matchar början av någon tagg (PB-4), nyast först. Tom sökning ger alla.
    /// <paramref name="skip"/> hoppar över de första träffarna, för sidindelning (B51).
    /// </summary>
    Task<PortraitPage> SearchAsync(string? query, int limit, int skip = 0, CancellationToken cancellationToken = default);

    /// <summary>Alla taggar som används, vanligast först, att föreslå vid uppladdning och sökning.</summary>
    Task<IReadOnlyList<TagCount>> GetTagsAsync(CancellationToken cancellationToken = default);

    /// <summary>Lägger till ett redan behandlat porträtt (<paramref name="image"/>) i biblioteket. Returnerar id.</summary>
    Task<int> UploadAsync(string userId, byte[] image, string? tags, string? source, CancellationToken cancellationToken = default);

    Task UpdateAsync(string userId, int portraitId, string? tags, string? source, CancellationToken cancellationToken = default);

    /// <summary>Tar bort porträttet. Karaktärer som använde det får initialer i stället (B20).</summary>
    Task DeleteAsync(string userId, int portraitId, CancellationToken cancellationToken = default);
}

/// <summary>En sida med porträtt. <see cref="TotalCount"/> är antalet träffar för hela sökningen.</summary>
public sealed record PortraitPage(IReadOnlyList<PortraitView> Portraits, bool HasMore, int TotalCount);

/// <summary>Ett porträtt att visa. <see cref="UsageCount"/> är hur många karaktärer som använder det.</summary>
public sealed record PortraitView(int Id, string Url, IReadOnlyList<string> Tags, string? Source, int UsageCount);

public sealed record TagCount(string Tag, int Count);
