using Taleshaven.Core;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Characters;
using Taleshaven.Core.Threads;

namespace Taleshaven.Tests.Characters;

public class CharacterTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    private static CharacterInput Input(string? name = "Aldric", string? sheet = "HP 12/12", string? url = null, string? rules = null) =>
        new(name, sheet, url, rules);

    [Fact]
    public void Create_TrimsAndStoresFields()
    {
        var character = Character.Create(7, "anna", isNpc: false,
            Input("  Aldric Stormbringer ", "  **HP** 12/12  ", " https://www.dndbeyond.com/characters/123 ", " D&D 5e "), Now);

        Assert.Equal(7, character.CampaignId);
        Assert.Equal("anna", character.OwnerId);
        Assert.False(character.IsNpc);
        Assert.Equal("Aldric Stormbringer", character.Name);
        Assert.Equal("**HP** 12/12", character.Sheet);
        Assert.Equal("https://www.dndbeyond.com/characters/123", character.SheetUrl);
        Assert.Equal("D&D 5e", character.RuleSystem);
        Assert.Null(character.AvatarKey);
    }

    [Fact]
    public void Create_AllowsEmptySheetAndOptionalFields()
    {
        var character = Character.Create(7, "anna", false, Input(sheet: null, url: " ", rules: ""), Now);

        Assert.Equal("", character.Sheet);
        Assert.Null(character.SheetUrl);
        Assert.Null(character.RuleSystem);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_RequiresName(string? name)
    {
        Assert.Throws<CampaignRuleException>(() => Character.Create(7, "anna", false, Input(name), Now));
    }

    [Fact]
    public void Create_EnforcesLengths()
    {
        Assert.Throws<CampaignRuleException>(() =>
            Character.Create(7, "anna", false, Input(new string('a', CharacterLimits.NameMaxLength + 1)), Now));
        Assert.Throws<CampaignRuleException>(() =>
            Character.Create(7, "anna", false, Input(sheet: new string('a', CharacterLimits.SheetMaxLength + 1)), Now));
        Assert.Equal(10_000, CharacterLimits.SheetMaxLength);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://example.com/blad")]
    [InlineData("data:text/html,hej")]
    [InlineData("www.example.com")]
    [InlineData("/lokal/sida")]
    public void Create_RejectsUnsafeOrRelativeUrls(string url)
    {
        Assert.Throws<CampaignRuleException>(() => Character.Create(7, "anna", false, Input(url: url), Now));
    }

    [Fact]
    public void Update_ChangesFieldsAndTimestamp()
    {
        var character = Character.Create(7, "anna", false, Input(), Now);
        var later = Now.AddHours(3);

        character.Update(Input("Aldric", "HP 3/12, förgiftad"), later);

        Assert.Equal("HP 3/12, förgiftad", character.Sheet);
        Assert.Equal(later, character.UpdatedAt);
        Assert.Equal(Now, character.CreatedAt);
    }

    [Fact]
    public void Npc_HasOnlyNameAndGmNote()
    {
        var npc = Character.Create(7, "gm", isNpc: true,
            new CharacterInput("  Hövdingen Grok ", "HP 40", "https://example.com", "D&D 5e", "  Vet var nyckeln finns.  "), Now);

        Assert.Equal("Hövdingen Grok", npc.Name);
        Assert.Equal("Vet var nyckeln finns.", npc.GmNote);
        Assert.Equal("", npc.Sheet);
        Assert.Null(npc.SheetUrl);
        Assert.Null(npc.RuleSystem);
    }

    [Fact]
    public void Npc_IgnoresInvalidSheetFields()
    {
        // Fälten används inte för NPC:er och ska därför inte heller kunna stoppa en sparning.
        var npc = Character.Create(7, "gm", isNpc: true, new CharacterInput("Grok", null, "javascript:alert(1)", null, null), Now);

        Assert.Null(npc.SheetUrl);
        Assert.Null(npc.GmNote);
    }

    [Fact]
    public void Npc_GmNoteHasMaxLength()
    {
        Assert.Throws<CampaignRuleException>(() => Character.Create(7, "gm", isNpc: true,
            new CharacterInput("Grok", null, null, null, new string('a', CharacterLimits.GmNoteMaxLength + 1)), Now));
    }

    [Fact]
    public void PlayerCharacter_NeverGetsGmNote()
    {
        var character = Character.Create(7, "anna", isNpc: false, new CharacterInput("Aldric", "HP 12", null, null, "Hemlig notering"), Now);

        Assert.Null(character.GmNote);
    }

    [Fact]
    public void Npc_CanBeArchivedAndRestored()
    {
        var npc = Character.Create(7, "gm", isNpc: true, new CharacterInput("Grok", null, null, null), Now);
        Assert.False(npc.IsArchived);

        npc.SetArchived(true);
        Assert.True(npc.IsArchived);

        npc.SetArchived(false);
        Assert.False(npc.IsArchived);
    }

    [Fact]
    public void PlayerCharacter_CannotBeArchived()
    {
        var character = Character.Create(7, "anna", isNpc: false, Input(), Now);

        Assert.Throws<CampaignRuleException>(() => character.SetArchived(true));
        Assert.False(character.IsArchived);
    }

    [Fact]
    public void Post_CanBeWrittenAsCharacter()
    {
        Assert.Equal(5, Post.Create(3, "anna", "Jag drar svärdet.", Now, characterId: 5).CharacterId);
        Assert.Null(Post.Create(3, "anna", "Utan karaktär.", Now).CharacterId);
    }
}

public class CharacterPermissionTests
{
    [Theory]
    [InlineData(CampaignRole.GameMaster, true)]
    [InlineData(CampaignRole.Player, true)]
    [InlineData(CampaignRole.None, false)]
    public void CanCreateCharacter_OnlyParticipants(CampaignRole role, bool expected)
    {
        Assert.Equal(expected, CampaignPermissions.CanCreateCharacter(role));
    }

    [Theory]
    [InlineData(CampaignRole.GameMaster, "gm", "anna", true)]   // GM redigerar alla
    [InlineData(CampaignRole.Player, "anna", "anna", true)]     // ägaren
    [InlineData(CampaignRole.Player, "bertil", "anna", false)]  // annan spelare
    [InlineData(CampaignRole.None, "anna", "anna", false)]      // ägare som inte längre deltar
    public void CanEditCharacter(CampaignRole role, string userId, string ownerId, bool expected)
    {
        Assert.Equal(expected, CampaignPermissions.CanEditCharacter(role, userId, ownerId));
    }

    [Theory]
    [InlineData(CampaignRole.Player, "anna", "anna", false, true)]   // egen karaktär
    [InlineData(CampaignRole.Player, "anna", "bertil", false, false)] // annans karaktär
    [InlineData(CampaignRole.Player, "anna", "gm", true, false)]      // NPC
    [InlineData(CampaignRole.GameMaster, "gm", "gm", true, true)]     // GM som NPC
    [InlineData(CampaignRole.GameMaster, "gm", "anna", false, false)] // GM som spelarens karaktär
    [InlineData(CampaignRole.None, "anna", "anna", false, false)]
    public void CanPostAsCharacter(CampaignRole role, string userId, string ownerId, bool isNpc, bool expected)
    {
        Assert.Equal(expected, CampaignPermissions.CanPostAsCharacter(role, userId, ownerId, isNpc));
    }
}
