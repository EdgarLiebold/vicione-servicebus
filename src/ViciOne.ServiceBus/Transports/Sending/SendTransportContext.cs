using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Defines serialization, diagnostics, and context creation shared by send transports.</summary>
public interface SendTransportContext :
    PipeContext,
    ISendObserverConnector
{
    /// <summary>Gets the diagnostic context used for send operations.</summary>
    ILogContext LogContext { get; }

    /// <summary>Gets the destination entity name.</summary>
    string EntityName { get; }
    /// <summary>Gets the tracing activity name.</summary>
    string ActivityName { get; }
    /// <summary>Gets the normalized tracing destination.</summary>
    string ActivityDestination { get; }
    /// <summary>Gets the messaging-system identifier used by tracing.</summary>
    string ActivitySystem { get; }

    /// <summary>Gets the observers notified around physical sends.</summary>
    SendObservable SendObservers { get; }

    /// <summary>Gets the serialization registry available to the transport.</summary>
    ISerialization Serialization { get; }

    /// <summary>Creates a transport-ready send context without dispatching the message.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="message">The message represented by the context.</param>
    /// <param name="pipe">The pipe that configures the send context.</param>
    /// <param name="cancellationToken">The token that cancels context creation.</param>
    /// <returns>A task that produces the configured send context.</returns>
    Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class;
}


/// <summary>Defines provider-specific send operations over a transport pipe context.</summary>
/// <typeparam name="TContext">The provider-specific transport context type.</typeparam>
public interface SendTransportContext<TContext> :
    SendTransportContext,
    IPipeContextSource<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Gets agents whose lifecycle is owned by the send transport.</summary>
    /// <returns>The owned transport agents.</returns>
    IEnumerable<IAgent> GetAgentHandles();

    /// <summary>Creates a send context from an acquired provider context.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="context">The acquired provider context.</param>
    /// <param name="message">The message represented by the context.</param>
    /// <param name="pipe">The pipe that configures the send context.</param>
    /// <param name="cancellationToken">The token that cancels context creation.</param>
    /// <returns>A task that produces the configured send context.</returns>
    Task<SendContext<T>> CreateSendContextAsync<T>(TContext context, T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class;

    /// <summary>Dispatches a configured message through an acquired provider context.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="transportContext">The acquired provider context.</param>
    /// <param name="sendContext">The configured send context.</param>
    /// <param name="cancellationToken">The token that cancels transport dispatch.</param>
    /// <returns>A task that completes when the provider accepts the message.</returns>
    Task SendAsync<T>(TContext transportContext, SendContext<T> sendContext, CancellationToken cancellationToken = default)
        where T : class;
}
