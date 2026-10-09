using Taleshaven.Core;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Text;

namespace Taleshaven.Tests.Campaigns;

/// <summary>Kampanjens taggar, standardtärning och sökning i kampanjlistan (B46, B47, B49).</summary>
public class CampaignTagTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);

    private static Campaign Create(string? tags = null, string? defaultRoll = null) =>
        Campaign.Create("gm", "Lanterns of Greywater", null, 4, Now, tags, defaultRoll);

    [Fact]
    public void TagsAreNormalizedAndOptional()
    {
        Assert.Equal(["dnd5e", "horror"], Create("#DnD5e, horror #dnd5e").Tags);
        Assert.Empty(Create().Tags);
        Assert.Empty(Create("   ").Tags);
    }

    [Fact]
    public void AtMostTenTags()
    {
        var eleven = string.Join(" ", Enumerable.Range(1, CampaignLimits.MaxTags + 1).Select(i => $"tag{i}"));

        var ex = Assert.Throws<CampaignRuleException>(() => Create(eleven));
        Assert.Contains("A campaign can have at most 10 tags", ex.Message);
    }

    [Theory]
    [InlineData("dark souls!")]
    [InlineData("#aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void InvalidTagsAreRejected(string tags)
    {
        Assert.Throws<CampaignRuleException>(() => Create(tags));
    }

    [Theory]
    [InlineData(null, "1d20")]
    [InlineData("", "1d20")]
    [InlineData("d100", "1d100")]
    [InlineData(" 2D6 ", "2d6")]
    [InlineData("1d20+0", "1d20")]
    [InlineData("3d6-1", "3d6-1")]
    public void DefaultRollIsNormalized(string? input, string expected)
    {
        Assert.Equal(expected, Create(defaultRoll: input).DefaultRoll);
    }

    [Theory]
    [InlineData("1d7")]
    [InlineData("lots")]
    public void InvalidDefaultRollIsRejected(string input)
    {
        Assert.Throws<CampaignRuleException>(() => Create(defaultRoll: input));
    }

    [Fact]
    public void UpdateDetailsChangesTagsAndDefaultRoll()
    {
        var campaign = Create("#dnd5e");

        campaign.UpdateDetails("Lanterns of Greywater", null, 4, "#mystery #fog", "1d100");

        Assert.Equal(["mystery", "fog"], campaign.Tags);
        Assert.Equal("1d100", campaign.DefaultRoll);
    }

    [Fact]
    public void InvalidUpdateChangesNothing()
    {
        var campaign = Create("#dnd5e");

        Assert.Throws<CampaignRuleException>(() => campaign.UpdateDetails("New name", null, 4, "#ok", "1d7"));

        Assert.Equal("Lanterns of Greywater", campaign.Name);
        Assert.Equal(["dnd5e"], campaign.Tags);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("lanterns", true)]
    [InlineData("grey", true)]                // början av ett ord i namnet
    [InlineData("water", false)]              // inte mitt i ett ord
    [InlineData("dnd", true)]                 // början av en tagg
    [InlineData("#horror", true)]
    [InlineData("lan hor", true)]             // alla ord måste matcha
    [InlineData("lan scifi", false)]
    [InlineData("LANTERNS", true)]
    public void SearchMatchesStartOfNameWordsAndTags(string query, bool expected)
    {
        Assert.Equal(expected, Campaign.MatchesSearch("Lanterns of Greywater", ["dnd5e", "horror"], TagList.ParseSearch(query)));
    }
}
