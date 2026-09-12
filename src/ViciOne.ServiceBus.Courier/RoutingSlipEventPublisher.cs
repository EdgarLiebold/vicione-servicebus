using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Messages;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Routes routing-slip lifecycle events to subscriptions and the publish topology.</summary>
internal sealed class RoutingSlipEventPublisher :
    IRoutingSlipEventPublisher
{
    readonly CourierContext? _context;
    readonly HostInfo _host;
    readonly IPublishEndpoint _publishEndpoint;
    readonly RoutingSlip _routingSlip;
    readonly ISendEndpointProvider _sendEndpointProvider;

    /// <summary>Creates a publisher that can preserve the current Courier serialization context.</summary>
    /// <param name="context">The active Courier execution context.</param>
    /// <param name="routingSlip">The routing slip whose lifecycle events are routed.</param>
    public RoutingSlipEventPublisher(CourierContext context, RoutingSlip routingSlip)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(routingSlip);

        _sendEndpointProvider = context;
        _publishEndpoint = context;
        _routingSlip = routingSlip;
        _host = context.Host;
        _context = context;
    }

    /// <summary>Creates a publisher from explicit send and publish capabilities.</summary>
    /// <param name="sendEndpointProvider">The provider that resolves subscription destinations.</param>
    /// <param name="publishEndpoint">The endpoint used for topology publication.</param>
    /// <param name="routingSlip">The routing slip whose lifecycle events are routed.</param>
    public RoutingSlipEventPublisher(ISendEndpointProvider sendEndpointProvider, IPublishEndpoint publishEndpoint, RoutingSlip routingSlip)
    {
        ArgumentNullException.ThrowIfNull(sendEndpointProvider);
        ArgumentNullException.ThrowIfNull(publishEndpoint);
        ArgumentNullException.ThrowIfNull(routingSlip);

        _sendEndpointProvider = sendEndpointProvider;
        _publishEndpoint = publishEndpoint;
        _routingSlip = routingSlip;
        _host = HostMetadataCache.Host;
    }

    static IReadOnlyDictionary<string, object> CreateEmptyObject() => new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Routes the terminal completion event.</summary>
    /// <param name="timestamp">The completion timestamp.</param>
    /// <param name="duration">The total routing-slip duration.</param>
    /// <param name="variables">The final routing-slip variables.</param>
    /// <param name="cancellationToken">The token that cancels event delivery.</param>
    /// <returns>A task that completes after every selected delivery has been accepted.</returns>
    public Task PublishRoutingSlipCompletedAsync(DateTimeOffset timestamp, TimeSpan duration, IReadOnlyDictionary<string, object> variables, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);
        ArgumentNullException.ThrowIfNull(variables);

        return PublishEventAsync<RoutingSlipCompleted>(
            RoutingSlipEvents.Completed,
            contents => new RoutingSlipCompletedMessage(
                _routingSlip.TrackingNumber,
                timestamp,
                duration,
                Includes(contents, RoutingSlipEventContents.Variables) ? variables : CreateEmptyObject()),
            cancellationToken);
    }

    /// <summary>Routes the terminal routing-slip failure event.</summary>
    /// <param name="timestamp">The failure timestamp.</param>
    /// <param name="duration">The total routing-slip duration before failure.</param>
    /// <param name="variables">The routing-slip variables available at failure.</param>
    /// <param name="exceptions">The recorded activity failures.</param>
    /// <param name="cancellationToken">The token that cancels event delivery.</param>
    /// <returns>A task that completes after every selected delivery has been accepted.</returns>
    public Task PublishRoutingSlipFaultedAsync(DateTimeOffset timestamp, TimeSpan duration, IReadOnlyDictionary<string, object> variables,
        IReadOnlyCollection<ActivityException> exceptions, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);
        ArgumentNullException.ThrowIfNull(variables);
        ArgumentNullException.ThrowIfNull(exceptions);

        return PublishEventAsync<RoutingSlipFaulted>(
            RoutingSlipEvents.Faulted,
            contents => new RoutingSlipFaultedMessage(
                _routingSlip.TrackingNumber,
                timestamp,
                duration,
                exceptions,
                Includes(contents, RoutingSlipEventContents.Variables) ? variables : CreateEmptyObject()),
            cancellationToken);
    }

    /// <summary>Routes an activity-completion event.</summary>
    /// <param name="activityName">The completed activity name.</param>
    /// <param name="executionId">The activity execution identifier.</param>
    /// <param name="timestamp">The completion timestamp.</param>
    /// <param name="duration">The activity duration.</param>
    /// <param name="variables">The routing-slip variables after completion.</param>
    /// <param name="arguments">The activity arguments.</param>
    /// <param name="data">The activity result data.</param>
    /// <param name="cancellationToken">The token that cancels event delivery.</param>
    /// <returns>A task that completes after every selected delivery has been accepted.</returns>
    public Task PublishRoutingSlipActivityCompletedAsync(string activityName, Guid executionId,
        DateTimeOffset timestamp, TimeSpan duration, IReadOnlyDictionary<string, object> variables, IReadOnlyDictionary<string, object> arguments,
        IReadOnlyDictionary<string, object> data, CancellationToken cancellationToken = default)
    {
        ValidateActivityBoundary(activityName, executionId, duration);
        ArgumentNullException.ThrowIfNull(variables);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(data);

        return PublishEventAsync<RoutingSlipActivityCompleted>(
            RoutingSlipEvents.ActivityCompleted,
            contents => new RoutingSlipActivityCompletedMessage(
                _host,
                _routingSlip.TrackingNumber,
                activityName,
                executionId,
                timestamp,
                duration,
                Includes(contents, RoutingSlipEventContents.Variables) ? variables : CreateEmptyObject(),
                Includes(contents, RoutingSlipEventContents.Arguments) ? arguments : CreateEmptyObject(),
                Includes(contents, RoutingSlipEventContents.Data) ? data : CreateEmptyObject()),
            cancellationToken,
            activityName);
    }

    /// <summary>Routes an activity-failure event.</summary>
    /// <param name="activityName">The failed activity name.</param>
    /// <param name="executionId">The activity execution identifier.</param>
    /// <param name="timestamp">The failure timestamp.</param>
    /// <param name="duration">The activity duration before failure.</param>
    /// <param name="exceptionInfo">The captured activity failure.</param>
    /// <param name="variables">The routing-slip variables available at failure.</param>
    /// <param name="arguments">The activity arguments.</param>
    /// <param name="cancellationToken">The token that cancels event delivery.</param>
    /// <returns>A task that completes after every selected delivery has been accepted.</returns>
    public Task PublishRoutingSlipActivityFaultedAsync(string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration, ExceptionInfo exceptionInfo,
        IReadOnlyDictionary<string, object> variables, IReadOnlyDictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        ValidateActivityBoundary(activityName, executionId, duration);
        ArgumentNullException.ThrowIfNull(exceptionInfo);
        ArgumentNullException.ThrowIfNull(variables);
        ArgumentNullException.ThrowIfNull(arguments);

        return PublishEventAsync<RoutingSlipActivityFaulted>(
            RoutingSlipEvents.ActivityFaulted,
            contents => new RoutingSlipActivityFaultedMessage(
                _host,
                _routingSlip.TrackingNumber,
                activityName,
                executionId,
                timestamp,
                duration,
                exceptionInfo,
                Includes(contents, RoutingSlipEventContents.Variables) ? variables : CreateEmptyObject(),
                Includes(contents, RoutingSlipEventContents.Arguments) ? arguments : CreateEmptyObject()),
            cancellationToken,
            activityName);
    }

    /// <summary>Routes an activity-compensation event.</summary>
    /// <param name="activityName">The compensated activity name.</param>
    /// <param name="executionId">The activity execution identifier.</param>
    /// <param name="timestamp">The compensation timestamp.</param>
    /// <param name="duration">The compensation duration.</param>
    /// <param name="variables">The routing-slip variables after compensation.</param>
    /// <param name="data">The compensation result data.</param>
    /// <param name="cancellationToken">The token that cancels event delivery.</param>
    /// <returns>A task that completes after every selected delivery has been accepted.</returns>
    public Task PublishRoutingSlipActivityCompensatedAsync(string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration,
        IReadOnlyDictionary<string, object> variables, IReadOnlyDictionary<string, object> data, CancellationToken cancellationToken = default)
    {
        ValidateActivityBoundary(activityName, executionId, duration);
        ArgumentNullException.ThrowIfNull(variables);
        ArgumentNullException.ThrowIfNull(data);

        return PublishEventAsync<RoutingSlipActivityCompensated>(
            RoutingSlipEvents.ActivityCompensated,
            contents => new RoutingSlipActivityCompensatedMessage(
                _host,
                _routingSlip.TrackingNumber,
                activityName,
                executionId,
                timestamp,
                duration,
                Includes(contents, RoutingSlipEventContents.Variables) ? variables : CreateEmptyObject(),
                Includes(contents, RoutingSlipEventContents.Data) ? data : CreateEmptyObject()),
            cancellationToken,
            activityName);
    }

    /// <summary>Routes an itinerary-revision event.</summary>
    /// <param name="activityName">The activity that revised the itinerary.</param>
    /// <param name="executionId">The activity execution identifier.</param>
    /// <param name="timestamp">The revision timestamp.</param>
    /// <param name="duration">The revising activity duration.</param>
    /// <param name="variables">The routing-slip variables after revision.</param>
    /// <param name="itinerary">The retained and appended itinerary.</param>
    /// <param name="previousItinerary">The discarded itinerary.</param>
    /// <param name="cancellationToken">The token that cancels event delivery.</param>
    /// <returns>A task that completes after every selected delivery has been accepted.</returns>
    public Task PublishRoutingSlipRevisedAsync(string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration,
        IReadOnlyDictionary<string, object> variables,
        IEnumerable<Activity> itinerary, IEnumerable<Activity> previousItinerary, CancellationToken cancellationToken = default)
    {
        ValidateActivityBoundary(activityName, executionId, duration);
        ArgumentNullException.ThrowIfNull(variables);
        ArgumentNullException.ThrowIfNull(itinerary);
        ArgumentNullException.ThrowIfNull(previousItinerary);

        return PublishEventAsync<RoutingSlipRevised>(
            RoutingSlipEvents.Revised,
            contents => new RoutingSlipRevisedMessage(
                _host,
                _routingSlip.TrackingNumber,
                activityName,
                executionId,
                timestamp,
                duration,
                Includes(contents, RoutingSlipEventContents.Variables) ? variables : CreateEmptyObject(),
                Includes(contents, RoutingSlipEventContents.Itinerary) ? itinerary : [],
                Includes(contents, RoutingSlipEventContents.Itinerary) ? previousItinerary : []),
            cancellationToken,
            activityName);
    }

    /// <summary>Routes a routing-slip termination event.</summary>
    /// <param name="activityName">The activity that terminated the routing slip.</param>
    /// <param name="executionId">The activity execution identifier.</param>
    /// <param name="timestamp">The termination timestamp.</param>
    /// <param name="duration">The terminating activity duration.</param>
    /// <param name="variables">The final routing-slip variables.</param>
    /// <param name="previousItinerary">The itinerary discarded by termination.</param>
    /// <param name="cancellationToken">The token that cancels event delivery.</param>
    /// <returns>A task that completes after every selected delivery has been accepted.</returns>
    public Task PublishRoutingSlipTerminatedAsync(string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration,
        IReadOnlyDictionary<string, object> variables,
        IEnumerable<Activity> previousItinerary, CancellationToken cancellationToken = default)
    {
        ValidateActivityBoundary(activityName, executionId, duration);
        ArgumentNullException.ThrowIfNull(variables);
        ArgumentNullException.ThrowIfNull(previousItinerary);

        return PublishEventAsync<RoutingSlipTerminated>(
            RoutingSlipEvents.Terminated,
            contents => new RoutingSlipTerminatedMessage(
                _host,
                _routingSlip.TrackingNumber,
                activityName,
                executionId,
                timestamp,
                duration,
                Includes(contents, RoutingSlipEventContents.Variables) ? variables : CreateEmptyObject(),
                Includes(contents, RoutingSlipEventContents.Itinerary) ? previousItinerary : []),
            cancellationToken,
            activityName);
    }

    /// <summary>Routes both the activity-level and routing-slip-level compensation failure events.</summary>
    /// <param name="activityName">The activity whose compensation failed.</param>
    /// <param name="executionId">The activity execution identifier.</param>
    /// <param name="timestamp">The activity compensation-failure timestamp.</param>
    /// <param name="duration">The failed compensation duration.</param>
    /// <param name="failureTimestamp">The routing-slip compensation-failure timestamp.</param>
    /// <param name="routingSlipDuration">The total routing-slip duration before compensation failed.</param>
    /// <param name="exceptionInfo">The captured compensation failure.</param>
    /// <param name="variables">The routing-slip variables available at failure.</param>
    /// <param name="data">The compensation data.</param>
    /// <param name="cancellationToken">The token that cancels event delivery.</param>
    /// <returns>A task that completes after both failure events have been routed.</returns>
    public Task PublishRoutingSlipActivityCompensationFailedAsync(string activityName, Guid executionId,
        DateTimeOffset timestamp, TimeSpan duration, DateTimeOffset failureTimestamp, TimeSpan routingSlipDuration,
        ExceptionInfo exceptionInfo, IReadOnlyDictionary<string, object> variables, IReadOnlyDictionary<string, object> data, CancellationToken cancellationToken = default)
    {
        ValidateActivityBoundary(activityName, executionId, duration);
        ArgumentOutOfRangeException.ThrowIfLessThan(routingSlipDuration, TimeSpan.Zero);
        ArgumentNullException.ThrowIfNull(exceptionInfo);
        ArgumentNullException.ThrowIfNull(variables);
        ArgumentNullException.ThrowIfNull(data);

        var activityTask = PublishEventAsync<RoutingSlipActivityCompensationFailed>(
            RoutingSlipEvents.ActivityCompensationFailed,
            contents => new RoutingSlipActivityCompensationFailedMessage(
                _host,
                _routingSlip.TrackingNumber,
                activityName,
                executionId,
                timestamp,
                duration,
                exceptionInfo,
                Includes(contents, RoutingSlipEventContents.Variables) ? variables : CreateEmptyObject(),
                Includes(contents, RoutingSlipEventContents.Data) ? data : CreateEmptyObject()),
            cancellationToken,
            activityName);

        var slipTask = PublishEventAsync<RoutingSlipCompensationFailed>(RoutingSlipEvents.CompensationFailed,
            contents => new RoutingSlipCompensationFailedMessage(
                _host,
                _routingSlip.TrackingNumber,
                failureTimestamp,
                routingSlipDuration,
                exceptionInfo,
                Includes(contents, RoutingSlipEventContents.Variables) ? variables : CreateEmptyObject()),
            cancellationToken);

        return Task.WhenAll(activityTask, slipTask);
    }

    async Task PublishEventAsync<T>(RoutingSlipEvents eventFlag, Func<RoutingSlipEventContents, T> messageFactory,
        CancellationToken cancellationToken, string? activityName = null)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var subscription in _routingSlip.Subscriptions)
            await PublishSubscriptionEventAsync(eventFlag, messageFactory, subscription, activityName, cancellationToken).ConfigureAwait(false);

        if (_routingSlip.Subscriptions.All(sub => sub.Events.HasFlag(RoutingSlipEvents.Supplemental)))
            await _publishEndpoint.PublishAsync(messageFactory(RoutingSlipEventContents.All), cancellationToken).ConfigureAwait(false);
    }

    async Task PublishSubscriptionEventAsync<T>(RoutingSlipEvents eventFlag, Func<RoutingSlipEventContents, T> messageFactory,
        Subscription subscription, string? activityName, CancellationToken cancellationToken)
        where T : class
    {
        if (subscription.Events.HasFlag(eventFlag))
        {
            if (activityName is null
                || string.IsNullOrWhiteSpace(subscription.ActivityName)
                || activityName?.Equals(subscription.ActivityName, StringComparison.OrdinalIgnoreCase) == true)
            {
                var endpoint = await _sendEndpointProvider.GetSendEndpointAsync(subscription.Address, cancellationToken).ConfigureAwait(false);

                var message = messageFactory(subscription.Include);

                SerializerContext? serializerContext = (_context as ConsumeContext)?.SerializerContext;
                if (subscription.Message != null && serializerContext != null)
                {
                    var adapter = new MessageEnvelopeContextAdapter<T>(serializerContext, subscription.Message);

                    await endpoint.SendAsync(message, adapter, cancellationToken).ConfigureAwait(false);
                }
                else
                    await endpoint.SendAsync(message, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    static bool Includes(RoutingSlipEventContents contents, RoutingSlipEventContents value) => contents.HasFlag(value);

    static void ValidateActivityBoundary(string activityName, Guid executionId, TimeSpan duration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityName);
        if (executionId == Guid.Empty)
            throw new ArgumentException("The activity execution identifier cannot be empty.", nameof(executionId));
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);
    }


    sealed class MessageEnvelopeContextAdapter<T> :
        IPipe<SendContext<T>>
        where T : class
    {
        readonly SerializerContext _context;
        readonly MessageEnvelope _envelope;

        public MessageEnvelopeContextAdapter(SerializerContext context, MessageEnvelope envelope)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
        }

        public Task SendAsync(SendContext<T> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            context.Serializer = _context.GetMessageSerializer(_envelope, context.Message);

            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            context.CreateFilterScope("routingSlipSubscriptionEnvelope");
        }
    }
}
