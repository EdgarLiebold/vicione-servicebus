using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.MessageJournal;
/// <summary>Selects and sanitizes raw message observations before they can reach a persistence provider.</summary>
public interface IMessageJournalPolicy
{
    /// <summary>Returns sanitized content to persist, or <see langword="null"/> to exclude the observation.</summary>
    /// <param name="capture">The immutable raw observation to inspect and sanitize.</param>
    /// <param name="cancellationToken">The token that cancels policy projection.</param>
    /// <returns>A value task containing sanitized content, or <see langword="null"/> when the observation is excluded.</returns>
    ValueTask<MessageJournalProjection?> ProjectAsync(
        MessageJournalCapture capture,
        CancellationToken cancellationToken);
}
