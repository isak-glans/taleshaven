using Taleshaven.Core.Portraits;

namespace Taleshaven.Tests.Portraits;

/// <summary>Automatiskt förslag på ikon efter namnet (B58).</summary>
public class IconMatcherTests
{
    private static readonly (int Id, IReadOnlyList<string> Tags)[] Icons =
    [
        (1, ["longsword", "sword", "weapon"]),
        (2, ["shortsword", "sword", "weapon"]),
        (3, ["dagger", "weapon"]),
        (4, ["daggers", "dagger", "throwing-knives", "weapon"]),
        (5, ["thieves-tools", "lockpicks", "tool"]),
        (6, ["bow", "arrows", "quiver", "weapon"]),
        (7, ["spellbook", "book", "magic"]),
        (8, ["potion", "healing", "red-potion"]),
        (9, ["mace", "weapon"]),
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
