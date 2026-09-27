using Taleshaven.Core;
using Taleshaven.Core.Users;

namespace Taleshaven.Tests.Users;

public class DeletedAccountTests
{
    [Fact]
    public void EnsureCanDelete_AllowsUserWithoutOwnCampaigns()
    {
        DeletedAccount.EnsureCanDelete(0);
    }

    [Theory]
    [InlineData(1, "a campaign")]
    [InlineData(3, "3 campaigns")]
    public void EnsureCanDelete_RejectsGameMaster(int campaigns, string expected)
    {
        var ex = Assert.Throws<CampaignRuleException>(() => DeletedAccount.EnsureCanDelete(campaigns));
        Assert.Contains(expected, ex.Message);
    }
}
