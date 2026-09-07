using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Messages;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Provides the send-endpoint pipeline used to capture custom routing-slip subscription messages.</summary>
internal sealed class RoutingSlipSubscriptionCaptureEndpoint :
    ISendEndpoint,
    Advanced.IAdvancedSendEndpoint
{
    readonly string? _activityName;
    readonly IRoutingSlipSubscriptionTarget _target;
    readonly Uri _destinationAddress;
    readonly RoutingSlipEvents _events;
    readonly RoutingSlipEventContents _contents;
    readonly SendObservable _observers;

    /// <summary>Creates an endpoint that captures messages as routing-slip subscriptions.</summary>
    /// <param name="target">The routing-slip target that receives captured messages.</param>
    /// <param name="destinationAddress">The destination that receives matching lifecycle events.</param>
    /// <param name="events">The non-empty lifecycle-event selection.</param>
    /// <param name="activityName">The activity-name filter, when configured.</param>
    /// <param name="contents">The optional routing-slip data included in delivered events.</param>
    internal RoutingSlipSubscriptionCaptureEndpoint(IRoutingSlipSubscriptionTarget target, Uri destinationAddress, RoutingSlipEvents events,
        string? activityName, RoutingSlipEventContents contents = RoutingSlipEventContents.All)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(destinationAddress);
        if (activityName is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(activityName);

        _observers = new SendObservable();
        _target = target;
        _events = RoutingSlipSubscriptionSelection.Validate(events, nameof(events));
        _activityName = activityName;
        _contents = RoutingSlipSubscriptionSelection.Validate(contents, nameof(contents));
        _destinationAddress = destinationAddress;
    }

    /// <summary>Captures a typed subscription message without additional send-pipeline configuration.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="message">The subscription message to capture.</param>
    /// <param name="cancellationToken">The token that cancels message capture.</param>
    /// <returns>A task that completes after the subscription message has been captured.</returns>
    public Task SendAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);

        return SendAsync(message, Pipe.Empty<SendContext<T>>(), cancellationToken);
    }

    /// <summary>Runs the typed send pipeline and captures its serialized subscription message.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="message">The subscription message to capture.</param>
    /// <param name="pipe">The send pipeline that configures the message context.</param>
    /// <param name="cancellationToken">The token that cancels message capture.</param>
    /// <returns>A task that completes after observer notification, pipeline execution, and message capture.</returns>
    public async Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        var context = new RoutingSlipSendContext<T>(message, cancellationToken, _destinationAddress);

        try
        {
            if (_observers.Count > 0)
                await _observers.PreSendAsync(context).ConfigureAwait(false);

            await pipe.SendAsync(context).ConfigureAwait(false);

            _target.AddSubscription(_destinationAddress, _events, _contents, _activityName, context.GetMessageEnvelope());

            if (_observers.Count > 0)
                await _observers.PostSendAsync(context).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (_observers.Count > 0)
                await _observers.SendFaultAsync(context, exception).ConfigureAwait(false);

            throw;
        }
    }

    /// <summary>Runs an untyped send pipeline for a typed message and captures the result.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="message">The subscription message to capture.</param>
    /// <param name="pipe">The send pipeline that configures the message context.</param>
    /// <param name="cancellationToken">The token that cancels message capture.</param>
    /// <returns>A task that completes after the subscription message has been captured.</returns>
    public Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        return SendAsync(message, (IPipe<SendContext<T>>)pipe, cancellationToken);
    }

    /// <summary>Captures a subscription message using its runtime contract type.</summary>
    /// <param name="message">The subscription message to capture.</param>
    /// <param name="cancellationToken">The token that cancels message capture.</param>
    /// <returns>A task that completes after the subscription message has been captured.</returns>
    public Task SendAsync(object message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var messageType = message.GetType();

        return SendEndpointConverterCache.SendAsync(this, message, messageType, cancellationToken);
    }

    /// <summary>Captures a subscription message using an explicit runtime contract type.</summary>
    /// <param name="message">The subscription message to capture.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="cancellationToken">The token that cancels message capture.</param>
    /// <returns>A task that completes after the subscription message has been captured.</returns>
    public Task SendAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);

        return SendEndpointConverterCache.SendAsync(this, message, messageType, cancellationToken);
    }

    /// <summary>Runs an untyped send pipeline and captures a message using its runtime contract type.</summary>
    /// <param name="message">The subscription message to capture.</param>
    /// <param name="pipe">The send pipeline that configures the message context.</param>
    /// <param name="cancellationToken">The token that cancels message capture.</param>
    /// <returns>A task that completes after the subscription message has been captured.</returns>
    public Task SendAsync(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        var messageType = message.GetType();

        return SendEndpointConverterCache.SendAsync(this, message, messageType, pipe, cancellationToken);
    }

    /// <summary>Runs an untyped send pipeline and captures a message using an explicit runtime contract type.</summary>
    /// <param name="message">The subscription message to capture.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="pipe">The send pipeline that configures the message context.</param>
    /// <param name="cancellationToken">The token that cancels message capture.</param>
    /// <returns>A task that completes after the subscription message has been captured.</returns>
    public Task SendAsync(object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(pipe);

        return SendEndpointConverterCache.SendAsync(this, message, messageType, pipe, cancellationToken);
    }

    /// <summary>Initializes and captures a typed subscription message from object values.</summary>
    /// <typeparam name="T">The message contract type to initialize.</typeparam>
    /// <param name="values">The values used to initialize the message contract.</param>
    /// <param name="cancellationToken">The token that cancels initialization and message capture.</param>
    /// <returns>A task that completes after the initialized message has been captured.</returns>
    public async Task SendAsync<T>(object values, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, cancellationToken).ConfigureAwait(false);

        await SendAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Initializes a typed subscription message, applies a typed pipeline, and captures the result.</summary>
    /// <typeparam name="T">The message contract type to initialize.</typeparam>
    /// <param name="values">The values used to initialize the message contract.</param>
    /// <param name="pipe">The typed send pipeline appended to initialization.</param>
    /// <param name="cancellationToken">The token that cancels initialization and message capture.</param>
    /// <returns>A task that completes after the initialized message has been captured.</returns>
    public async Task SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, pipe, cancellationToken).ConfigureAwait(false);

        await SendAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Initializes a typed subscription message, applies an untyped pipeline, and captures the result.</summary>
    /// <typeparam name="T">The message contract type to initialize.</typeparam>
    /// <param name="values">The values used to initialize the message contract.</param>
    /// <param name="pipe">The untyped send pipeline appended to initialization.</param>
    /// <param name="cancellationToken">The token that cancels initialization and message capture.</param>
    /// <returns>A task that completes after the initialized message has been captured.</returns>
    public async Task SendAsync<T>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, pipe, cancellationToken).ConfigureAwait(false);

        await SendAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Connects an observer to the subscription-message capture lifecycle.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _observers.Connect(observer);
    }


    sealed class RoutingSlipSendContext<T> :
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
