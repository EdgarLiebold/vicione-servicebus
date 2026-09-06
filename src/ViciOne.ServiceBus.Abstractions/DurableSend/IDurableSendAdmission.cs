using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Provider-level admission SPI for an already serialized durable send intent.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
public interface IDurableSendAdmission<TBus>
    where TBus : class, IBus
{
    /// <summary>Commits the exact serialized intent to durable storage. Success confirms persistence commit only.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the admit outcome.</returns>
    Task<DurableSendAdmissionResult> AdmitAsync(
        SerializedDurableSend message,
        CancellationToken cancellationToken = default);
}
