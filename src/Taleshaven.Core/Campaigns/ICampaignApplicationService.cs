namespace Taleshaven.Core.Campaigns;

/// <summary>
/// Ansökningar till kampanjer. Regelbrott och saknad behörighet ger <see cref="CampaignRuleException"/>.
/// </summary>
public interface ICampaignApplicationService
{
    Task ApplyAsync(int campaignId, string userId, string? message, CancellationToken cancellationToken = default);

    /// <summary>Väntande ansökningar, äldst först. Tom lista om användaren inte är kampanjens GM.</summary>
    Task<IReadOnlyList<PendingApplication>> GetPendingApplicationsAsync(int campaignId, string gameMasterId, CancellationToken cancellationToken = default);

    Task ApproveAsync(int campaignId, Guid applicationId, string gameMasterId, CancellationToken cancellationToken = default);

    Task RejectAsync(int campaignId, Guid applicationId, string gameMasterId, CancellationToken cancellationToken = default);
}

public sealed record PendingApplication(Guid Id, string ApplicantName, string Message, DateTimeOffset SubmittedAt);
