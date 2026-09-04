using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>
/// Process-local completion capability for a durable send dispatched through a volatile transport. Transport adapters
/// may attach this object to an in-process consume pipeline, but MUST NOT serialize it into message headers or payload.
/// Invoking it after the logical consumer pipeline has committed removes the persisted producer intent only when the
/// capability still matches that intent's persisted generation. A stale capability cannot remove a later re-admission.
/// </summary>
public interface IDurableSendConsumerCompletion
{
    /// <summary>
    /// Gets the durable send id value.
    /// </summary>
    DurableSendId DurableSendId { get; }

    /// <summary>
    /// Reports successful logical consumer completion. The operation is idempotent: <see langword="false"/> means the
    /// intent was already removed or the capability belongs to an older discarded incarnation. A valid capability is
    /// bound to one persisted intent generation, so a later successful logical completion is allowed to resolve an
    /// overlapping retry/quarantine race for that generation without affecting a future re-admission.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    ValueTask<bool> CompleteAsync(CancellationToken cancellationToken = default);
}
