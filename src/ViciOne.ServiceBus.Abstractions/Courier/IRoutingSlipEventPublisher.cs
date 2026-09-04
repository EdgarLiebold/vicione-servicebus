using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier;

public interface IRoutingSlipEventPublisher
{
    Task PublishRoutingSlipCompletedAsync(DateTimeOffset timestamp, TimeSpan duration, IDictionary<string, object> variables, CancellationToken cancellationToken = default);

    Task PublishRoutingSlipFaultedAsync(DateTimeOffset timestamp, TimeSpan duration, IDictionary<string, object> variables,
        IReadOnlyCollection<ActivityException> exceptions, CancellationToken cancellationToken = default);

    Task PublishRoutingSlipActivityCompletedAsync(string activityName, Guid executionId,
        DateTimeOffset timestamp, TimeSpan duration, IDictionary<string, object> variables, IDictionary<string, object> arguments,
        IDictionary<string, object> data, CancellationToken cancellationToken = default);

    Task PublishRoutingSlipActivityFaultedAsync(string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration, ExceptionInfo exceptionInfo,
        IDictionary<string, object> variables, IDictionary<string, object> arguments, CancellationToken cancellationToken = default);

    Task PublishRoutingSlipActivityCompensationFailedAsync(string activityName, Guid executionId,
        DateTimeOffset timestamp, TimeSpan duration, DateTimeOffset failureTimestamp, TimeSpan routingSlipDuration,
        ExceptionInfo exceptionInfo, IDictionary<string, object> variables, IDictionary<string, object> data, CancellationToken cancellationToken = default);

    Task PublishRoutingSlipActivityCompensatedAsync(string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration,
        IDictionary<string, object> variables, IDictionary<string, object> data, CancellationToken cancellationToken = default);

    Task PublishRoutingSlipRevisedAsync(string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration, IDictionary<string, object> variables,
        IList<Activity> itinerary,
        IList<Activity> previousItinerary, CancellationToken cancellationToken = default);

    Task PublishRoutingSlipTerminatedAsync(string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration, IDictionary<string, object> variables,
        IList<Activity> previousItinerary, CancellationToken cancellationToken = default);
}
