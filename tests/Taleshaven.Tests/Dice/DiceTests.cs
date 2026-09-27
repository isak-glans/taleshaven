using Taleshaven.Core;
using Taleshaven.Core.Dice;
using Taleshaven.Core.Threads;

namespace Taleshaven.Tests.Dice;

public class DiceNotationTests
{
    [Theory]
    [InlineData("2d6+3", 2, 6, 3, "2d6+3")]
    [InlineData("1d20", 1, 20, 0, "1d20")]
    [InlineData("d100", 1, 100, 0, "1d100")]
    [InlineData("3d8-2", 3, 8, -2, "3d8-2")]
    [InlineData(" 2 D 12 + 1 ", 2, 12, 1, "2d12+1")]
    [InlineData("50d4", 50, 4, 0, "50d4")]
    [InlineData("1d10+999", 1, 10, 999, "1d10+999")]
    [InlineData("1d6+0", 1, 6, 0, "1d6")]
    public void ParsesValidNotation(string input, int count, int sides, int modifier, string normalized)
    {
        Assert.True(DiceNotation.TryParse(input, out var notation, out var error));
        Assert.Null(error);
        Assert.Equal(count, notation.Count);
        Assert.Equal(sides, notation.Sides);
        Assert.Equal(modifier, notation.Modifier);
        Assert.Equal(normalized, notation.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2d")]
    [InlineData("d")]
    [InlineData("2x6")]
    [InlineData("2d6+")]
    [InlineData("2d6+3+1")]
    [InlineData("-1d6")]
    [InlineData("2d6*2")]
    [InlineData("99999999999d6")]
    public void RejectsMalformedNotation(string? input)
    {
        Assert.False(DiceNotation.TryParse(input, out _, out var error));
        Assert.NotNull(error);
    }

    [Theory]
    [InlineData("1d7")]
    [InlineData("1d3")]
    [InlineData("1d1000")]
    [InlineData("0d6")]
    [InlineData("51d6")]
    [InlineData("1d6+1000")]
    [InlineData("1d6-1000")]
    public void RejectsValuesOutsideLimits(string input)
    {
        Assert.False(DiceNotation.TryParse(input, out _, out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void AllowedSidesMatchRequirements()
    {
        Assert.Equal([4, 6, 8, 10, 12, 20, 100], DiceNotation.AllowedSides);
    }
}

public class DiceCommandTests
{
    [Theory]
    [InlineData("/roll 2d6+3", "2d6+3", null)]
    [InlineData("/roll 1d20", "1d20", null)]
    [InlineData("/ROLL d20 attack the orc", "1d20", "attack the orc")]
    [InlineData("  /roll   1d100   ", "1d100", null)]
    public void RecognizesCommands(string input, string expectedNotation, string? expectedLabel)
    {
        Assert.True(DiceCommand.IsCommand(input, out var notation, out var label, out var error));
        Assert.Null(error);
        Assert.Equal(expectedNotation, notation.ToString());
        Assert.Equal(expectedLabel, label);
    }

    [Theory]
    [InlineData("Hello everyone")]
    [InlineData("I roll 2d6")]
    [InlineData("/slå 2d6")]
    [InlineData("/rollercoaster")]
    [InlineData("/rolled")]
    [InlineData("")]
    public void IgnoresOrdinaryText(string input)
    {
        Assert.False(DiceCommand.IsCommand(input, out _, out _, out _));
    }

    [Theory]
    [InlineData("/roll")]
    [InlineData("/roll 3d7")]
    [InlineData("/roll abc")]
    public void ReportsErrorsForInvalidCommands(string input)
    {
        Assert.True(DiceCommand.IsCommand(input, out _, out _, out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void RejectsTooLongLabel()
    {
        var input = "/roll 1d20 " + new string('a', DiceRoll.LabelMaxLength + 1);

        Assert.True(DiceCommand.IsCommand(input, out _, out _, out var error));
        Assert.NotNull(error);
    }
}

public class DiceRollTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    // Ger förbestämda värden i tur och ordning.
    private sealed class FixedRoller(params int[] values) : IDiceRoller
    {
        private int next;
        public List<int> RequestedSides { get; } = [];

        public int RollDie(int sides)
        {
            RequestedSides.Add(sides);
            return values[next++];
        }
    }

    [Fact]
    public void RollsEachDieAndAddsModifier()
    {
        var roller = new FixedRoller(4, 5);

        var roll = DiceRoll.Roll(new DiceNotation(2, 6, 3), "anfall", roller);

        Assert.Equal([4, 5], roll.Results);
        Assert.Equal(12, roll.Total);
        Assert.Equal("2d6+3", roll.Notation);
        Assert.Equal("anfall", roll.Label);
        Assert.Equal([6, 6], roller.RequestedSides);
        Assert.Equal("🎲 2d6+3 (anfall): [4, 5] + 3 = 12", roll.ToText());
    }

    [Fact]
    public void NegativeModifierIsSubtracted()
    {
        var roll = DiceRoll.Roll(new DiceNotation(1, 20, -2), null, new FixedRoller(1));

        Assert.Equal(-1, roll.Total);
        Assert.Null(roll.Label);
        Assert.Equal("🎲 1d20-2: [1] - 2 = -1", roll.ToText());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void RejectsOutOfRangeResultsFromRoller(int value)
    {
        Assert.Throws<InvalidOperationException>(() => DiceRoll.Roll(new DiceNotation(1, 6), null, new FixedRoller(value)));
    }

    [Fact]
    public void BlankLabelBecomesNull()
    {
        Assert.Null(DiceRoll.Roll(new DiceNotation(1, 6), "   ", new FixedRoller(3)).Label);
    }

    [Fact]
    public void RejectsTooLongLabel()
    {
        Assert.Throws<CampaignRuleException>(() =>
            DiceRoll.Roll(new DiceNotation(1, 6), new string('a', DiceRoll.LabelMaxLength + 1), new FixedRoller(3)));
    }

    [Fact]
    public void InlineDiceAreRolledWhenPostIsCreated()
    {
        var post = Post.Create(3, "anna", "Attack [dice]1d20+5[/dice], damage [DICE] 2d6 [/dice]!", Now, roller: new FixedRoller(17, 3, 4));

        Assert.Equal("Attack [dice:1], damage [dice:2]!", post.Content);
        Assert.Equal(2, post.Rolls.Count);
        Assert.Equal(22, post.Rolls[0].Total);
        Assert.Equal([3, 4], post.Rolls[1].Results);
        Assert.True(post.HasRolls);
    }

    [Fact]
    public void OrdinaryPostHasNoRolls()
    {
        Assert.Empty(Post.Create(3, "anna", "Hej", Now).Rolls);
    }

    [Fact]
    public void InvalidInlineDiceAreRejected()
    {
        Assert.Throws<CampaignRuleException>(() => Post.Create(3, "anna", "[dice]3d7[/dice]", Now, roller: new FixedRoller(1)));
        Assert.Throws<CampaignRuleException>(() => Post.Create(3, "anna", "[dice]lots[/dice]", Now, roller: new FixedRoller(1)));
    }

    [Fact]
    public void AtMostTenRollsPerPost()
    {
        var content = string.Concat(Enumerable.Repeat("[dice]1d6[/dice] ", ThreadLimits.MaxRollsPerPost + 1));

        Assert.Throws<CampaignRuleException>(() =>
            Post.Create(3, "anna", content, Now, roller: new FixedRoller(Enumerable.Repeat(1, 20).ToArray())));
    }

    [Fact]
    public void ReferencesAreReplacedOnlyForExistingRolls()
    {
        var html = InlineDice.ReplaceReferences("<p>[dice:1] and [dice:2] and [dice:9]</p>", 2, n => $"<b>{n}</b>");

        Assert.Equal("<p><b>1</b> and <b>2</b> and [dice:9]</p>", html);
    }
}
