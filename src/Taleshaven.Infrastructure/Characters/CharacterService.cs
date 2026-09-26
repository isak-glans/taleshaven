using Microsoft.EntityFrameworkCore;
using Taleshaven.Core;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Characters;
using Taleshaven.Core.Media;
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
            summaries.Where(c => !c.IsNpc).ToList(),
            summaries.Where(c => c.IsNpc && !c.IsArchived).ToList(),
            summaries.Where(c => c.IsNpc && c.IsArchived).ToList(),
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
            Alias: isGameMaster ? character.Alias : null);
    }

    public async Task<IReadOnlyList<CharacterOption>> GetPostingOptionsAsync(int campaignId, string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var access = await CampaignAccess.GetAsync(db, campaignId, userId, cancellationToken);

        var characters = access.Role switch
        {
            CampaignRole.GameMaster => db.Characters.Where(c => c.CampaignId == campaignId && c.IsNpc && !c.IsArchived),
            CampaignRole.Player => db.Characters.Where(c => c.CampaignId == campaignId && !c.IsNpc && c.OwnerId == userId),
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
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new CharacterOption(r.Id, r.Name, r.IsNpc, PortraitUrl(r.ImageKey), r.LastUsedAt, r.IsHidden)).ToList();
    }

    public async Task<int> CreateAsync(int campaignId, string userId, CharacterInput input, int? portraitId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var access = await CampaignAccess.GetAsync(db, campaignId, userId, cancellationToken);
        if (!CampaignPermissions.CanCreateCharacter(access.Role))
            throw new CampaignRuleException("Endast kampanjens deltagare kan skapa karaktärer.");
        await EnsurePortraitExistsAsync(db, portraitId, cancellationToken);

        // GM:s karaktärer är alltid NPC:er, spelares aldrig.
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

        character.SetArchived(archived);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int campaignId, int characterId, string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var character = await LoadEditableAsync(db, campaignId, characterId, userId, cancellationToken);

        if (await db.Posts.AnyAsync(p => p.CharacterId == characterId, cancellationToken))
            throw new CampaignRuleException("Karaktären har skrivit inlägg och kan inte tas bort.");

        // Porträttet ligger kvar i biblioteket.
        db.Characters.Remove(character);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Character> LoadEditableAsync(
        TaleshavenDbContext db, int campaignId, int characterId, string userId, CancellationToken cancellationToken)
    {
        var access = await CampaignAccess.GetAsync(db, campaignId, userId, cancellationToken);
        var character = await db.Characters.SingleOrDefaultAsync(c => c.CampaignId == campaignId && c.Id == characterId, cancellationToken)
            ?? throw new CampaignRuleException("Karaktären finns inte.");

        if (!CampaignPermissions.CanEditCharacter(access.Role, userId, character.OwnerId))
            throw new CampaignRuleException("Du kan inte ändra den här karaktären.");

        return character;
    }

    private static async Task EnsurePortraitExistsAsync(TaleshavenDbContext db, int? portraitId, CancellationToken cancellationToken)
    {
        if (portraitId is { } id && !await db.Portraits.AnyAsync(p => p.Id == id, cancellationToken))
            throw new CampaignRuleException("Porträttet finns inte längre i biblioteket. Välj ett annat.");
    }


    private static string? PortraitUrl(string? key) => key is null ? null : IImageStore.PortraitUrl(key);
}
