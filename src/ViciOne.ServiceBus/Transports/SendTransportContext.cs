using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Exposes state for send transport operations.</summary>
public interface SendTransportContext :
    PipeContext,
    ISendObserverConnector
{
    /// <summary>The LogContext used for sending transport messages, to ensure proper activity filtering.</summary>
    ILogContext LogContext { get; }

    /// <summary>Gets the entity name.</summary>
    string EntityName { get; }
    /// <summary>Gets the activity name.</summary>
    string ActivityName { get; }
    /// <summary>Gets the activity destination.</summary>
    string ActivityDestination { get; }
    /// <summary>Gets the activity system.</summary>
    string ActivitySystem { get; }

    /// <summary>Gets the send observers.</summary>
    SendObservable SendObservers { get; }

    /// <summary>Gets the serialization.</summary>
    ISerialization Serialization { get; }

    /// <summary>Create the send context without the presence of a transport, but in a way that it can be used by the transport.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class;
}


/// <summary>Exposes state for send transport operations.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface SendTransportContext<TContext> :
    SendTransportContext,
    IPipeContextSource<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Gets agent handles.</summary>
    /// <returns>The agent handles.</returns>
    IEnumerable<IAgent> GetAgentHandles();

    /// <summary>Create the send.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The send transport context, which may be used to create the underlying send context.</param>
    /// <param name="message">The message being sent.</param>
    /// <param name="pipe">The developer supplied pipe to configure the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    Task<SendContext<T>> CreateSendContextAsync<T>(TContext context, T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class;

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="transportContext">The transport context.</param>
    /// <param name="sendContext">The send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendAsync<T>(TContext transportContext, SendContext<T> sendContext, CancellationToken cancellationToken = default)
        where T : class;
}
