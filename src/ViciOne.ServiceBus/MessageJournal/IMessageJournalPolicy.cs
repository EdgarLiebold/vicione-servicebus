#nullable enable
namespace ViciOne.ServiceBus.MessageJournal;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Selects and sanitizes raw message observations before they can reach a persistence provider.
/// </summary>
public interface IMessageJournalPolicy
{
    /// <summary>
    /// Returns sanitized content to persist, or <see langword="null"/> to exclude the observation.
    /// </summary>
    ValueTask<MessageJournalProjection?> ProjectAsync(
        MessageJournalCapture capture,
        CancellationToken cancellationToken);
}
