using Microsoft.AspNetCore.Components;
using Taleshaven.Core.Messages;
using Taleshaven.Web.Components.Account;

namespace Taleshaven.Web.Components.Pages.Messages;

/// <summary>
/// Mejlar mottagaren om ett nytt meddelande (B73), om <see cref="SentMessage.NotifyEmail"/> säger det. Mejlet skickas i
/// bakgrunden, så att den som skriver inte väntar på SMTP-servern.
/// </summary>
internal static class MessageNotification
{
    public static void Send(IMessageEmailSender sender, NavigationManager navigation, SentMessage sent, ILogger logger)
    {
        if (sent.NotifyEmail is not { } email)
            return;
        var link = navigation.ToAbsoluteUri(MessageLinks.Conversation(sent.ConversationId)).ToString();
        _ = Task.Run(async () =>
        {
            try
            {
                await sender.SendNewMessageAsync(email, sent.RecipientName, sent.SenderName, link);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Mejlet om meddelande {PostId} kunde inte skickas", sent.PostId);
            }
        });
    }
}
