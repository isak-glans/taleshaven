namespace Taleshaven.Web.Components.Account;

/// <summary>
/// Mejlet "New message from …" (B73). Texten i meddelandet följer inte med, bara en länk till konversationen.
/// </summary>
public interface IMessageEmailSender
{
    Task SendNewMessageAsync(string email, string recipientName, string senderName, string link);
}

/// <summary>Utan <c>Email:Host</c> skickas inga mejl (B70).</summary>
internal sealed class NoOpMessageEmailSender : IMessageEmailSender
{
    public Task SendNewMessageAsync(string email, string recipientName, string senderName, string link) => Task.CompletedTask;
}
