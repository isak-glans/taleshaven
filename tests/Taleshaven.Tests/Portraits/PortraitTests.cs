using Taleshaven.Core;
using Taleshaven.Core.Portraits;

namespace Taleshaven.Tests.Portraits;

public class PortraitTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("#dvärg #krigare", new[] { "dvärg", "krigare" })]
    [InlineData("Dvärg, KRIGARE", new[] { "dvärg", "krigare" })]
    [InlineData("  #ÄLVA   #ålderstigen\n#öken-nomad ", new[] { "älva", "ålderstigen", "öken-nomad" })]
    [InlineData("#dvärg #Dvärg dvärg", new[] { "dvärg" })]
    public void Parse_NormalizesTags(string input, string[] expected)
    {
        Assert.Equal(expected, PortraitTags.Parse(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("# ##")]
    public void Parse_RequiresAtLeastOneTag(string? input)
    {
        Assert.Throws<CampaignRuleException>(() => PortraitTags.Parse(input));
    }

    [Theory]
    [InlineData("#dvärg! #krigare")]
    [InlineData("<script>")]
    [InlineData("#två.ord")]
    public void Parse_RejectsInvalidCharacters(string input)
    {
        Assert.Throws<CampaignRuleException>(() => PortraitTags.Parse(input));
    }

    [Fact]
    public void Parse_EnforcesLimits()
    {
        Assert.Throws<CampaignRuleException>(() => PortraitTags.Parse(new string('a', PortraitTags.TagMaxLength + 1)));
        Assert.Throws<CampaignRuleException>(() =>
            PortraitTags.Parse(string.Join(" ", Enumerable.Range(1, PortraitTags.MaxTags + 1).Select(i => $"tagg{i}"))));
    }

    [Theory]
    [InlineData("dvä KRI", new[] { "dvä", "kri" })]
    [InlineData("#dvärg! <b>", new[] { "dvärg", "b" })]
    [InlineData("", new string[0])]
    [InlineData(null, new string[0])]
    public void ParseSearch_IgnoresInvalidCharacters(string? query, string[] expected)
    {
        Assert.Equal(expected, PortraitTags.ParseSearch(query));
    }

    [Fact]
    public void Format_WritesHashTags()
    {
        Assert.Equal("#dvärg #krigare", PortraitTags.Format(["dvärg", "krigare"]));
    }

    [Fact]
    public void Create_StoresNormalizedTagsAndSource()
    {
        var portrait = Portrait.Create("abc.webp", "anna", "#Dvärg #krigare", "  Egen bild, CC BY 4.0  ", Now);

        Assert.Equal(["dvärg", "krigare"], portrait.Tags);
        Assert.Equal("Egen bild, CC BY 4.0", portrait.Source);
        Assert.Equal("anna", portrait.UploadedById);
        Assert.Equal(Now, portrait.CreatedAt);
    }

    [Fact]
    public void Update_ChangesTagsAndClearsEmptySource()
    {
        var portrait = Portrait.Create("abc.webp", "anna", "#dvärg", "Källa", Now);

        portrait.Update("#alv #magiker", " ");

        Assert.Equal(["alv", "magiker"], portrait.Tags);
        Assert.Null(portrait.Source);
    }

    [Fact]
    public void Source_HasMaxLength()
    {
        Assert.Throws<CampaignRuleException>(() =>
            Portrait.Create("abc.webp", "anna", "#dvärg", new string('a', PortraitTags.SourceMaxLength + 1), Now));
    }
}
