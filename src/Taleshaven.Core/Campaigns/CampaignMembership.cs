namespace Taleshaven.Core.Campaigns;

// En godkänd spelare i en kampanj. GM lagras som Campaign.GameMasterId och har ingen membership-rad.
public class CampaignMembership
{
    private CampaignMembership() { }

    public CampaignMembership(string userId, DateTimeOffset joinedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        UserId = userId;
        JoinedAt = joinedAt;
    }

    public int CampaignId { get; private set; }
    public string UserId { get; private set; } = "";
    public DateTimeOffset JoinedAt { get; private set; }
}
