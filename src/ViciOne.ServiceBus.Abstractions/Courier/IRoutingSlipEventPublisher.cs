using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Defines the operations required by routing slip event publisher.</summary>
public interface IRoutingSlipEventPublisher
{
    /// <summary>Publishes routing slip completed.</summary>
    /// <param name="timestamp">The timestamp.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="variables">The variables.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PublishRoutingSlipCompletedAsync(DateTimeOffset timestamp, TimeSpan duration, IDictionary<string, object> variables, CancellationToken cancellationToken = default);

    /// <summary>Publishes routing slip faulted.</summary>
    /// <param name="timestamp">The timestamp.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="variables">The variables.</param>
    /// <param name="exceptions">The exceptions.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PublishRoutingSlipFaultedAsync(DateTimeOffset timestamp, TimeSpan duration, IDictionary<string, object> variables,
        IReadOnlyCollection<ActivityException> exceptions, CancellationToken cancellationToken = default);

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
    Task PublishRoutingSlipActivityCompletedAsync(string activityName, Guid executionId,
        DateTimeOffset timestamp, TimeSpan duration, IDictionary<string, object> variables, IDictionary<string, object> arguments,
        IDictionary<string, object> data, CancellationToken cancellationToken = default);

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
    Task PublishRoutingSlipActivityFaultedAsync(string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration, ExceptionInfo exceptionInfo,
        IDictionary<string, object> variables, IDictionary<string, object> arguments, CancellationToken cancellationToken = default);

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
    Task PublishRoutingSlipActivityCompensationFailedAsync(string activityName, Guid executionId,
        DateTimeOffset timestamp, TimeSpan duration, DateTimeOffset failureTimestamp, TimeSpan routingSlipDuration,
        ExceptionInfo exceptionInfo, IDictionary<string, object> variables, IDictionary<string, object> data, CancellationToken cancellationToken = default);

    /// <summary>Publishes routing slip activity compensated.</summary>
    /// <param name="activityName">The activity name.</param>
    /// <param name="executionId">The execution id.</param>
    /// <param name="timestamp">The timestamp.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="variables">The variables.</param>
    /// <param name="data">The data.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PublishRoutingSlipActivityCompensatedAsync(string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration,
        IDictionary<string, object> variables, IDictionary<string, object> data, CancellationToken cancellationToken = default);

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
    Task PublishRoutingSlipRevisedAsync(string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration, IDictionary<string, object> variables,
        IList<Activity> itinerary,
        IList<Activity> previousItinerary, CancellationToken cancellationToken = default);

    /// <summary>Publishes routing slip terminated.</summary>
    /// <param name="activityName">The activity name.</param>
    /// <param name="executionId">The execution id.</param>
    /// <param name="timestamp">The timestamp.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="variables">The variables.</param>
    /// <param name="previousItinerary">The previous itinerary.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PublishRoutingSlipTerminatedAsync(string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration, IDictionary<string, object> variables,
        IList<Activity> previousItinerary, CancellationToken cancellationToken = default);
}
