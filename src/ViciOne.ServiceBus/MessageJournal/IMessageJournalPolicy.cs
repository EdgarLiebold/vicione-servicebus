using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.MessageJournal;
/// <summary>Selects and sanitizes raw message observations before they can reach a persistence provider.</summary>
public interface IMessageJournalPolicy
{
    /// <summary>Returns sanitized content to persist, or <see langword="null"/> to exclude the observation.</summary>
    /// <param name="capture">The capture used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the project outcome.</returns>
    ValueTask<MessageJournalProjection?> ProjectAsync(
        MessageJournalCapture capture,
        CancellationToken cancellationToken);
}
