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
    const RoutingSlipEvents EventMask = (RoutingSlipEvents)0xFFFF;

    readonly CourierContext? _context;
    readonly HostInfo _host;
    readonly IPublishEndpoint _publishEndpoint;
    readonly RoutingSlip _routingSlip;
    readonly ISendEndpointProvider _sendEndpointProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="routingSlip">The routing slip.</param>
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

    /// <summary>Initializes a new instance.</summary>
    /// <param name="sendEndpointProvider">The send endpoint provider.</param>
    /// <param name="publishEndpoint">The publish endpoint.</param>
    /// <param name="routingSlip">The routing slip.</param>
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

    static IDictionary<string, object> CreateEmptyObject() => new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Publishes routing slip completed.</summary>
    /// <param name="timestamp">The timestamp.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="variables">The variables.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishRoutingSlipCompletedAsync(DateTimeOffset timestamp, TimeSpan duration, IDictionary<string, object> variables, CancellationToken cancellationToken = default)
    {
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

    /// <summary>Publishes routing slip faulted.</summary>
    /// <param name="timestamp">The timestamp.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="variables">The variables.</param>
    /// <param name="exceptions">The exceptions.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishRoutingSlipFaultedAsync(DateTimeOffset timestamp, TimeSpan duration, IDictionary<string, object> variables,
        IReadOnlyCollection<ActivityException> exceptions, CancellationToken cancellationToken = default)
    {
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

    /// <summary>Publishes routing slip activity completed.</summary>
    /// <param name="activityName">The activity name.</param>
    /// <param name="executionId">The execution id.</param>
    /// <param name="timestamp">The timestamp.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="variables">The variables.</param>
    /// <param name="arguments">The arguments.</param>
    /// <param name="data">The data.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishRoutingSlipActivityCompletedAsync(string activityName, Guid executionId,
        DateTimeOffset timestamp, TimeSpan duration, IDictionary<string, object> variables, IDictionary<string, object> arguments,
        IDictionary<string, object> data, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityName);
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

    /// <summary>Publishes routing slip activity faulted.</summary>
    /// <param name="activityName">The activity name.</param>
    /// <param name="executionId">The execution id.</param>
    /// <param name="timestamp">The timestamp.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="exceptionInfo">The exception info.</param>
    /// <param name="variables">The variables.</param>
    /// <param name="arguments">The arguments.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishRoutingSlipActivityFaultedAsync(string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration, ExceptionInfo exceptionInfo,
        IDictionary<string, object> variables, IDictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityName);
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

    /// <summary>Publishes routing slip activity compensated.</summary>
    /// <param name="activityName">The activity name.</param>
    /// <param name="executionId">The execution id.</param>
    /// <param name="timestamp">The timestamp.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="variables">The variables.</param>
    /// <param name="data">The data.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishRoutingSlipActivityCompensatedAsync(string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration,
        IDictionary<string, object> variables, IDictionary<string, object> data, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityName);
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

    /// <summary>Publishes routing slip revised.</summary>
    /// <param name="activityName">The activity name.</param>
    /// <param name="executionId">The execution id.</param>
    /// <param name="timestamp">The timestamp.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="variables">The variables.</param>
    /// <param name="itinerary">The itinerary.</param>
    /// <param name="previousItinerary">The previous itinerary.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishRoutingSlipRevisedAsync(string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration,
        IDictionary<string, object> variables,
        IList<Activity> itinerary, IList<Activity> previousItinerary, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityName);
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

    /// <summary>Publishes routing slip terminated.</summary>
    /// <param name="activityName">The activity name.</param>
    /// <param name="executionId">The execution id.</param>
    /// <param name="timestamp">The timestamp.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="variables">The variables.</param>
    /// <param name="previousItinerary">The previous itinerary.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishRoutingSlipTerminatedAsync(string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration,
        IDictionary<string, object> variables,
        IList<Activity> previousItinerary, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityName);
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

    /// <summary>Publishes routing slip activity compensation failed.</summary>
    /// <param name="activityName">The activity name.</param>
    /// <param name="executionId">The execution id.</param>
    /// <param name="timestamp">The timestamp.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="failureTimestamp">The failure timestamp.</param>
    /// <param name="routingSlipDuration">The routing slip duration.</param>
    /// <param name="exceptionInfo">The exception info.</param>
    /// <param name="variables">The variables.</param>
    /// <param name="data">The data.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishRoutingSlipActivityCompensationFailedAsync(string activityName, Guid executionId,
        DateTimeOffset timestamp, TimeSpan duration, DateTimeOffset failureTimestamp, TimeSpan routingSlipDuration,
        ExceptionInfo exceptionInfo, IDictionary<string, object> variables, IDictionary<string, object> data, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityName);
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
        if ((subscription.Events & EventMask) == RoutingSlipEvents.All || subscription.Events.HasFlag(eventFlag))
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

    static bool Includes(RoutingSlipEventContents contents, RoutingSlipEventContents value) =>
        contents == RoutingSlipEventContents.All || contents.HasFlag(value);


    class MessageEnvelopeContextAdapter<T> :
        IPipe<SendContext<T>>
        where T : class
    {
        readonly SerializerContext _context;
        readonly MessageEnvelope _envelope;

        public MessageEnvelopeContextAdapter(SerializerContext context, MessageEnvelope envelope)
        {
            _context = context;
            _envelope = envelope;
        }

        public Task SendAsync(SendContext<T> context)
        {
            context.Serializer = _context.GetMessageSerializer(_envelope, context.Message);

            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
        }
    }
}
