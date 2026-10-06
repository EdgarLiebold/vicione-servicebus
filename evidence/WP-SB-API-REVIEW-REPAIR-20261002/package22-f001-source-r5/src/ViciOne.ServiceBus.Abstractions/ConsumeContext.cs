using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus;

/// <summary>Provides message, transport, response, and completion operations for a received message.</summary>
public interface ConsumeContext :
    PipeContext,
    MessageContext,
    IPublishEndpoint,
    Advanced.IAdvancedPublishEndpoint,
    ISendEndpointProvider
{
    /// <summary>Gets the underlying transport receive context.</summary>
    ReceiveContext ReceiveContext { get; }

    /// <summary>Gets the serializer context that decoded the message.</summary>
    SerializerContext SerializerContext { get; }

    /// <summary>Gets a task that completes after all registered consume work completes.</summary>
    Task ConsumeCompleted { get; }

    /// <summary>Gets application-level outgoing operations bound to this consume scope.</summary>
    IOutgoingMessages Outgoing { get; }

    /// <summary>Gets the message contract identifiers declared by the serialized envelope.</summary>
    IEnumerable<string> SupportedMessageTypes { get; }

    /// <summary>Determines whether the serialized envelope supports a runtime message contract.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns><see langword="true" /> when the message contract is supported; otherwise, <see langword="false" />.</returns>
    bool HasMessageType(Type messageType);

    /// <summary>Tries to expose the message as a supported contract type.</summary>
    /// <typeparam name="TMessage">The requested message contract type.</typeparam>
    /// <param name="consumeContext">The typed consume context when the contract is supported.</param>
    /// <returns><see langword="true" /> when the typed context is available; otherwise, <see langword="false" />.</returns>
    bool TryGetMessage<TMessage>([NotNullWhen(true)] out ConsumeContext<TMessage>? consumeContext)
        where TMessage : class;

    /// <summary>Registers work that must finish before consumption is complete.</summary>
    /// <param name="task">The pending consume task.</param>
    void AddConsumeTask(Task task);

    /// <summary>Sends a typed response and tracks it as part of consume completion.</summary>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="message">The message to send in response.</param>
    /// <returns>A task that represents the response send.</returns>
    Task RespondAsync<TResponse>(TResponse message)
        where TResponse : class;

    /// <summary>Responds to the current message with application-level send options.</summary>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="message">The message to send in response.</param>
    /// <param name="options">The application-level send options.</param>
    /// <returns>A task that represents the response send.</returns>
    Task RespondAsync<TResponse>(TResponse message, SendOptions options)
        where TResponse : class;

    /// <summary>Sends a typed response through a typed send-context pipe.</summary>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="message">The message to send in response.</param>
    /// <param name="sendPipe">The pipe that configures the typed response send context.</param>
    /// <returns>A task that represents the response send.</returns>
    Task RespondAsync<TResponse>(TResponse message, IPipe<SendContext<TResponse>> sendPipe)
        where TResponse : class;

    /// <summary>Sends a typed response through an untyped send-context pipe.</summary>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="message">The message to send in response.</param>
    /// <param name="sendPipe">The pipe that configures the response send context.</param>
    /// <returns>A task that represents the response send.</returns>
    Task RespondAsync<TResponse>(TResponse message, IPipe<SendContext> sendPipe)
        where TResponse : class;

    /// <summary>Sends a response using the message's runtime type.</summary>
    /// <param name="message">The response message.</param>
    /// <returns>A task that represents the response send.</returns>
    Task RespondAsync(object message);

    /// <summary>Sends a response as an explicit runtime message contract.</summary>
    /// <param name="message">The response message.</param>
    /// <param name="messageType">The runtime response contract type.</param>
    /// <returns>A task that represents the response send.</returns>
    Task RespondAsync(object message, Type messageType);

    /// <summary>Sends a runtime-typed response through a send-context pipe.</summary>
    /// <param name="message">The response message.</param>
    /// <param name="sendPipe">The pipe that configures the response send context.</param>
    /// <returns>A task that represents the response send.</returns>
    Task RespondAsync(object message, IPipe<SendContext> sendPipe);

    /// <summary>Sends a response as an explicit runtime contract through a send-context pipe.</summary>
    /// <param name="message">The response message.</param>
    /// <param name="messageType">The runtime response contract type.</param>
    /// <param name="sendPipe">The pipe that configures the response send context.</param>
    /// <returns>A task that represents the response send.</returns>
    Task RespondAsync(object message, Type messageType, IPipe<SendContext> sendPipe);

    /// <summary>Initializes and sends a typed response from a property-value source.</summary>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="values">The source values used to initialize the response.</param>
    /// <returns>A task that represents response initialization and sending.</returns>
    Task RespondAsync<TResponse>(object values)
        where TResponse : class;

    /// <summary>Initializes and sends a typed response through a typed send-context pipe.</summary>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="values">The source values used to initialize the response.</param>
    /// <param name="sendPipe">The pipe that configures the typed response send context.</param>
    /// <returns>A task that represents response initialization and sending.</returns>
    Task RespondAsync<TResponse>(object values, IPipe<SendContext<TResponse>> sendPipe)
        where TResponse : class;

    /// <summary>Initializes and sends a typed response through an untyped send-context pipe.</summary>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="values">The source values used to initialize the response.</param>
    /// <param name="sendPipe">The pipe that configures the response send context.</param>
    /// <returns>A task that represents response initialization and sending.</returns>
    Task RespondAsync<TResponse>(object values, IPipe<SendContext> sendPipe)
        where TResponse : class;

    /// <summary>
    /// Starts the configured response operation and attaches it to consume completion without awaiting it at the call site.
    /// </summary>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="message">The message to send in response.</param>
    /// <remarks>Successful-consumer release depends on an explicitly configured outbox. Without that policy, this call does not defer dispatch until consumer success; completion does not confirm delivery or consumption.</remarks>
    void DeferResponse<TResponse>(TResponse message)
        where TResponse : class;

    /// <summary>Records a successful consumer delivery with the receive pipeline.</summary>
    /// <typeparam name="TMessage">The consumed message type.</typeparam>
    /// <param name="context">The typed context that completed successfully.</param>
    /// <param name="duration">The elapsed consumer execution time.</param>
    /// <param name="consumerType">The consumer type name.</param>
    /// <param name="cancellationToken">Cancels notification before the receive pipeline is invoked.</param>
    /// <returns>A task that represents the notification operation.</returns>
    /// <remarks>Required arguments are validated before cancellation. Once notification starts, observer completion remains part of the returned operation.</remarks>
    Task NotifyConsumedAsync<TMessage>(ConsumeContext<TMessage> context, TimeSpan duration, string consumerType,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Records a consumer delivery fault with the receive pipeline.</summary>
    /// <typeparam name="TMessage">The consumed message type.</typeparam>
    /// <param name="context">The typed context whose consumer faulted.</param>
    /// <param name="duration">The elapsed consumer execution time.</param>
    /// <param name="consumerType">The consumer type name.</param>
    /// <param name="exception">The consumer exception.</param>
    /// <param name="cancellationToken">Cancels before fault generation starts and is forwarded to the receive notification after any required generation completes.</param>
    /// <returns>A task that represents any required fault generation and the receive notification.</returns>
    /// <remarks>Required arguments are validated before cancellation. Started fault generation follows the delivery cancellation policy and is awaited before receive notification; later caller cancellation does not replace its failure.</remarks>
    Task NotifyFaultedAsync<TMessage>(ConsumeContext<TMessage> context, TimeSpan duration, string consumerType,
        Exception exception, CancellationToken cancellationToken = default)
        where TMessage : class;
}


/// <summary>Provides the typed message and application operations for a consumer invocation.</summary>
/// <typeparam name="TMessage">The consumed message type.</typeparam>
public interface ConsumeContext<out TMessage> :
    PipeContext,
    MessageContext
    where TMessage : class
{
    /// <summary>Gets the consumed message.</summary>
    TMessage Message { get; }

    /// <summary>Gets application-level outgoing operations bound to this consume scope.</summary>
    IOutgoingMessages Outgoing { get; }

    /// <summary>Responds to the consumed message.</summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="response">The response message.</param>
    /// <returns>A task that represents the response send.</returns>
    Task RespondAsync<TResponse>(TResponse response)
        where TResponse : class;

    /// <summary>Responds to the consumed message with application-level send options.</summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="response">The response message.</param>
    /// <param name="options">The application-level send options.</param>
    /// <returns>A task that represents the response send.</returns>
    Task RespondAsync<TResponse>(TResponse response, SendOptions options)
        where TResponse : class;

    /// <summary>Starts the configured response operation and attaches it to consume completion without awaiting it at the call site.</summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="response">The response message.</param>
    /// <remarks>Successful-consumer release depends on an explicitly configured outbox. Without that policy, this call does not defer dispatch until consumer success; completion does not confirm delivery or consumption.</remarks>
    void DeferResponse<TResponse>(TResponse response)
        where TResponse : class;

    /// <summary>Tries to expose another supported message type from the same envelope.</summary>
    /// <typeparam name="TOtherMessage">The requested message contract type.</typeparam>
    /// <param name="context">The typed context when the contract is supported.</param>
    /// <returns><see langword="true" /> when the typed context is available; otherwise, <see langword="false" />.</returns>
    bool TryGetMessage<TOtherMessage>([NotNullWhen(true)] out ConsumeContext<TOtherMessage>? context)
        where TOtherMessage : class;
}
