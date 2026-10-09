using Microsoft.EntityFrameworkCore;
using Taleshaven.Core;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Characters;
using Taleshaven.Core.Media;
using Taleshaven.Core.Portraits;
using Taleshaven.Infrastructure.Campaigns;
using Taleshaven.Infrastructure.Data;

namespace Taleshaven.Infrastructure.Characters;

internal sealed class CharacterService(
    IDbContextFactory<TaleshavenDbContext> dbFactory,
    TimeProvider timeProvider) : ICharacterService
{
    public async Task<CharacterList> GetCharactersAsync(int campaignId, string viewerId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var access = await CampaignAccess.GetAsync(db, campaignId, viewerId, cancellationToken);
        var isGameMaster = access.Role == CampaignRole.GameMaster;

        var rows = await (
                from c in db.Characters.AsNoTracking()
                where c.CampaignId == campaignId
                where !c.IsHidden || isGameMaster
                join u in db.Users on c.OwnerId equals u.Id
                orderby c.Name
                select new { c.Id, c.Name, c.OwnerId, OwnerName = u.DisplayName, c.IsNpc, c.RuleSystem, ImageKey = db.Portraits.Where(p => p.Id == c.PortraitId).Select(p => p.ImageKey).FirstOrDefault(), c.IsArchived, c.IsHidden })
            .ToListAsync(cancellationToken);

        var summaries = rows
            .Select(r => new CharacterSummary(r.Id, r.Name, r.OwnerId, r.OwnerName, r.IsNpc, r.RuleSystem, PortraitUrl(r.ImageKey), r.IsArchived, r.IsHidden))
            .ToList();

        return new CharacterList(
            summaries.Where(c => !c.IsNpc && !c.IsArchived).ToList(),
            summaries.Where(c => c.IsNpc && !c.IsArchived).ToList(),
            summaries.Where(c => c.IsArchived).ToList(),
            CampaignPermissions.CanCreateCharacter(access.Role));
    }

    public async Task<CharacterDetails?> GetCharacterAsync(int campaignId, int characterId, string viewerId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var access = await CampaignAccess.GetAsync(db, campaignId, viewerId, cancellationToken);

        var row = await (
                from c in db.Characters.AsNoTracking()
                where c.CampaignId == campaignId && c.Id == characterId
                join u in db.Users on c.OwnerId equals u.Id
                select new { Character = c, OwnerName = u.DisplayName, ImageKey = db.Portraits.Where(p => p.Id == c.PortraitId).Select(p => p.ImageKey).FirstOrDefault() })
            .SingleOrDefaultAsync(cancellationToken);

        var isGameMaster = access.Role == CampaignRole.GameMaster;

        // En dold NPC finns inte för andra än GM (B16).
        if (row is null || (row.Character.IsHidden && !isGameMaster))
            return null;

        var character = row.Character;
        return new CharacterDetails(
            character.Id, character.Name, row.OwnerName, character.IsNpc, character.Sheet, character.SheetUrl, character.RuleSystem,
            PortraitUrl(row.ImageKey), character.PortraitId, character.UpdatedAt,
            CampaignPermissions.CanEditCharacter(access.Role, viewerId, character.OwnerId),
            // Anteckningen lämnar aldrig servern för andra än GM.
            GmNote: isGameMaster ? character.GmNote : null,
            IsArchived: character.IsArchived,
            IsHidden: character.IsHidden,
            Alias: isGameMaster ? character.Alias : null,
            Status: StatusFor(character, isGameMaster, await IconUrlsAsync(db, IconIds(character), cancellationToken)),
            CanDuplicate: CanDuplicate(access.Role, viewerId, character));
    }

    public async Task<CharacterStatus> EditStatusAsync(int campaignId, int characterId, string userId, CharacterStatusChange change,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(change);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var access = await CampaignAccess.GetAsync(db, campaignId, userId, cancellationToken);

        var character = await db.Characters.SingleOrDefaultAsync(c => c.Id == characterId && c.CampaignId == campaignId, cancellationToken)
            ?? throw new CampaignRuleException("The character doesn't exist.");
        if (!CampaignPermissions.CanEditCharacter(access.Role, userId, character.OwnerId))
            throw new CampaignRuleException("You can't change this character.");

        // En ikon som väljs för hand måste finnas i biblioteket och vara en ikon (B58, B61).
        if (change is CharacterStatusChange.SetIcon { IconId: { } iconId }
            && !await db.Portraits.AnyAsync(p => p.Id == iconId && p.Kind == ImageKind.Icon, cancellationToken))
            throw new CampaignRuleException("The icon doesn't exist any more. Choose another one.");

        var icons = await db.Portraits.AsNoTracking()
            .Where(p => p.Kind == ImageKind.Icon)
            .Select(p => new { p.Id, p.Tags })
            .ToListAsync(cancellationToken);
        var candidates = icons.Select(i => (i.Id, (IReadOnlyList<string>)i.Tags)).ToList();

        change.ApplyTo(character, timeProvider.GetUtcNow(), name => IconMatcher.Suggest(name, candidates));
        await db.SaveChangesAsync(cancellationToken);
        return StatusFor(character, access.Role == CampaignRole.GameMaster, await IconUrlsAsync(db, IconIds(character), cancellationToken));
    }

    public async Task<IReadOnlyList<string>> GetConditionSuggestionsAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var used = await db.Characters.AsNoTracking()
            .Where(c => c.CampaignId == campaignId)
            .Select(c => c.Conditions)
            .ToListAsync(cancellationToken);

        return CharacterTrackers.StandardConditions
            .Concat(used.SelectMany(list => list).Select(c => c.Name))
            .DistinctBy(name => name.ToLowerInvariant())
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // Räknare och sparade slag på en NPC är GM:s anteckningar; tillstånden ser alla (B56).
    private static CharacterStatus StatusFor(Character character, bool viewerIsGameMaster, IReadOnlyDictionary<int, string> iconUrls)
    {
        string? Url(int? id) => id is { } value && iconUrls.TryGetValue(value, out var url) ? url : null;
        var showsPrivate = !character.IsNpc || viewerIsGameMaster;
        return new CharacterStatus(
            showsPrivate ? (character.Counters ?? []).Select(c => new CounterView(c.Uid, c.Label, c.Current, c.Max, c.IconId, Url(c.IconId))).ToList() : [],
            (character.Conditions ?? []).Select(c => new ConditionView(c.Uid, c.Name, c.IconId, Url(c.IconId))).ToList(),
            showsPrivate ? (character.SavedRolls ?? []).Select(r => SavedRollView.From(r, Url(r.IconId))).ToList() : [],
            showsPrivate);
    }

    private static IEnumerable<int?> IconIds(Character character) =>
        (character.Counters ?? []).Select(c => c.IconId)
            .Concat((character.Conditions ?? []).Select(c => c.IconId))
            .Concat((character.SavedRolls ?? []).Select(r => r.IconId));

    // Adresserna till ikonerna (B58). En ikon som tagits bort ur biblioteket saknas här och visas inte.
    private static async Task<IReadOnlyDictionary<int, string>> IconUrlsAsync(
        TaleshavenDbContext db, IEnumerable<int?> iconIds, CancellationToken cancellationToken)
    {
        var ids = iconIds.Where(id => id is not null).Select(id => id!.Value).Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, string>();
        return await db.Portraits.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => IImageStore.PortraitUrl(p.ImageKey), cancellationToken);
    }

    public async Task<IReadOnlyList<CharacterOption>> GetPostingOptionsAsync(int campaignId, string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var access = await CampaignAccess.GetAsync(db, campaignId, userId, cancellationToken);

        var characters = access.Role switch
        {
            CampaignRole.GameMaster => db.Characters.Where(c => c.CampaignId == campaignId && c.IsNpc && !c.IsArchived),
            CampaignRole.Player => db.Characters.Where(c => c.CampaignId == campaignId && !c.IsNpc && c.OwnerId == userId && !c.IsArchived),
            _ => null,
        };

        if (characters is null)
            return [];

        var rows = await characters.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.IsNpc,
                ImageKey = db.Portraits.Where(p => p.Id == c.PortraitId).Select(p => p.ImageKey).FirstOrDefault(),
                c.IsHidden,
                LastUsedAt = db.Posts.Where(p => p.CharacterId == c.Id).Max(p => (DateTimeOffset?)p.CreatedAt),
                c.SavedRolls,
            })
            .ToListAsync(cancellationToken);

        // Bara egna karaktärer och GM:s NPC:er väljs här, så de sparade slagen får följa med (B57), med ikoner (B58).
        var iconUrls = await IconUrlsAsync(db, rows.SelectMany(r => (r.SavedRolls ?? []).Select(s => s.IconId)), cancellationToken);
        return rows.Select(r => new CharacterOption(r.Id, r.Name, r.IsNpc, PortraitUrl(r.ImageKey), r.LastUsedAt, r.IsHidden,
            (r.SavedRolls ?? []).Select(s => SavedRollView.From(s, s.IconId is { } id ? iconUrls.GetValueOrDefault(id) : null)).ToList())).ToList();
    }

    public async Task<int> CreateAsync(int campaignId, string userId, CharacterInput input, int? portraitId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var access = await CampaignAccess.GetAsync(db, campaignId, userId, cancellationToken);
        if (!CampaignPermissions.CanCreateCharacter(access.Role))
            throw new CampaignRuleException("Only the campaign's participants can create characters.");
        await EnsurePortraitExistsAsync(db, portraitId, cancellationToken);

        // GM:s karaktärer är alltid NPC:er, spelares aldrig.
        if (access.Role != CampaignRole.GameMaster)
            await EnsureRoomForPlayerCharacterAsync(db, campaignId, userId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var character = Character.Create(campaignId, userId, isNpc: access.Role == CampaignRole.GameMaster, input, now);
        character.SetPortrait(portraitId, now);

        db.Characters.Add(character);
        await db.SaveChangesAsync(cancellationToken);
        return character.Id;
    }

    public async Task UpdateAsync(int campaignId, int characterId, string userId, CharacterInput input, int? portraitId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var character = await LoadEditableAsync(db, campaignId, characterId, userId, cancellationToken);
        await EnsurePortraitExistsAsync(db, portraitId, cancellationToken);

        var now = timeProvider.GetUtcNow();
        character.Update(input, now);
        character.SetPortrait(portraitId, now);
        await db.SaveChangesAsync(cancellationToken);
    }
    public async Task SetArchivedAsync(int campaignId, int characterId, string userId, bool archived, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var character = await LoadEditableAsync(db, campaignId, characterId, userId, cancellationToken);

        if (!archived && character.IsArchived && !character.IsNpc)
            await EnsureRoomForPlayerCharacterAsync(db, campaignId, character.OwnerId, cancellationToken);
        character.SetArchived(archived);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> DuplicateAsync(int campaignId, int characterId, string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var access = await CampaignAccess.GetAsync(db, campaignId, userId, cancellationToken);
        var character = await db.Characters.AsNoTracking().SingleOrDefaultAsync(c => c.CampaignId == campaignId && c.Id == characterId, cancellationToken)
            ?? throw new CampaignRuleException("The character doesn't exist.");
        if (!CanDuplicate(access.Role, userId, character))
            throw new CampaignRuleException(character.IsNpc ? "Only the GM can duplicate NPCs." : "You can only duplicate your own characters.");
        if (!character.IsNpc)
            await EnsureRoomForPlayerCharacterAsync(db, campaignId, userId, cancellationToken);

        var names = await db.Characters.Where(c => c.CampaignId == campaignId).Select(c => c.Name).ToListAsync(cancellationToken);
        var copy = character.Duplicate(Character.NextNumberedName(character.Name, names), timeProvider.GetUtcNow());
        db.Characters.Add(copy);
        await db.SaveChangesAsync(cancellationToken);
        return copy.Id;
    }

    // GM duplicerar NPC:er; spelare sina egna karaktärer (B68).
    private static bool CanDuplicate(CampaignRole role, string userId, Character character) => character.IsNpc
        ? role == CampaignRole.GameMaster
        : role == CampaignRole.Player && character.OwnerId == userId;

    // En spelare har högst ett visst antal aktiva (ej arkiverade) karaktärer per kampanj (B68).
    private static async Task EnsureRoomForPlayerCharacterAsync(TaleshavenDbContext db, int campaignId, string ownerId, CancellationToken cancellationToken)
    {
        var active = await db.Characters.CountAsync(
            c => c.CampaignId == campaignId && c.OwnerId == ownerId && !c.IsNpc && !c.IsArchived, cancellationToken);
        if (active >= CharacterLimits.MaxActivePlayerCharacters)
            throw new CampaignRuleException(
                $"A player can have at most {CharacterLimits.MaxActivePlayerCharacters} active characters in a campaign. Archive one first.");
    }

    public async Task DeleteAsync(int campaignId, int characterId, string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var character = await LoadEditableAsync(db, campaignId, characterId, userId, cancellationToken);

        if (await db.Posts.AnyAsync(p => p.CharacterId == characterId, cancellationToken))
            throw new CampaignRuleException("The character has written posts and can't be deleted.");

        // Porträttet ligger kvar i biblioteket.
        db.Characters.Remove(character);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Character> LoadEditableAsync(
        TaleshavenDbContext db, int campaignId, int characterId, string userId, CancellationToken cancellationToken)
    {
        var access = await CampaignAccess.GetAsync(db, campaignId, userId, cancellationToken);
        var character = await db.Characters.SingleOrDefaultAsync(c => c.CampaignId == campaignId && c.Id == characterId, cancellationToken)
            ?? throw new CampaignRuleException("The character doesn't exist.");

        if (!CampaignPermissions.CanEditCharacter(access.Role, userId, character.OwnerId))
            throw new CampaignRuleException("You can't change this character.");

        return character;
    }

    private static async Task EnsurePortraitExistsAsync(TaleshavenDbContext db, int? portraitId, CancellationToken cancellationToken)
    {
        if (portraitId is { } id && !await db.Portraits.AnyAsync(p => p.Id == id && p.Kind == ImageKind.Portrait, cancellationToken))
            throw new CampaignRuleException("The portrait is no longer in the library. Choose another one.");
    }


    private static string? PortraitUrl(string? key) => key is null ? null : IImageStore.PortraitUrl(key);
}
