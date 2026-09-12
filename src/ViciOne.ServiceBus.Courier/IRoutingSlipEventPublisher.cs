using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Routes routing-slip lifecycle events to configured subscriptions and the publish topology.</summary>
internal interface IRoutingSlipEventPublisher
{
    /// <summary>Routes the terminal completion event.</summary>
    /// <param name="timestamp">The completion timestamp.</param>
    /// <param name="duration">The total routing-slip duration.</param>
    /// <param name="variables">The final routing-slip variables.</param>
    /// <param name="cancellationToken">The token that cancels event delivery.</param>
    /// <returns>A task that completes after every selected delivery has been accepted.</returns>
    Task PublishRoutingSlipCompletedAsync(DateTimeOffset timestamp, TimeSpan duration, IReadOnlyDictionary<string, object> variables, CancellationToken cancellationToken = default);

    /// <summary>Routes the terminal routing-slip failure event.</summary>
    /// <param name="timestamp">The failure timestamp.</param>
    /// <param name="duration">The total routing-slip duration before failure.</param>
    /// <param name="variables">The routing-slip variables available at failure.</param>
    /// <param name="exceptions">The recorded activity failures.</param>
    /// <param name="cancellationToken">The token that cancels event delivery.</param>
    /// <returns>A task that completes after every selected delivery has been accepted.</returns>
    Task PublishRoutingSlipFaultedAsync(DateTimeOffset timestamp, TimeSpan duration, IReadOnlyDictionary<string, object> variables,
        IReadOnlyCollection<ActivityException> exceptions, CancellationToken cancellationToken = default);

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
    Task PublishRoutingSlipActivityCompletedAsync(string activityName, Guid executionId,
        DateTimeOffset timestamp, TimeSpan duration, IReadOnlyDictionary<string, object> variables, IReadOnlyDictionary<string, object> arguments,
        IReadOnlyDictionary<string, object> data, CancellationToken cancellationToken = default);

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
    Task PublishRoutingSlipActivityFaultedAsync(string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration, ExceptionInfo exceptionInfo,
        IReadOnlyDictionary<string, object> variables, IReadOnlyDictionary<string, object> arguments, CancellationToken cancellationToken = default);

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
    Task PublishRoutingSlipActivityCompensationFailedAsync(string activityName, Guid executionId,
        DateTimeOffset timestamp, TimeSpan duration, DateTimeOffset failureTimestamp, TimeSpan routingSlipDuration,
        ExceptionInfo exceptionInfo, IReadOnlyDictionary<string, object> variables, IReadOnlyDictionary<string, object> data, CancellationToken cancellationToken = default);

    /// <summary>Routes an activity-compensation event.</summary>
    /// <param name="activityName">The compensated activity name.</param>
    /// <param name="executionId">The activity execution identifier.</param>
    /// <param name="timestamp">The compensation timestamp.</param>
    /// <param name="duration">The compensation duration.</param>
    /// <param name="variables">The routing-slip variables after compensation.</param>
    /// <param name="data">The compensation result data.</param>
    /// <param name="cancellationToken">The token that cancels event delivery.</param>
    /// <returns>A task that completes after every selected delivery has been accepted.</returns>
    Task PublishRoutingSlipActivityCompensatedAsync(string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration,
        IReadOnlyDictionary<string, object> variables, IReadOnlyDictionary<string, object> data, CancellationToken cancellationToken = default);

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
    Task PublishRoutingSlipRevisedAsync(string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration, IReadOnlyDictionary<string, object> variables,
        IEnumerable<Activity> itinerary,
        IEnumerable<Activity> previousItinerary, CancellationToken cancellationToken = default);

    /// <summary>Routes a routing-slip termination event.</summary>
    /// <param name="activityName">The activity that terminated the routing slip.</param>
    /// <param name="executionId">The activity execution identifier.</param>
    /// <param name="timestamp">The termination timestamp.</param>
    /// <param name="duration">The terminating activity duration.</param>
    /// <param name="variables">The final routing-slip variables.</param>
    /// <param name="previousItinerary">The itinerary discarded by termination.</param>
    /// <param name="cancellationToken">The token that cancels event delivery.</param>
    /// <returns>A task that completes after every selected delivery has been accepted.</returns>
    Task PublishRoutingSlipTerminatedAsync(string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration, IReadOnlyDictionary<string, object> variables,
        IEnumerable<Activity> previousItinerary, CancellationToken cancellationToken = default);
}
