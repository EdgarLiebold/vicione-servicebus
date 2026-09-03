using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>Producer-side durable acceptance API.</summary>
public interface IDurableSender<TBus>
    where TBus : class, IBus
{
    /// <summary>
    /// Commits an already serialized send intent to durable storage. Returning successfully means the durable intent is
    /// restart-safe; it does not claim that the target transport has already accepted the message.
    /// </summary>
    Task<DurableSendAdmissionResult> AdmitAsync(
        SerializedDurableSend message,
        CancellationToken cancellationToken = default);
}
