using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Provider-level admission SPI for an already serialized durable send intent.</summary>
public interface IDurableSendAdmission<TBus>
    where TBus : class, IBus
{
    /// <summary>
    /// Commits the exact serialized intent to durable storage. Success confirms persistence commit only.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="message">The message processed by the operation.</param>
    Task<DurableSendAdmissionResult> AdmitAsync(
        SerializedDurableSend message,
        CancellationToken cancellationToken = default);
}
