namespace Taleshaven.Core.Campaigns;

public static class CampaignLimits
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 2000;
    public const int MinPlayers = 1;
    public const int MaxPlayers = 20;
    public const int ApplicationMessageMaxLength = 1000;

    /// <summary>Högst så många taggar på en kampanj (B47).</summary>
    public const int MaxTags = 10;

    /// <summary>Standardtärningen för nya kampanjer (B46).</summary>
    public const string DefaultRoll = "1d20";

    /// <summary>Kolumnens storlek för standardtärningen; den längsta normaliserade formeln är t.ex. "999d100-9999".</summary>
    public const int DefaultRollMaxLength = 16;

    /// <summary>Kampanjer per sida i kampanjlistan, bland de kampanjer man inte själv är med i (B48).</summary>
    public const int CampaignsPerPage = 25;

    /// <summary>Förslag på standardtärningar för olika spel (B46).</summary>
    public static readonly IReadOnlyList<string> CommonRolls = ["1d20", "1d100", "2d6", "3d6", "1d6", "1d8", "1d10", "1d12", "1d4"];
}
