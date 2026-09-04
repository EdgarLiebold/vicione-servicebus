using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

/// <summary>
/// Provides a missing consume context implementation.
/// </summary>
public class MissingConsumeContext :
    ConsumeContext
{
    /// <summary>
    /// Gets the instance value.
    /// </summary>
    public static ConsumeContext Instance { get; } = new MissingConsumeContext();

    /// <summary>
    /// Determines whether the current value has payload type.
    /// </summary>
    /// <param name="payloadType">The payload type value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasPayloadType(Type payloadType)
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Attempts to get payload.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="payload">The payload value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetPayload<T>([NotNullWhen(true)] out T? payload)
        where T : class
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Gets or add payload.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="payloadFactory">The payload factory value.</param>
    /// <returns>The result of the operation.</returns>
    public T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
        where T : class
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Adds or update payload to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="addFactory">The add factory value.</param>
    /// <param name="updateFactory">The update factory value.</param>
    /// <returns>The result of the operation.</returns>
    public T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
        where T : class
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
    public CancellationToken CancellationToken => throw new ConsumeContextNotAvailableException();

    /// <summary>
    /// Gets the message id value.
    /// </summary>
    public Guid? MessageId => throw new ConsumeContextNotAvailableException();

    /// <summary>
    /// Gets the request id value.
    /// </summary>
    public Guid? RequestId => throw new ConsumeContextNotAvailableException();

    /// <summary>
    /// Gets the correlation id value.
    /// </summary>
    public Guid? CorrelationId => throw new ConsumeContextNotAvailableException();

    /// <summary>
    /// Gets the conversation id value.
    /// </summary>
    public Guid? ConversationId => throw new ConsumeContextNotAvailableException();

    /// <summary>
    /// Gets the initiator id value.
    /// </summary>
    public Guid? InitiatorId => throw new ConsumeContextNotAvailableException();

    /// <summary>
    /// Gets the expiration time value.
    /// </summary>
    public DateTimeOffset? ExpirationTime => throw new ConsumeContextNotAvailableException();

    /// <summary>
    /// Gets the source address value.
    /// </summary>
    public Uri SourceAddress => throw new ConsumeContextNotAvailableException();

    /// <summary>
    /// Gets the destination address value.
    /// </summary>
    public Uri DestinationAddress => throw new ConsumeContextNotAvailableException();

    /// <summary>
    /// Gets the response address value.
    /// </summary>
    public Uri ResponseAddress => throw new ConsumeContextNotAvailableException();

    /// <summary>
    /// Gets the fault address value.
    /// </summary>
    public Uri FaultAddress => throw new ConsumeContextNotAvailableException();

    /// <summary>
    /// Gets the sent time value.
    /// </summary>
    public DateTimeOffset? SentTime => throw new ConsumeContextNotAvailableException();

    /// <summary>
    /// Gets the headers value.
    /// </summary>
    public Headers Headers => throw new ConsumeContextNotAvailableException();

    /// <summary>
    /// Gets the host value.
    /// </summary>
    public HostInfo Host => throw new ConsumeContextNotAvailableException();

    /// <summary>
    /// Connects publish observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="publishPipe">The publish pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PublishAsync<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="publishPipe">The publish pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PublishAsync<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PublishAsync(object message, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="publishPipe">The publish pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PublishAsync(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <param name="publishPipe">The publish pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PublishAsync(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PublishAsync<T>(object values, CancellationToken cancellationToken)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="publishPipe">The publish pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PublishAsync<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="publishPipe">The publish pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PublishAsync<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Connects send observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Gets send endpoint.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.ISendEndpoint>(cancellationToken); throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Gets the receive context value.
    /// </summary>
    public ReceiveContext ReceiveContext => throw new ConsumeContextNotAvailableException();
    /// <summary>
    /// Gets the serializer context value.
    /// </summary>
    public SerializerContext SerializerContext => throw new ConsumeContextNotAvailableException();

    /// <summary>
    /// Gets the consume completed value.
    /// </summary>
    public Task ConsumeCompleted => throw new ConsumeContextNotAvailableException();

    /// <summary>
    /// Gets the supported message types value.
    /// </summary>
    public IEnumerable<string> SupportedMessageTypes => throw new ConsumeContextNotAvailableException();

    /// <summary>
    /// Determines whether the current value has message type.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasMessageType(Type messageType)
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Attempts to get message.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="consumeContext">The consume context value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetMessage<T>([NotNullWhen(true)] out ConsumeContext<T>? consumeContext)
        where T : class
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Adds consume task to the configuration.
    /// </summary>
    /// <param name="task">The task value.</param>
    public void AddConsumeTask(Task task)
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Performs the respond operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public Task RespondAsync<T>(T message)
        where T : class
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Performs the respond operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="sendPipe">The send pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public Task RespondAsync<T>(T message, IPipe<SendContext<T>> sendPipe)
        where T : class
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Performs the respond operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="sendPipe">The send pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public Task RespondAsync<T>(T message, IPipe<SendContext> sendPipe)
        where T : class
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Performs the respond operation.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public Task RespondAsync(object message)
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Performs the respond operation.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <returns>The result of the operation.</returns>
    public Task RespondAsync(object message, Type messageType)
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Performs the respond operation.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="sendPipe">The send pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public Task RespondAsync(object message, IPipe<SendContext> sendPipe)
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Performs the respond operation.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <param name="sendPipe">The send pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public Task RespondAsync(object message, Type messageType, IPipe<SendContext> sendPipe)
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Performs the respond operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <returns>The result of the operation.</returns>
    public Task RespondAsync<T>(object values)
        where T : class
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Performs the respond operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="sendPipe">The send pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public Task RespondAsync<T>(object values, IPipe<SendContext<T>> sendPipe)
        where T : class
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Performs the respond operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="sendPipe">The send pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public Task RespondAsync<T>(object values, IPipe<SendContext> sendPipe)
        where T : class
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Performs the defer response operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    public void DeferResponse<T>(T message)
        where T : class
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Performs the notify consumed operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="consumerType">The consumer type value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new ConsumeContextNotAvailableException();
    }

    /// <summary>
    /// Performs the notify faulted operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="consumerType">The consumer type value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new ConsumeContextNotAvailableException();
    }
}
