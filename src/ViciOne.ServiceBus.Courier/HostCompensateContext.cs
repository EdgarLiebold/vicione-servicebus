using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Results;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Carries state for host compensate operations.</summary>
/// <typeparam name="TLog">The log type.</typeparam>
internal sealed class HostCompensateContext<TLog> :
    BaseCourierContext,
    CompensateContext<TLog>
    where TLog : class
{
    readonly ActivityLog _activityLog;
    readonly CompensateLog _compensateLog;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public HostCompensateContext(ConsumeContext<RoutingSlip> context)
        : base(context)
    {
        if (RoutingSlip.CompensateLogs.Count == 0)
            throw new ArgumentException("The routing slip must contain at least one compensation log.", nameof(context));

        _compensateLog = RoutingSlip.CompensateLogs.Last();

        _activityLog = RoutingSlip.ActivityLogs.SingleOrDefault(x => x.ExecutionId == _compensateLog.ExecutionId)
            ?? throw new RoutingSlipException("The compensation log did not have a matching activity log entry: "
                + _compensateLog.ExecutionId);

        Log = RoutingSlip.GetCompensateLogData<TLog>();
    }

    /// <summary>Gets the activity name.</summary>
    public override string ActivityName => _activityLog.Name;
    /// <summary>Gets the log.</summary>
    public TLog Log { get; }

    /// <summary>Creates activity context.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <param name="activity">The activity.</param>
    /// <returns>The created activity context.</returns>
    public CompensateActivityContext<TActivity, TLog> CreateActivityContext<TActivity>(TActivity activity)
        where TActivity : class
    {
        return new HostCompensateActivityContext<TActivity, TLog>(activity, this);
    }

    /// <summary>Gets or sets the result; the value is unset until the activity completes.</summary>
    public CompensationResult? Result { get; set; }
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

    /// <summary>Reports a failed result.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The compensation result produced by the operation.</returns>
    public CompensationResult Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new FailedCompensationResult<TLog>(this, Publisher, _compensateLog, RoutingSlip, exception);
    }
}
