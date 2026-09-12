using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Results;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Binds the newest routing-slip compensation entry to its activity record, deserialized log, and result factories.</summary>
/// <typeparam name="TLog">The compensation-log contract.</typeparam>
internal sealed class HostCompensateContext<TLog> :
    BaseCourierContext,
    CompensateContext<TLog>
    where TLog : class
{
    readonly ActivityLog _activityLog;
    readonly CompensateLog _compensateLog;

    /// <summary>Creates a compensation context for the newest compensation entry in the received routing slip.</summary>
    /// <param name="context">The received routing slip and transport context.</param>
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

    /// <summary>Gets the logical name of the activity being compensated.</summary>
    public override string ActivityName => _activityLog.Name;
    /// <summary>Gets the deserialized log recorded by the successful activity execution.</summary>
    public TLog Log { get; }

    /// <summary>Binds a resolved activity instance to this compensation context.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <param name="activity">The activity instance to expose to middleware.</param>
    /// <returns>A compensation context containing the activity instance and current log.</returns>
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

    /// <summary>Reports that compensation failed for the current activity.</summary>
    /// <param name="exception">The failure raised while compensating the activity.</param>
    /// <returns>A compensation-failure result containing the supplied exception.</returns>
    public CompensationResult Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new FailedCompensationResult<TLog>(this, Publisher, _compensateLog, RoutingSlip, exception);
    }
}
