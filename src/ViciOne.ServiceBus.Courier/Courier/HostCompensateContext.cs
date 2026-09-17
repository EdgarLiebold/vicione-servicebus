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
    readonly IActivityLog _activityLog;
    readonly ICompensateLog _compensateLog;

    /// <summary>Creates a compensation context for the newest compensation entry in the received routing slip.</summary>
    /// <param name="context">The received routing slip and transport context.</param>
    public HostCompensateContext(ConsumeContext<IRoutingSlip> context)
        : base(RequireContext(context))
    {
        if (RoutingSlip.CompensateLogs.Count == 0)
            throw new ArgumentException("The routing slip must contain at least one compensation log.", nameof(context));

        _compensateLog = RoutingSlip.CompensateLogs[^1];

        IActivityLog[] matchingActivityLogs = RoutingSlip.ActivityLogs
            .Where(x => x.ExecutionId == _compensateLog.ExecutionId)
            .Take(2)
            .ToArray();
        if (matchingActivityLogs.Length != 1)
            throw new RoutingSlipException($"The compensation log must have exactly one matching activity log entry: {_compensateLog.ExecutionId}");

        _activityLog = matchingActivityLogs[0];

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

    /// <summary>Reports successful compensation.</summary>
    /// <returns>A compensation result that removes the newest compensation entry.</returns>
    public CompensationResult Compensated()
    {
        return new CompensatedCompensationResult<TLog>(this, Publisher, _compensateLog, RoutingSlip);
    }

    /// <summary>Reports successful compensation and updates routing-slip variables.</summary>
    /// <param name="values">The object's public values merged into routing-slip state.</param>
    /// <returns>A compensation result containing the variable updates.</returns>
    public CompensationResult Compensated(object values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var result = new CompensatedCompensationResult<TLog>(this, Publisher, _compensateLog, RoutingSlip);

        result.SetVariables(values);

        return result;
    }

    /// <summary>Reports successful compensation and updates routing-slip variables.</summary>
    /// <param name="variables">The variable entries merged into routing-slip state.</param>
    /// <returns>A compensation result containing the variable updates.</returns>
    public CompensationResult Compensated(IDictionary<string, object> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);

        var result = new CompensatedCompensationResult<TLog>(this, Publisher, _compensateLog, RoutingSlip);

        result.SetVariables(variables);

        return result;
    }

    /// <summary>Reports compensation failure with the default routing-slip failure.</summary>
    /// <returns>A compensation-failure result containing the default failure.</returns>
    public CompensationResult Failed()
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

    static ConsumeContext<IRoutingSlip> RequireContext(ConsumeContext<IRoutingSlip> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context;
    }
}
