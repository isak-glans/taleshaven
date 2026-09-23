namespace Taleshaven.Core;

/// <summary>
/// En handling bryter mot en affärsregel eller behörighet. Meddelandet är skrivet för att visas för användaren.
/// </summary>
public class CampaignRuleException(string message) : Exception(message);
