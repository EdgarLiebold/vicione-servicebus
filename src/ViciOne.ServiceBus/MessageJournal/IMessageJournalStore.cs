using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.MessageJournal;
/// <summary>Persists sanitized message-journal entries within declared finite limits.</summary>
/// <remarks>
/// This is an optional diagnostic journal, never the ViciOne Suite audit owner. Implementations must
/// enforce <see cref="Limits"/> on every append and must not create a queue in front of persistence.
/// </remarks>
public interface IMessageJournalStore
{
    /// <summary>Gets the finite entry-size, capacity, and retention limits enforced by this store.</summary>
    MessageJournalStoreLimits Limits { get; }

    /// <summary>Enforces <see cref="Limits"/> and persists one sanitized entry without internal queuing or retry.</summary>
    /// <param name="entry">The immutable sanitized entry to persist.</param>
    /// <param name="cancellationToken">The token that cancels retention, capacity enforcement, or persistence.</param>
    /// <returns>A value task that completes when the bounded append has completed.</returns>
    ValueTask AppendAsync(MessageJournalEntry entry, CancellationToken cancellationToken);
}
