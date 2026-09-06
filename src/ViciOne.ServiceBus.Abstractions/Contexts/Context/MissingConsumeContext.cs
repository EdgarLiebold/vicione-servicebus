using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

/// <summary>Carries state for missing consume operations.</summary>
public class MissingConsumeContext :
    ConsumeContext
{
    /// <summary>Gets the instance.</summary>
    public static ConsumeContext Instance { get; } = new MissingConsumeContext();

    /// <summary>Determines whether the current value has payload type.</summary>
    /// <param name="payloadType">The runtime payload type used by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasPayloadType(Type payloadType)
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Attempts to get payload.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="payload">Receives the payload produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetPayload<T>([NotNullWhen(true)] out T? payload)
        where T : class
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Gets or add payload.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="payloadFactory">The payload factory.</param>
    /// <returns>The or add payload.</returns>
    public T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
        where T : class
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Adds or update payload to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="addFactory">The add factory.</param>
    /// <param name="updateFactory">The update factory.</param>
    /// <returns>The t produced by the operation.</returns>
    public T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
        where T : class
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Gets the cancellation token.</summary>
    public CancellationToken CancellationToken => throw new ConsumeContextNotAvailableException();

    /// <summary>Gets the message id.</summary>
    public Guid? MessageId => throw new ConsumeContextNotAvailableException();

    /// <summary>Gets the request id.</summary>
    public Guid? RequestId => throw new ConsumeContextNotAvailableException();

    /// <summary>Gets the correlation id.</summary>
    public Guid? CorrelationId => throw new ConsumeContextNotAvailableException();

    /// <summary>Gets the conversation id.</summary>
    public Guid? ConversationId => throw new ConsumeContextNotAvailableException();

    /// <summary>Gets the initiator id.</summary>
    public Guid? InitiatorId => throw new ConsumeContextNotAvailableException();

    /// <summary>Gets the expiration time.</summary>
    public DateTimeOffset? ExpirationTime => throw new ConsumeContextNotAvailableException();

    /// <summary>Gets the source address.</summary>
    public Uri SourceAddress => throw new ConsumeContextNotAvailableException();

    /// <summary>Gets the destination address.</summary>
    public Uri DestinationAddress => throw new ConsumeContextNotAvailableException();

    /// <summary>Gets the response address.</summary>
    public Uri ResponseAddress => throw new ConsumeContextNotAvailableException();

    /// <summary>Gets the fault address.</summary>
    public Uri FaultAddress => throw new ConsumeContextNotAvailableException();

    /// <summary>Gets the sent time.</summary>
    public DateTimeOffset? SentTime => throw new ConsumeContextNotAvailableException();

    /// <summary>Gets the headers.</summary>
    public Headers Headers => throw new ConsumeContextNotAvailableException();

    /// <summary>Gets the host.</summary>
    public HostInfo Host => throw new ConsumeContextNotAvailableException();

    /// <summary>Connects publish observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync(object message, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync<T>(object values, CancellationToken cancellationToken)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Connects send observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Gets send endpoint.</summary>
    /// <param name="address">The address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.ISendEndpoint>(cancellationToken); throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Gets the receive context.</summary>
    public ReceiveContext ReceiveContext => throw new ConsumeContextNotAvailableException();
    /// <summary>Gets the serializer context.</summary>
    public SerializerContext SerializerContext => throw new ConsumeContextNotAvailableException();

    /// <summary>Gets the consume completed.</summary>
    public Task ConsumeCompleted => throw new ConsumeContextNotAvailableException();

    /// <summary>Gets the outgoing.</summary>
    public IOutgoingMessages Outgoing => throw new ConsumeContextNotAvailableException();

    /// <summary>Gets the supported message types.</summary>
    public IEnumerable<string> SupportedMessageTypes => throw new ConsumeContextNotAvailableException();

    /// <summary>Determines whether the current value has message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasMessageType(Type messageType)
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Attempts to get message.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="consumeContext">Receives the consume context produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetMessage<T>([NotNullWhen(true)] out ConsumeContext<T>? consumeContext)
        where T : class
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Adds consume task to the configuration.</summary>
    /// <param name="task">The task.</param>
    public void AddConsumeTask(Task task)
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RespondAsync<T>(T message)
        where T : class
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Reports that application-level response options cannot be used without a consume context.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>This member always throws.</returns>
    public Task RespondAsync<T>(T message, SendOptions options)
        where T : class
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="sendPipe">The send pipe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RespondAsync<T>(T message, IPipe<SendContext<T>> sendPipe)
        where T : class
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="sendPipe">The send pipe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RespondAsync<T>(T message, IPipe<SendContext> sendPipe)
        where T : class
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Sends the configured response.</summary>
    /// <param name="message">The message to process.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RespondAsync(object message)
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Sends the configured response.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RespondAsync(object message, Type messageType)
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Sends the configured response.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="sendPipe">The send pipe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RespondAsync(object message, IPipe<SendContext> sendPipe)
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Sends the configured response.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="sendPipe">The send pipe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RespondAsync(object message, Type messageType, IPipe<SendContext> sendPipe)
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RespondAsync<T>(object values)
        where T : class
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="sendPipe">The send pipe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RespondAsync<T>(object values, IPipe<SendContext<T>> sendPipe)
        where T : class
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="sendPipe">The send pipe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RespondAsync<T>(object values, IPipe<SendContext> sendPipe)
        where T : class
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Defers response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    public void DeferResponse<T>(T message)
        where T : class
    {
        throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Reports that notify has been consumed.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new ConsumeContextNotAvailableException();
    }

    /// <summary>Reports that notify has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new ConsumeContextNotAvailableException();
    }
}
