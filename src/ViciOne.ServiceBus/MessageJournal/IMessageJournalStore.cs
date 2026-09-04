using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace ViciOne.ServiceBus.MessageJournal;
/// <summary>
/// Persists sanitized message-journal entries within declared finite limits.
/// </summary>
/// <remarks>
/// This is an optional diagnostic journal, never the ViciOne Suite audit owner. Implementations must
/// enforce <see cref="Limits"/> on every append and must not create a queue in front of persistence.
/// </remarks>
public interface IMessageJournalStore
{
    /// <summary>
    /// Gets the limits value.
    /// </summary>
    MessageJournalStoreLimits Limits { get; }

    /// <summary>
    /// Performs the append operation.
    /// </summary>
    /// <param name="entry">The entry value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    ValueTask AppendAsync(MessageJournalEntry entry, CancellationToken cancellationToken);
}
