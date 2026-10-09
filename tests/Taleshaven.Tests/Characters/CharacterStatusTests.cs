using Taleshaven.Core;
using Taleshaven.Core.Characters;
using Taleshaven.Core.Dice;

namespace Taleshaven.Tests.Characters;

/// <summary>Räknare, tillstånd och sparade slag på en karaktär (B56).</summary>
public class CharacterStatusTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

    private static Character Ilse() => Character.Create(1, "freja", false, new CharacterInput("Ilse Marrow", "", null, null), Now);

    [Fact]
    public void CountersCanBeAddedAdjustedAndRemoved()
    {
        var ilse = Ilse();
        var hp = ilse.AddCounter(" HP ", 21, 21, Now);

        ilse.AdjustCounter(hp, -5, Now);
        ilse.AdjustCounter(hp, 30, Now);   // över max går bra, t.ex. tillfälliga HP

        var counter = ilse.Counters.Single();
        Assert.Equal("HP", counter.Label);
        Assert.Equal(46, counter.Current);
        Assert.Equal(21, counter.Max);

        ilse.UpdateCounter(hp, "Hit points", 10, 25, Now);
        Assert.Equal((10, 25), (counter.Current, counter.Max));

        ilse.RemoveCounter(hp, Now);
        Assert.Empty(ilse.Counters);
    }

    [Theory]
    [InlineData("", 1, 10)]
    [InlineData("HP", 1, 0)]
    [InlineData("HP", 1, 10_000)]
    public void InvalidCountersAreRejected(string label, int current, int max)
    {
        Assert.Throws<CampaignRuleException>(() => Ilse().AddCounter(label, current, max, Now));
    }

    [Fact]
    public void CounterValueIsClampedToLimits()
    {
        var ilse = Ilse();
        var arrows = ilse.AddCounter("Arrows", 5, 20, Now);

        ilse.AdjustCounter(arrows, -50_000, Now);

        Assert.Equal(-CharacterTrackers.MaxCounterValue, ilse.Counters.Single().Current);
    }

    [Fact]
    public void ConditionsAreUniqueIgnoringCase()
    {
        var ilse = Ilse();

        ilse.AddCondition("Poisoned", Now);
        ilse.AddCondition(" poisoned ", Now);
        ilse.AddCondition("Bloodied", Now);

        Assert.Equal(["Poisoned", "Bloodied"], ilse.Conditions.Select(c => c.Name));

        ilse.RemoveCondition(ilse.Conditions[0].Uid, Now);
        Assert.Equal("Bloodied", ilse.Conditions.Single().Name);
    }

    [Fact]
    public void SavedRollsAreValidatedAndNormalized()
    {
        var ilse = Ilse();

        ilse.AddSavedRoll("Rapier", "d20+5", Now);

        var roll = ilse.SavedRolls.Single();
        Assert.Equal(("Rapier", "1d20+5"), (roll.Label, roll.Notation));
        Assert.Throws<CampaignRuleException>(() => ilse.AddSavedRoll("Odd", "1d7", Now));

        ilse.UpdateSavedRoll(roll.Uid, " Rapier (finesse) ", "1d20 + 6", Now);
        Assert.Equal(("Rapier (finesse)", "1d20+6"), (roll.Label, roll.Notation));
        Assert.Throws<CampaignRuleException>(() => ilse.UpdateSavedRoll(roll.Uid, "Rapier", "", Now));
    }

    [Fact]
    public void EditingACounterChangesNameAndMaxAndLowersTheValue()
    {
        var ilse = Ilse();
        var hp = ilse.AddCounter("HP", 12, 12, Now);

        new CharacterStatusChange.EditCounter(hp, "Hit points", 20).ApplyTo(ilse, Now);
        Assert.Equal(("Hit points", 12, 20), (ilse.Counters[0].Label, ilse.Counters[0].Current, ilse.Counters[0].Max));

        new CharacterStatusChange.EditCounter(hp, "Hit points", 10).ApplyTo(ilse, Now);
        Assert.Equal((10, 10), (ilse.Counters[0].Current, ilse.Counters[0].Max));

        Assert.Throws<CampaignRuleException>(() => new CharacterStatusChange.EditCounter(hp, "HP", 0).ApplyTo(ilse, Now));
    }

    [Fact]
    public void CountersAndRollsCanBeMovedButNotPastTheEnds()
    {
        var ilse = Ilse();
        var hp = ilse.AddCounter("HP", 12, 12, Now);
        var ki = ilse.AddCounter("Ki", 3, 3, Now);
        var arrows = ilse.AddCounter("Arrows", 20, 20, Now);
        var rapier = ilse.AddSavedRoll("Rapier", "1d20+5", Now);
        var dagger = ilse.AddSavedRoll("Dagger", "1d20+5", Now);

        new CharacterStatusChange.Move(arrows, -1).ApplyTo(ilse, Now);
        Assert.Equal([hp, arrows, ki], ilse.Counters.Select(c => c.Uid));
        new CharacterStatusChange.Move(hp, -1).ApplyTo(ilse, Now);
        Assert.Equal([hp, arrows, ki], ilse.Counters.Select(c => c.Uid));
        new CharacterStatusChange.Move(hp, 5).ApplyTo(ilse, Now);
        Assert.Equal([arrows, ki, hp], ilse.Counters.Select(c => c.Uid));

        new CharacterStatusChange.Move(dagger, -1).ApplyTo(ilse, Now);
        Assert.Equal([dagger, rapier], ilse.SavedRolls.Select(r => r.Uid));

        var prone = ilse.AddCondition("Prone", Now);
        Assert.Throws<CampaignRuleException>(() => new CharacterStatusChange.Move(prone, 1).ApplyTo(ilse, Now));
    }

    [Fact]
    public void ListsHaveMaximumSizes()
    {
        var ilse = Ilse();
        for (var i = 0; i < CharacterTrackers.MaxConditions; i++)
            ilse.AddCondition($"Condition {i}", Now);

        Assert.Throws<CampaignRuleException>(() => ilse.AddCondition("One too many", Now));
    }

    [Fact]
    public void ChangesAreAppliedThroughStatusChange()
    {
        var ilse = Ilse();

        new CharacterStatusChange.AddCounter("HP", 21, 21).ApplyTo(ilse, Now);
        new CharacterStatusChange.AdjustCounter(ilse.Counters[0].Uid, -3).ApplyTo(ilse, Now);
        new CharacterStatusChange.AddCondition("Prone").ApplyTo(ilse, Now);

        Assert.Equal(18, ilse.Counters[0].Current);
        Assert.Equal("Prone", ilse.Conditions.Single().Name);
    }

    [Fact]
    public void NewItemsGetASuggestedIconThatCanBeChangedOrSuggestedAgain()
    {
        var ilse = Ilse();
        int? Suggest(string name) => name.Contains("sword", StringComparison.OrdinalIgnoreCase) ? 7 : null;

        new CharacterStatusChange.AddSavedRoll("Shortsword", "1d20+5").ApplyTo(ilse, Now, Suggest);
        new CharacterStatusChange.AddCounter("HP", 21, 21).ApplyTo(ilse, Now, Suggest);
        var roll = ilse.SavedRolls.Single();
        Assert.Equal(7, roll.IconId);
        Assert.Null(ilse.Counters.Single().IconId);

        new CharacterStatusChange.SetIcon(roll.Uid, null).ApplyTo(ilse, Now, Suggest);
        Assert.Null(roll.IconId);

        new CharacterStatusChange.SuggestIcon(roll.Uid).ApplyTo(ilse, Now, Suggest);
        Assert.Equal(7, roll.IconId);

        // Ett nytt namn byter inte ikonen av sig själv; den sparas tills någon ändrar den.
        new CharacterStatusChange.UpdateSavedRoll(roll.Uid, "Dagger", "1d20+5").ApplyTo(ilse, Now, Suggest);
        Assert.Equal(7, roll.IconId);
    }

    [Fact]
    public void StandardConditionsIncludeFifthEditionAndBloodied()
    {
        Assert.Contains("Unconscious", CharacterTrackers.StandardConditions);
        Assert.Contains("Bloodied", CharacterTrackers.StandardConditions);
        Assert.Equal(16, CharacterTrackers.StandardConditions.Count);
    }
}
