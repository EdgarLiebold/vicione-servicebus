using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Exposes low-level send operations required by transport and middleware adapters.</summary>
public interface ITransportSendEndpoint :
    ISendEndpoint,
    Advanced.IAdvancedSendEndpoint
{
    /// <summary>Creates and configures the transport send context for a typed message without dispatching it.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="message">The message represented by the context.</param>
    /// <param name="pipe">The pipe that configures the send context.</param>
    /// <param name="cancellationToken">The token that cancels context creation.</param>
    /// <returns>A task that produces the configured send context.</returns>
    Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class;
}
