using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Provides an endpoint for routing slip builder send.</summary>
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

    /// <summary>Initializes a new instance.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="events">The events.</param>
    /// <param name="activityName">The activity name.</param>
    /// <param name="include">The include.</param>
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

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return SendAsync(message, Pipe.Empty<SendContext<T>>(), cancellationToken);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        return SendAsync(message, (IPipe<SendContext<T>>)pipe, cancellationToken);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(object message, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();

        return SendEndpointConverterCache.SendAsync(this, message, messageType, cancellationToken);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));

        return SendEndpointConverterCache.SendAsync(this, message, messageType, cancellationToken);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        var messageType = message.GetType();

        return SendEndpointConverterCache.SendAsync(this, message, messageType, pipe, cancellationToken);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync<T>(object values, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, cancellationToken).ConfigureAwait(false);

        await SendAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, pipe, cancellationToken).ConfigureAwait(false);

        await SendAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>Connects send observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
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
