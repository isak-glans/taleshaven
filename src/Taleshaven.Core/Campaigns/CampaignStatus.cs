namespace Taleshaven.Core.Campaigns;

// Värdena lagras i databasen och styr sorteringen i kampanjlistan. Ändra inte befintliga värden.
public enum CampaignStatus
{
    OpenForApplications = 0,
    Ongoing = 1,
    Closed = 2,
    Archived = 3,
}
