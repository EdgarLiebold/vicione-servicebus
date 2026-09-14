using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals.Dispatching;
using ViciOne.ServiceBus.Internals.Outgoing;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Context;

/// <summary>Provides shared message, payload, response, publish, and observer behavior for consume contexts.</summary>
public abstract class BaseConsumeContext :
    PublishEndpoint,
    ConsumeContext
{
    readonly IOutgoingMessages _outgoing;

    /// <summary>Initializes a consume context from the transport receive and deserialization contexts.</summary>
    /// <param name="receiveContext">The transport context that owns delivery state and endpoint providers.</param>
    /// <param name="serializerContext">The context that reads the received message body.</param>
    protected BaseConsumeContext(ReceiveContext receiveContext, SerializerContext serializerContext)
        : base((receiveContext ?? throw new ArgumentNullException(nameof(receiveContext))).PublishEndpointProvider)
    {
        ReceiveContext = receiveContext;
        SerializerContext = serializerContext ?? throw new ArgumentNullException(nameof(serializerContext));
        _outgoing = new ConsumeContextOutgoingMessages(this);
    }

    /// <summary>Gets the token that is canceled when the received delivery is no longer active.</summary>
    public virtual CancellationToken CancellationToken => ReceiveContext.CancellationToken;

    /// <summary>Determines whether a payload assignable to the specified type is available.</summary>
    /// <param name="payloadType">The payload type to locate.</param>
    /// <returns><see langword="true" /> when a matching payload is available; otherwise, <see langword="false" />.</returns>
    public abstract bool HasPayloadType(Type payloadType);

    /// <summary>Attempts to retrieve a typed payload.</summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="payload">Receives the matching payload when one is available.</param>
    /// <returns><see langword="true" /> when a matching payload is returned; otherwise, <see langword="false" />.</returns>
    public abstract bool TryGetPayload<T>([NotNullWhen(true)] out T? payload)
        where T : class;

    /// <summary>Returns the existing typed payload or atomically adds one from the supplied factory.</summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="payloadFactory">The factory invoked only when no matching payload exists.</param>
    /// <returns>The existing or newly added payload.</returns>
    public abstract T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
        where T : class;

    /// <summary>Adds a payload when absent or replaces the existing payload through an update factory.</summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="addFactory">The factory used when no matching payload exists.</param>
    /// <param name="updateFactory">The factory that produces a replacement from the existing payload.</param>
    /// <returns>The added or updated payload.</returns>
    public abstract T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
        where T : class;

    /// <summary>Gets the transport receive context associated with this delivery.</summary>
    public ReceiveContext ReceiveContext { get; protected set; }

    /// <summary>Gets the serializer context used to materialize message contracts.</summary>
    public SerializerContext SerializerContext { get; }

    /// <summary>Gets a task that completes after all work attached to this delivery completes.</summary>
    public abstract Task ConsumeCompleted { get; }

    /// <summary>Gets the outgoing-message API bound to this consume context.</summary>
    public IOutgoingMessages Outgoing => _outgoing;

    /// <summary>Gets the identifier of the received message.</summary>
    public abstract Guid? MessageId { get; }
    /// <summary>Gets the request identifier carried by the received message.</summary>
    public abstract Guid? RequestId { get; }
    /// <summary>Gets the correlation identifier carried by the received message.</summary>
    public abstract Guid? CorrelationId { get; }
    /// <summary>Gets the conversation identifier carried by the received message.</summary>
    public abstract Guid? ConversationId { get; }
    /// <summary>Gets the identifier of the message that initiated this conversation.</summary>
    public abstract Guid? InitiatorId { get; }
    /// <summary>Gets the instant after which the received message is expired.</summary>
    public abstract DateTimeOffset? ExpirationTime { get; }
    /// <summary>Gets the address from which the message was sent.</summary>
    public abstract Uri? SourceAddress { get; }
    /// <summary>Gets the receive endpoint address to which the message was delivered.</summary>
    public abstract Uri? DestinationAddress { get; }
    /// <summary>Gets the address to which a response should be sent.</summary>
    public abstract Uri? ResponseAddress { get; }
    /// <summary>Gets the address to which a fault should be sent.</summary>
    public abstract Uri? FaultAddress { get; }
    /// <summary>Gets the instant at which the message was sent.</summary>
    public abstract DateTimeOffset? SentTime { get; }
    /// <summary>Gets the headers carried by the received message.</summary>
    public abstract Headers Headers { get; }
    /// <summary>Gets information about the host that sent the message.</summary>
    public abstract HostInfo Host { get; }
    /// <summary>Gets the serialized contract identifiers supported by the message body.</summary>
    public abstract IEnumerable<string> SupportedMessageTypes { get; }
    /// <summary>Determines whether the received body can be represented as a contract type.</summary>
    /// <param name="messageType">The contract type to inspect.</param>
    /// <returns><see langword="true" /> when the body supports the contract; otherwise, <see langword="false" />.</returns>
    public abstract bool HasMessageType(Type messageType);

    /// <summary>Attempts to materialize the received body as a typed consume context.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="consumeContext">Receives the typed consume context when the contract is supported.</param>
    /// <returns><see langword="true" /> when a typed context is returned; otherwise, <see langword="false" />.</returns>
    public abstract bool TryGetMessage<T>([NotNullWhen(true)] out ConsumeContext<T>? consumeContext)
        where T : class;

    /// <summary>Sends a typed response to the response address of the received message.</summary>
    /// <typeparam name="T">The response contract type.</typeparam>
    /// <param name="message">The response message.</param>
    /// <returns>A task that completes when the response is accepted by the transport.</returns>
    public virtual Task RespondAsync<T>(T message)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);

        return ConsumeTaskAsync(RespondInternalAsync(message));
    }

    /// <summary>Responds to the consumed message with application-level send options.</summary>
    /// <typeparam name="T">The response contract type.</typeparam>
    /// <param name="message">The response message.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A task that completes when the response is accepted by the transport.</returns>
    public virtual Task RespondAsync<T>(T message, SendOptions options)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(options);

        return RespondAsync(message, new SendOptionsPipe<T>(options));
    }

    /// <summary>Sends a typed response after applying a typed send-context pipe.</summary>
    /// <typeparam name="T">The response contract type.</typeparam>
    /// <param name="message">The response message.</param>
    /// <param name="sendPipe">The typed pipe that customizes the response send context.</param>
    /// <returns>A task that completes when the response is accepted by the transport.</returns>
    public virtual Task RespondAsync<T>(T message, IPipe<SendContext<T>> sendPipe)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(sendPipe);

        return ConsumeTaskAsync(RespondInternalAsync(message, sendPipe));
    }

    /// <summary>Sends a typed response after applying an untyped send-context pipe.</summary>
    /// <typeparam name="T">The response contract type.</typeparam>
    /// <param name="message">The response message.</param>
    /// <param name="sendPipe">The untyped pipe that customizes the response send context.</param>
    /// <returns>A task that completes when the response is accepted by the transport.</returns>
    public virtual Task RespondAsync<T>(T message, IPipe<SendContext> sendPipe)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(sendPipe);

        return ConsumeTaskAsync(RespondInternalAsync(message, sendPipe));
    }

    /// <summary>Sends a response using the runtime type of the supplied message as its contract.</summary>
    /// <param name="message">The response message.</param>
    /// <returns>A task that completes when the response is accepted by the transport.</returns>
    public virtual Task RespondAsync(object message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var messageType = message.GetType();

        return ResponseEndpointDispatcher.RespondAsync(this, message, messageType);
    }

    /// <summary>Sends a response using an explicit runtime contract type.</summary>
    /// <param name="message">The response message.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns>A task that completes when the response is accepted by the transport.</returns>
    public virtual Task RespondAsync(object message, Type messageType)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);

        return ResponseEndpointDispatcher.RespondAsync(this, message, messageType);
    }

    /// <summary>Sends a runtime-typed response after applying a send-context pipe.</summary>
    /// <param name="message">The response message.</param>
    /// <param name="sendPipe">The pipe that customizes the response send context.</param>
    /// <returns>A task that completes when the response is accepted by the transport.</returns>
    public virtual Task RespondAsync(object message, IPipe<SendContext> sendPipe)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(sendPipe);

        var messageType = message.GetType();

        return ResponseEndpointDispatcher.RespondAsync(this, message, messageType, sendPipe);
    }

    /// <summary>Sends a response using an explicit runtime contract type and send-context pipe.</summary>
    /// <param name="message">The response message.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="sendPipe">The pipe that customizes the response send context.</param>
    /// <returns>A task that completes when the response is accepted by the transport.</returns>
    public virtual Task RespondAsync(object message, Type messageType, IPipe<SendContext> sendPipe)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(sendPipe);

        return ResponseEndpointDispatcher.RespondAsync(this, message, messageType, sendPipe);
    }

    /// <summary>Initializes and sends a response contract from an anonymous values object.</summary>
    /// <typeparam name="T">The response contract to initialize.</typeparam>
    /// <param name="values">The values used to initialize the response contract.</param>
    /// <returns>A task that completes when the response is accepted by the transport.</returns>
    public virtual Task RespondAsync<T>(object values)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);

        return ConsumeTaskAsync(RespondInternalAsync<T>(values));
    }

    /// <summary>Initializes and sends a response contract after applying a typed send-context pipe.</summary>
    /// <typeparam name="T">The response contract to initialize.</typeparam>
    /// <param name="values">The values used to initialize the response contract.</param>
    /// <param name="sendPipe">The typed pipe that customizes the response send context.</param>
    /// <returns>A task that completes when the response is accepted by the transport.</returns>
    public virtual Task RespondAsync<T>(object values, IPipe<SendContext<T>> sendPipe)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(sendPipe);
        return ConsumeTaskAsync(RespondInternalAsync(values, sendPipe));
    }

    /// <summary>Initializes and sends a response contract after applying an untyped send-context pipe.</summary>
    /// <typeparam name="T">The response contract to initialize.</typeparam>
    /// <param name="values">The values used to initialize the response contract.</param>
    /// <param name="sendPipe">The untyped pipe that customizes the response send context.</param>
    /// <returns>A task that completes when the response is accepted by the transport.</returns>
    public virtual Task RespondAsync<T>(object values, IPipe<SendContext> sendPipe)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(sendPipe);
        return ConsumeTaskAsync(RespondInternalAsync<T>(values, sendPipe));
    }

    /// <summary>Attaches a response operation to consume completion without awaiting it at the call site.</summary>
    /// <typeparam name="T">The response contract type.</typeparam>
    /// <param name="message">The response message.</param>
    public virtual void DeferResponse<T>(T message)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        AddConsumeTask(RespondInternalAsync(message));
    }

    /// <summary>Resolves a send endpoint decorated with the current consume-context metadata.</summary>
    /// <param name="address">The destination address to resolve.</param>
    /// <param name="cancellationToken">The token that cancels endpoint resolution.</param>
    /// <returns>A task containing the consume-context-aware send endpoint.</returns>
    public virtual async Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);

        Task<ISendEndpoint> resolution = ReceiveContext.SendEndpointProvider.GetSendEndpointAsync(
                address,
                cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException(
                $"The receive context send endpoint provider returned no resolution task for '{address}'.");
        ISendEndpoint sendEndpoint = await resolution.ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"The receive context send endpoint provider resolved no send endpoint for '{address}'.");

        return new ConsumeSendEndpoint(sendEndpoint, this);
    }

    /// <summary>Reports a successful consumer invocation to the receive pipeline.</summary>
    /// <typeparam name="T">The consumed message contract type.</typeparam>
    /// <param name="context">The typed consume context that completed.</param>
    /// <param name="duration">The elapsed consumer execution time.</param>
    /// <param name="consumerType">The diagnostic name of the consumer implementation.</param>
    /// <param name="cancellationToken">The token that cancels observer notification.</param>
    /// <returns>A task that completes when all receive observers have been notified.</returns>
    public virtual Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerType);
        return ReceiveContext.NotifyConsumedAsync(context, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>Generates a message fault when appropriate and reports a failed consumer invocation to the receive pipeline.</summary>
    /// <typeparam name="T">The consumed message contract type.</typeparam>
    /// <param name="context">The typed consume context whose consumer failed.</param>
    /// <param name="duration">The elapsed consumer execution time.</param>
    /// <param name="consumerType">The diagnostic name of the consumer implementation.</param>
    /// <param name="exception">The consumer failure.</param>
    /// <param name="cancellationToken">The token that cancels observer notification.</param>
    /// <returns>A task that completes when fault generation and observer notification finish.</returns>
    public virtual async Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerType);
        ArgumentNullException.ThrowIfNull(exception);

        switch (exception)
        {
            case OperationCanceledException canceled when canceled.CancellationToken == context.CancellationToken:
                break;

            default:
                if (!context.CancellationToken.IsCancellationRequested)
                    await GenerateFaultAsync(context, exception).ConfigureAwait(false);
                break;
        }

        await ReceiveContext.NotifyFaultedAsync(context, duration, consumerType, exception, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Registers an observer for sends initiated by this consume context.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>An idempotent handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return ReceiveContext.SendEndpointProvider.ConnectSendObserver(observer);
    }

    /// <summary>Adds an operation that must finish before this delivery is considered consumed.</summary>
    /// <param name="task">The operation to attach to consume completion.</param>
    public abstract void AddConsumeTask(Task task);

    async Task RespondInternalAsync<T>(T message, IPipe<SendContext<T>>? pipe = null)
        where T : class
    {
        var sendEndpoint = await this.GetResponseEndpointAsync<T>(CancellationToken).ConfigureAwait(false);

        if (pipe.IsNotEmpty())
            await sendEndpoint.SendAsync(message, pipe!, CancellationToken).ConfigureAwait(false);
        else
            await sendEndpoint.SendAsync(message, CancellationToken).ConfigureAwait(false);
    }

    async Task RespondInternalAsync<T>(object values, IPipe<SendContext<T>>? pipe = null)
        where T : class
    {
        var sendEndpoint = await this.GetResponseEndpointAsync<T>(CancellationToken).ConfigureAwait(false);

        if (pipe.IsNotEmpty())
            await sendEndpoint.SendAsync(values, pipe!, CancellationToken).ConfigureAwait(false);
        else
            await sendEndpoint.SendAsync<T>(values, CancellationToken).ConfigureAwait(false);
    }

    /// <summary>Generates and sends the fault contract for a failed message.</summary>
    /// <typeparam name="T">The failed message contract type.</typeparam>
    /// <param name="context">The consume context whose processing failed.</param>
    /// <param name="exception">The failure represented by the fault.</param>
    /// <returns>The fault-publication operation for the failed consume context.</returns>
    protected virtual Task GenerateFaultAsync<T>(ConsumeContext<T> context, Exception exception)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exception);
        return context.GenerateFaultAsync(exception);
    }

    Task ConsumeTaskAsync(Task task)
    {
        ArgumentNullException.ThrowIfNull(task);
        AddConsumeTask(task);

        return task;
    }

    /// <summary>Resolves a publish endpoint and decorates it with the current consume context.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="cancellationToken">The token that cancels endpoint resolution.</param>
    /// <returns>A task containing the consume-context-aware send endpoint.</returns>
    protected override async Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
    {
        var publishSendEndpoint = await base.GetPublishSendEndpointAsync<T>(cancellationToken).ConfigureAwait(false);

        return new ConsumeSendEndpoint(publishSendEndpoint, this);
    }
}
