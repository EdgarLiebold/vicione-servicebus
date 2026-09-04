using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.ProviderAbstractions;

/// <summary>Provider-level admission SPI for an already serialized durable send intent.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDurableSendAdmission<TBus>
    where TBus : class, IBus
{
    /// <summary>
    /// Commits the exact serialized intent to durable storage. Success confirms persistence commit only.
    /// </summary>
    Task<DurableSendAdmissionResult> AdmitAsync(
        SerializedDurableSend message,
        CancellationToken cancellationToken = default);
}
