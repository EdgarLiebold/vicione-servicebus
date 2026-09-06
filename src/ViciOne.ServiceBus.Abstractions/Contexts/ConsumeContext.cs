using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>Exposes state for consume operations.</summary>
public interface ConsumeContext :
    PipeContext,
    MessageContext,
    IPublishEndpoint,
    Advanced.IAdvancedPublishEndpoint,
    ISendEndpointProvider
{
    /// <summary>The received message context.</summary>
    ReceiveContext ReceiveContext { get; }

    /// <summary>The serializer context from message deserialization.</summary>
    SerializerContext SerializerContext { get; }

    /// <summary>An awaitable task that is completed once the consume context is completed.</summary>
    Task ConsumeCompleted { get; }

    /// <summary>Gets the outgoing.</summary>
    IOutgoingMessages Outgoing { get; }

    /// <summary>Returns the supported message types from the message.</summary>
    IEnumerable<string> SupportedMessageTypes { get; }

    /// <summary>Returns true if the specified message type is contained in the serialized message.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool HasMessageType(Type messageType);

    /// <summary>Returns the specified message type if available, otherwise returns false.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="consumeContext">Receives the consume context produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetMessage<T>([NotNullWhen(true)] out ConsumeContext<T>? consumeContext)
        where T : class;

    /// <summary>Add a task that must complete before the consume is completed.</summary>
    /// <param name="task">The task.</param>
    void AddConsumeTask(Task task);

    /// <summary>
    /// Responds to the current message immediately, returning the Task for the
    /// sending message. The caller may choose to await the response to ensure it was sent, or
    /// allow the framework to wait for it (which will happen automatically before the message is acknowledged).
    /// </summary>
    /// <typeparam name="T">The type of the message to respond with.</typeparam>
    /// <param name="message">The message to send in response.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RespondAsync<T>(T message)
        where T : class;

    /// <summary>Responds to the current message with application-level send options.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RespondAsync<T>(T message, SendOptions options)
        where T : class;

    /// <summary>
    /// Responds to the current message immediately, returning the Task for the
    /// sending message. The caller may choose to await the response to ensure it was sent, or
    /// allow the framework to wait for it (which will happen automatically before the message is acknowledged).
    /// </summary>
    /// <typeparam name="T">The type of the message to respond with.</typeparam>
    /// <param name="message">The message to send in response.</param>
    /// <param name="sendPipe">The pipe used to customize the response send context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RespondAsync<T>(T message, IPipe<SendContext<T>> sendPipe)
        where T : class;

    /// <summary>
    /// Responds to the current message immediately, returning the Task for the
    /// sending message. The caller may choose to await the response to ensure it was sent, or
    /// allow the framework to wait for it (which will happen automatically before the message is acknowledged).
    /// </summary>
    /// <typeparam name="T">The type of the message to respond with.</typeparam>
    /// <param name="message">The message to send in response.</param>
    /// <param name="sendPipe">The pipe used to customize the response send context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RespondAsync<T>(T message, IPipe<SendContext> sendPipe)
        where T : class;

    /// <summary>
    /// Responds to the current message immediately, returning the Task for the
    /// sending message. The caller may choose to await the response to ensure it was sent, or
    /// allow the framework to wait for it (which will happen automatically before the message is acknowledged).
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RespondAsync(object message);

    /// <summary>
    /// Responds to the current message immediately, returning the Task for the
    /// sending message. The caller may choose to await the response to ensure it was sent, or
    /// allow the framework to wait for it (which will happen automatically before the message is acknowledged).
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="messageType">The message type to send.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RespondAsync(object message, Type messageType);

    /// <summary>
    /// Responds to the current message immediately, returning the Task for the
    /// sending message. The caller may choose to await the response to ensure it was sent, or
    /// allow the framework to wait for it (which will happen automatically before the message is acknowledged).
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="sendPipe">The send pipe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RespondAsync(object message, IPipe<SendContext> sendPipe);

    /// <summary>
    /// Responds to the current message immediately, returning the Task for the
    /// sending message. The caller may choose to await the response to ensure it was sent, or
    /// allow the framework to wait for it (which will happen automatically before the message is acknowledged).
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="messageType">The message type to send.</param>
    /// <param name="sendPipe">The send pipe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RespondAsync(object message, Type messageType, IPipe<SendContext> sendPipe);

    /// <summary>
    /// Responds to the current message immediately, returning the Task for the
    /// sending message. The caller may choose to await the response to ensure it was sent, or
    /// allow the framework to wait for it (which will happen automatically before the message is acknowledged).
    /// </summary>
    /// <typeparam name="T">The type of the message to respond with.</typeparam>
    /// <param name="values">The values for the message properties.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RespondAsync<T>(object values)
        where T : class;

    /// <summary>
    /// Responds to the current message immediately, returning the Task for the
    /// sending message. The caller may choose to await the response to ensure it was sent, or
    /// allow the framework to wait for it (which will happen automatically before the message is acknowledged).
    /// </summary>
    /// <typeparam name="T">The type of the message to respond with.</typeparam>
    /// <param name="values">The values for the message properties.</param>
    /// <param name="sendPipe">The send pipe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RespondAsync<T>(object values, IPipe<SendContext<T>> sendPipe)
        where T : class;

    /// <summary>
    /// Responds to the current message immediately, returning the Task for the
    /// sending message. The caller may choose to await the response to ensure it was sent, or
    /// allow the framework to wait for it (which will happen automatically before the message is acknowledged).
    /// </summary>
    /// <typeparam name="T">The type of the message to respond with.</typeparam>
    /// <param name="values">The values for the message properties.</param>
    /// <param name="sendPipe">The send pipe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RespondAsync<T>(object values, IPipe<SendContext> sendPipe)
        where T : class;

    /// <summary>
    /// Adds a response to the message being consumed, which will be sent once the consumer
    /// has completed. The message is not acknowledged until the response is acknowledged.
    /// </summary>
    /// <typeparam name="T">The type of the message to respond with.</typeparam>
    /// <param name="message">The message to send in response.</param>
    void DeferResponse<T>(T message)
        where T : class;

    /// <summary>Notify that the message has been consumed -- note that this is internal, and should not be called by a consumer.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The consumer type.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Notify that a message consumer has faulted -- note that this is internal, and should not be called by a consumer.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The message consumer type.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
        where T : class;
}


/// <summary>Exposes state for consume operations.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface ConsumeContext<out T> :
    PipeContext,
    MessageContext
    where T : class
{
    /// <summary>Gets the message.</summary>
    T Message { get; }

    /// <summary>Gets the outgoing.</summary>
    IOutgoingMessages Outgoing { get; }

    /// <summary>Responds to the consumed message.</summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="response">The response.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RespondAsync<TResponse>(TResponse response)
        where TResponse : class;

    /// <summary>Responds to the consumed message with application-level send options.</summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="response">The response.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RespondAsync<TResponse>(TResponse response, SendOptions options)
        where TResponse : class;

    /// <summary>Defers a response until the consumer has completed successfully.</summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="response">The response.</param>
    void DeferResponse<TResponse>(TResponse response)
        where TResponse : class;

    /// <summary>Tries to expose another supported message type from the same envelope.</summary>
    /// <typeparam name="TOther">The other type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetMessage<TOther>([NotNullWhen(true)] out ConsumeContext<TOther>? context)
        where TOther : class;
}
