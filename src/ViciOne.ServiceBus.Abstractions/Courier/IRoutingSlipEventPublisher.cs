using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier;

/// <summary>
/// Defines the contract for routing slip event publisher.
/// </summary>
public interface IRoutingSlipEventPublisher
{
    /// <summary>
    /// Publishes routing slip completed.
    /// </summary>
    /// <param name="timestamp">The timestamp value.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="variables">The variables value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task PublishRoutingSlipCompletedAsync(DateTimeOffset timestamp, TimeSpan duration, IDictionary<string, object> variables, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes routing slip faulted.
    /// </summary>
    /// <param name="timestamp">The timestamp value.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="variables">The variables value.</param>
    /// <param name="exceptions">The exceptions value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task PublishRoutingSlipFaultedAsync(DateTimeOffset timestamp, TimeSpan duration, IDictionary<string, object> variables,
        IReadOnlyCollection<ActivityException> exceptions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes routing slip activity completed.
    /// </summary>
    /// <param name="activityName">The activity name value.</param>
    /// <param name="executionId">The execution id value.</param>
    /// <param name="timestamp">The timestamp value.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="variables">The variables value.</param>
    /// <param name="arguments">The arguments value.</param>
    /// <param name="data">The data value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task PublishRoutingSlipActivityCompletedAsync(string activityName, Guid executionId,
        DateTimeOffset timestamp, TimeSpan duration, IDictionary<string, object> variables, IDictionary<string, object> arguments,
        IDictionary<string, object> data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes routing slip activity faulted.
    /// </summary>
    /// <param name="activityName">The activity name value.</param>
    /// <param name="executionId">The execution id value.</param>
    /// <param name="timestamp">The timestamp value.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="exceptionInfo">The exception info value.</param>
    /// <param name="variables">The variables value.</param>
    /// <param name="arguments">The arguments value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task PublishRoutingSlipActivityFaultedAsync(string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration, ExceptionInfo exceptionInfo,
        IDictionary<string, object> variables, IDictionary<string, object> arguments, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes routing slip activity compensation failed.
    /// </summary>
    /// <param name="activityName">The activity name value.</param>
    /// <param name="executionId">The execution id value.</param>
    /// <param name="timestamp">The timestamp value.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="failureTimestamp">The failure timestamp value.</param>
    /// <param name="routingSlipDuration">The routing slip duration value.</param>
    /// <param name="exceptionInfo">The exception info value.</param>
    /// <param name="variables">The variables value.</param>
    /// <param name="data">The data value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task PublishRoutingSlipActivityCompensationFailedAsync(string activityName, Guid executionId,
        DateTimeOffset timestamp, TimeSpan duration, DateTimeOffset failureTimestamp, TimeSpan routingSlipDuration,
        ExceptionInfo exceptionInfo, IDictionary<string, object> variables, IDictionary<string, object> data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes routing slip activity compensated.
    /// </summary>
    /// <param name="activityName">The activity name value.</param>
    /// <param name="executionId">The execution id value.</param>
    /// <param name="timestamp">The timestamp value.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="variables">The variables value.</param>
    /// <param name="data">The data value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task PublishRoutingSlipActivityCompensatedAsync(string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration,
        IDictionary<string, object> variables, IDictionary<string, object> data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes routing slip revised.
    /// </summary>
    /// <param name="activityName">The activity name value.</param>
    /// <param name="executionId">The execution id value.</param>
    /// <param name="timestamp">The timestamp value.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="variables">The variables value.</param>
    /// <param name="itinerary">The itinerary value.</param>
    /// <param name="previousItinerary">The previous itinerary value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task PublishRoutingSlipRevisedAsync(string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration, IDictionary<string, object> variables,
        IList<Activity> itinerary,
        IList<Activity> previousItinerary, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes routing slip terminated.
    /// </summary>
    /// <param name="activityName">The activity name value.</param>
    /// <param name="executionId">The execution id value.</param>
    /// <param name="timestamp">The timestamp value.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="variables">The variables value.</param>
    /// <param name="previousItinerary">The previous itinerary value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task PublishRoutingSlipTerminatedAsync(string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration, IDictionary<string, object> variables,
        IList<Activity> previousItinerary, CancellationToken cancellationToken = default);
}
