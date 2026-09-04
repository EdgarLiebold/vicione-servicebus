using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Courier;

/// <summary>
/// Provides a routing slip builder send endpoint implementation.
/// </summary>
public class RoutingSlipBuilderSendEndpoint :
    ISendEndpoint,
    Advanced.IAdvancedSendEndpoint
{
    readonly string? _activityName = null!;
    readonly IRoutingSlipSendEndpointTarget _builder;
    readonly Uri _destinationAddress;
    readonly RoutingSlipEvents _events;
    readonly RoutingSlipEventContents _include;
    readonly SendObservable _observers;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="events">The events value.</param>
    /// <param name="activityName">The activity name value.</param>
    /// <param name="include">The include value.</param>
    public RoutingSlipBuilderSendEndpoint(IRoutingSlipSendEndpointTarget builder, Uri destinationAddress, RoutingSlipEvents events, string? activityName,
        RoutingSlipEventContents include = RoutingSlipEventContents.All)
    {
        _observers = new SendObservable();
        _builder = builder;
        _events = events;
        _activityName = activityName;
        _include = include;
        _destinationAddress = destinationAddress;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return SendAsync(message, Pipe.Empty<SendContext<T>>(), cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        var context = new RoutingSlipSendContext<T>(message, cancellationToken, _destinationAddress);

        await pipe.SendAsync(context).ConfigureAwait(false);

        _builder.AddSubscription(_destinationAddress, _events, _include, _activityName, context.GetMessageEnvelope());
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        return SendAsync(message, (IPipe<SendContext<T>>)pipe, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(object message, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();

        return SendEndpointConverterCache.SendAsync(this, message, messageType, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));

        return SendEndpointConverterCache.SendAsync(this, message, messageType, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        var messageType = message.GetType();

        return SendEndpointConverterCache.SendAsync(this, message, messageType, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));

        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        return SendEndpointConverterCache.SendAsync(this, message, messageType, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync<T>(object values, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, cancellationToken).ConfigureAwait(false);

        await SendAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, pipe, cancellationToken).ConfigureAwait(false);

        await SendAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync<T>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, pipe, cancellationToken).ConfigureAwait(false);

        await SendAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Connects send observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _observers.Connect(observer);
    }


    class RoutingSlipSendContext<T> :
        MessageSendContext<T>
        where T : class
    {
        public RoutingSlipSendContext(T message, CancellationToken cancellationToken, Uri destinationAddress)
            : base(message, cancellationToken)
        {
            DestinationAddress = destinationAddress;

            Serializer = ServiceBusMetadataJson.MessageSerializer;
        }

        public MessageEnvelope GetMessageEnvelope()
        {
            var envelope = new JsonMessageEnvelope(this, Message);

            return envelope;
        }
    }
}
