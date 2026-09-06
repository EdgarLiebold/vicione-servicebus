using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Defines the contract for send transport context.
/// </summary>
public interface SendTransportContext :
    PipeContext,
    ISendObserverConnector
{
    /// <summary>
    /// The LogContext used for sending transport messages, to ensure proper activity filtering
    /// </summary>
    ILogContext LogContext { get; }

    /// <summary>
    /// Gets the entity name value.
    /// </summary>
    string EntityName { get; }
    /// <summary>
    /// Gets the activity name value.
    /// </summary>
    string ActivityName { get; }
    /// <summary>
    /// Gets the activity destination value.
    /// </summary>
    string ActivityDestination { get; }
    /// <summary>
    /// Gets the activity system value.
    /// </summary>
    string ActivitySystem { get; }

    /// <summary>
    /// Gets the send observers value.
    /// </summary>
    SendObservable SendObservers { get; }

    /// <summary>
    /// Gets the serialization value.
    /// </summary>
    ISerialization Serialization { get; }

    /// <summary>
    /// Create the send context without the presence of a transport, but in a way that it can be used by the transport
    /// </summary>
    /// <param name="message"></param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class;
}


/// <summary>
/// Defines the contract for send transport context.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public interface SendTransportContext<TContext> :
    SendTransportContext,
    IPipeContextSource<TContext>
    where TContext : class, PipeContext
{
    /// <summary>
    /// Gets agent handles.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    IEnumerable<IAgent> GetAgentHandles();

    /// <summary>
    /// Create the send
    /// </summary>
    /// <param name="context">The send transport context, which may be used to create the underlying send context</param>
    /// <param name="message">The message being sent</param>
    /// <param name="pipe">The developer supplied pipe to configure the send context</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T">The message type</typeparam>
    /// <returns></returns>
    Task<SendContext<T>> CreateSendContextAsync<T>(TContext context, T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class;

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="transportContext">The transport context value.</param>
    /// <param name="sendContext">The send context value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task SendAsync<T>(TContext transportContext, SendContext<T> sendContext, CancellationToken cancellationToken = default)
        where T : class;
}
