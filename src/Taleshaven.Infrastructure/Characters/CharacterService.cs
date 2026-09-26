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
    IImageStore imageStore,
    TimeProvider timeProvider) : ICharacterService
{
    public async Task<CharacterList> GetCharactersAsync(int campaignId, string viewerId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var access = await CampaignAccess.GetAsync(db, campaignId, viewerId, cancellationToken);

        var rows = await (
                from c in db.Characters.AsNoTracking()
                where c.CampaignId == campaignId
                join u in db.Users on c.OwnerId equals u.Id
                orderby c.Name
                select new { c.Id, c.Name, c.OwnerId, OwnerName = u.DisplayName, c.IsNpc, c.RuleSystem, c.AvatarKey, c.IsArchived })
            .ToListAsync(cancellationToken);

        var summaries = rows
            .Select(r => new CharacterSummary(r.Id, r.Name, r.OwnerId, r.OwnerName, r.IsNpc, r.RuleSystem, AvatarUrl(r.AvatarKey), r.IsArchived))
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
                select new { Character = c, OwnerName = u.DisplayName })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
            return null;

        var character = row.Character;
        return new CharacterDetails(
            character.Id, character.Name, row.OwnerName, character.IsNpc, character.Sheet, character.SheetUrl, character.RuleSystem,
            AvatarUrl(character.AvatarKey), character.UpdatedAt,
            CampaignPermissions.CanEditCharacter(access.Role, viewerId, character.OwnerId),
            // Anteckningen lämnar aldrig servern för andra än GM.
            GmNote: access.Role == CampaignRole.GameMaster ? character.GmNote : null,
            IsArchived: character.IsArchived);
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
                c.AvatarKey,
                LastUsedAt = db.Posts.Where(p => p.CharacterId == c.Id).Max(p => (DateTimeOffset?)p.CreatedAt),
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new CharacterOption(r.Id, r.Name, r.IsNpc, AvatarUrl(r.AvatarKey), r.LastUsedAt)).ToList();
    }

    public async Task<int> CreateAsync(int campaignId, string userId, CharacterInput input, byte[]? avatar, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var access = await CampaignAccess.GetAsync(db, campaignId, userId, cancellationToken);
        if (!CampaignPermissions.CanCreateCharacter(access.Role))
            throw new CampaignRuleException("Endast kampanjens deltagare kan skapa karaktärer.");

        // GM:s karaktärer är alltid NPC:er, spelares aldrig.
        var now = timeProvider.GetUtcNow();
        var character = Character.Create(campaignId, userId, isNpc: access.Role == CampaignRole.GameMaster, input, now);

        var avatarKey = avatar is null ? null : await imageStore.SaveAvatarAsync(avatar, cancellationToken);
        character.SetAvatar(avatarKey, now);

        try
        {
            db.Characters.Add(character);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (avatarKey is not null)
                imageStore.DeleteAvatar(avatarKey);
            throw;
        }

        return character.Id;
    }

    public async Task UpdateAsync(int campaignId, int characterId, string userId, CharacterInput input, byte[]? avatar, bool removeAvatar, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var character = await LoadEditableAsync(db, campaignId, characterId, userId, cancellationToken);

        var now = timeProvider.GetUtcNow();
        character.Update(input, now);

        var oldAvatarKey = character.AvatarKey;
        string? newAvatarKey = null;
        if (avatar is not null)
        {
            newAvatarKey = await imageStore.SaveAvatarAsync(avatar, cancellationToken);
            character.SetAvatar(newAvatarKey, now);
        }
        else if (removeAvatar)
        {
            character.SetAvatar(null, now);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (newAvatarKey is not null)
                imageStore.DeleteAvatar(newAvatarKey);
            throw;
        }

        // Den gamla bilden tas bort först när den nya är sparad i databasen.
        if (oldAvatarKey is not null && oldAvatarKey != character.AvatarKey)
            imageStore.DeleteAvatar(oldAvatarKey);
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

        db.Characters.Remove(character);
        await db.SaveChangesAsync(cancellationToken);

        if (character.AvatarKey is not null)
            imageStore.DeleteAvatar(character.AvatarKey);
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

    private static string? AvatarUrl(string? key) => key is null ? null : IImageStore.AvatarUrl(key);
}
