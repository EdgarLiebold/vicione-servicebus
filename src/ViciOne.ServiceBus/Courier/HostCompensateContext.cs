using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Results;

namespace ViciOne.ServiceBus.Courier;

/// <summary>
/// Provides a host compensate context implementation.
/// </summary>
/// <typeparam name="TLog">The t log type.</typeparam>
public class HostCompensateContext<TLog> :
    BaseCourierContext,
    CompensateContext<TLog>
    where TLog : class
{
    readonly ActivityLog _activityLog;
    readonly CompensateLog _compensateLog;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public HostCompensateContext(ConsumeContext<RoutingSlip> context)
        : base(context)
    {
        if (RoutingSlip.CompensateLogs.Count == 0)
            throw new ArgumentException("The routingSlip must contain at least one activity log");

        _compensateLog = RoutingSlip.CompensateLogs.Last();

        _activityLog = RoutingSlip.ActivityLogs.SingleOrDefault(x => x.ExecutionId == _compensateLog.ExecutionId)
            ?? throw new RoutingSlipException("The compensation log did not have a matching activity log entry: "
                + _compensateLog.ExecutionId);

        Log = RoutingSlip.GetCompensateLogData<TLog>();
    }

    /// <summary>
    /// Gets the activity name value.
    /// </summary>
    public override string ActivityName => _activityLog.Name;
    /// <summary>
    /// Gets the log value.
    /// </summary>
    public TLog Log { get; }

    /// <summary>
    /// Creates activity context.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <param name="activity">The activity value.</param>
    /// <returns>The result of the operation.</returns>
    public CompensateActivityContext<TActivity, TLog> CreateActivityContext<TActivity>(TActivity activity)
        where TActivity : class, ICompensateActivity<TLog>
    {
        return new HostCompensateActivityContext<TActivity, TLog>(activity, this);
    }

    /// <summary>
    /// Gets or sets the result value.
    /// </summary>
    public CompensationResult Result { get; set; } = null!;
    CompensationResult CompensateContext.Compensated()
    {
        return new CompensatedCompensationResult<TLog>(this, Publisher, _compensateLog, RoutingSlip);
    }

    CompensationResult CompensateContext.Compensated(object values)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        var result = new CompensatedCompensationResult<TLog>(this, Publisher, _compensateLog, RoutingSlip);

        result.SetVariables(values);

        return result;
    }

    CompensationResult CompensateContext.Compensated(IDictionary<string, object> variables)
    {
        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        var result = new CompensatedCompensationResult<TLog>(this, Publisher, _compensateLog, RoutingSlip);

        result.SetVariables(variables);

        return result;
    }

    CompensationResult CompensateContext.Failed()
    {
        return Failed(new RoutingSlipException("The routing slip compensation failed"));
    }

    /// <summary>
    /// Performs the failed operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public CompensationResult Failed(Exception exception)
    {
        return new FailedCompensationResult<TLog>(this, Publisher, _compensateLog, RoutingSlip, exception);
    }
}
