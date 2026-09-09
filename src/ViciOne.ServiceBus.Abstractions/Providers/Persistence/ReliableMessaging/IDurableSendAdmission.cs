using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Provider-level admission SPI for an already serialized durable send intent.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
public interface IDurableSendAdmission<TBus>
    where TBus : class, IBus
{
    /// <summary>Commits the exact serialized intent to durable storage. Success confirms persistence commit only.</summary>
    /// <param name="message">The validated serialized intent.</param>
    /// <param name="cancellationToken">The token used to cancel admission.</param>
    /// <returns>A task containing the idempotent admission result.</returns>
    Task<DurableSendAdmissionResult> AdmitAsync(
        SerializedDurableSend message,
        CancellationToken cancellationToken = default);
}
