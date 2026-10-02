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
        Assert.Equal(DiceMode.Normal, roll.Mode);
        Assert.Equal([6, 6], roller.RequestedSides);
    }

    [Fact]
    public void NegativeModifierIsSubtracted()
    {
        var roll = DiceRoll.Roll(new DiceNotation(1, 20, -2), null, new FixedRoller(1));

        Assert.Equal(-1, roll.Total);
        Assert.Null(roll.Label);
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
    public void AdvantageRollsTwoD20AndKeepsTheHigher()
    {
        var roller = new FixedRoller(7, 16);

        var roll = DiceRoll.Roll(new RollRequest("1d20+5", "Attack", DiceMode.Advantage), roller);

        Assert.Equal([7, 16], roll.Results);
        Assert.Equal(21, roll.Total);
        Assert.Equal(DiceMode.Advantage, roll.Mode);
        Assert.Equal([20, 20], roller.RequestedSides);
        Assert.Equal(1, DiceRollView.From(roll).KeptIndex);
    }

    [Fact]
    public void DisadvantageKeepsTheLower()
    {
        var roll = DiceRoll.Roll(new RollRequest("d20-1", null, DiceMode.Disadvantage), new FixedRoller(12, 3));

        Assert.Equal(2, roll.Total);
        Assert.Equal(1, DiceRollView.From(roll).KeptIndex);
    }

    [Theory]
    [InlineData("2d20")]
    [InlineData("1d6+2")]
    public void AdvantageOnlyWorksWithASingleD20(string notation)
    {
        Assert.Throws<CampaignRuleException>(() =>
            DiceRoll.Roll(new RollRequest(notation, null, DiceMode.Advantage), new FixedRoller(1, 1, 1, 1)));
    }

    [Fact]
    public void NormalRollHasNoKeptDie()
    {
        Assert.Null(DiceRollView.From(DiceRoll.Roll(new DiceNotation(2, 6), null, new FixedRoller(1, 2))).KeptIndex);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("3d7")]
    [InlineData("lots")]
    public void InvalidRequestsAreRejected(string? notation)
    {
        Assert.Throws<CampaignRuleException>(() => DiceRoll.Roll(new RollRequest(notation), new FixedRoller(1)));
    }

    [Fact]
    public void RollsAreRolledWhenPostIsCreated()
    {
        var post = Post.Create(3, "anna", "Sigrun charges.", Now,
            rolls: [new RollRequest("1d20+5", "Attack"), new RollRequest(" 2d6 ", "Damage")], roller: new FixedRoller(17, 3, 4));

        Assert.Equal("Sigrun charges.", post.Content);
        Assert.Equal(2, post.Rolls.Count);
        Assert.Equal(22, post.Rolls[0].Total);
        Assert.Equal("Attack", post.Rolls[0].Label);
        Assert.Equal([3, 4], post.Rolls[1].Results);
        Assert.True(post.HasRolls);
    }

    [Fact]
    public void DiceTagsInTextAreNoLongerRolled()
    {
        var post = Post.Create(3, "anna", "I roll [dice]1d20[/dice]", Now);

        Assert.Empty(post.Rolls);
        Assert.Equal("I roll [dice]1d20[/dice]", post.Content);
    }

    [Fact]
    public void PostWithOnlyRollsMayHaveNoText()
    {
        var post = Post.Create(3, "anna", "  ", Now, rolls: [new RollRequest("1d20+2", "Initiative")], roller: new FixedRoller(11));

        Assert.Equal("", post.Content);
        Assert.Equal(13, post.Rolls[0].Total);
    }

    [Fact]
    public void EmptyPostWithoutRollsIsRejected()
    {
        Assert.Throws<CampaignRuleException>(() => Post.Create(3, "anna", " ", Now));
    }

    [Fact]
    public void OrdinaryPostHasNoRolls()
    {
        Assert.Empty(Post.Create(3, "anna", "Hej", Now).Rolls);
    }

    [Fact]
    public void AtMostTenRollsPerPost()
    {
        var rolls = Enumerable.Repeat(new RollRequest("1d6"), ThreadLimits.MaxRollsPerPost + 1).ToList();

        Assert.Throws<CampaignRuleException>(() =>
            Post.Create(3, "anna", "Many", Now, rolls: rolls, roller: new FixedRoller(Enumerable.Repeat(1, 20).ToArray())));
    }
}
