using Taleshaven.Core.Portraits;

namespace Taleshaven.Tests.Portraits;

/// <summary>Automatiskt förslag på ikon efter namnet (B58).</summary>
public class IconMatcherTests
{
    private static readonly (int Id, IReadOnlyList<string> Tags)[] Icons =
    [
        (1, ["longsword", "icon", "sword", "weapon"]),
        (2, ["shortsword", "icon", "sword", "weapon"]),
        (3, ["dagger", "icon", "weapon"]),
        (4, ["daggers", "icon", "dagger", "throwing-knives", "weapon"]),
        (5, ["thieves-tools", "icon", "lockpicks", "tool"]),
        (6, ["bow", "icon", "arrows", "quiver", "weapon"]),
        (7, ["spellbook", "icon", "book", "magic"]),
        (8, ["potion", "icon", "healing", "red-potion"]),
        (9, ["mace", "icon", "weapon"]),
    ];

    [Theory]
    [InlineData("Shortsword", 2)]
    [InlineData("Short sword", 2)]
    [InlineData("Longsword attack", 1)]
    [InlineData("Scimitar of the sun sword", 1)]   // "sword" finns som tagg på flera; lägst id vinner
    [InlineData("Dagger", 3)]
    [InlineData("Daggers", 4)]
    [InlineData("Thieves' tools", 5)]
    [InlineData("Arrows", 6)]
    [InlineData("Spell slots (1st)", 7)]
    [InlineData("Potion of healing", 8)]
    [InlineData("MACE", 9)]
    public void SuggestsTheBestIcon(string name, int expected)
    {
        Assert.Equal(expected, IconMatcher.Suggest(name, Icons));
    }

    [Theory]
    [InlineData("HP")]
    [InlineData("Poisoned")]
    [InlineData("Weapon attack")]   // kategoritaggar ger inga träffar
    [InlineData("")]
    [InlineData(null)]
    public void NoIconWhenNothingFits(string? name)
    {
        Assert.Null(IconMatcher.Suggest(name, Icons));
    }
}
