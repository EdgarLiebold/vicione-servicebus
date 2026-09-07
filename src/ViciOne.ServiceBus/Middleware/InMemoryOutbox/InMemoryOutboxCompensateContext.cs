using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>Buffers outgoing compensation operations while preserving the activity log and result.</summary>
/// <typeparam name="TLog">The compensation log contract.</typeparam>
internal sealed class InMemoryOutboxCompensateContext<TLog> :
    InMemoryOutboxActivityContextProxy,
    CompensateContext<TLog>
    where TLog : class
{
    readonly CompensateContext<TLog> _context;

    /// <summary>Initializes an in-memory outbox over a compensation context.</summary>
    /// <param name="context">The compensation context whose outgoing operations are buffered.</param>
    public InMemoryOutboxCompensateContext(CompensateContext<TLog> context)
        : base(context)
    {
        _context = context;
    }

    CompensationResult CompensateContext.Compensated()
    {
        return _context.Compensated();
    }

    CompensationResult CompensateContext.Compensated(object values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return _context.Compensated(values);
    }

    CompensationResult CompensateContext.Compensated(IDictionary<string, object> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);
        return _context.Compensated(variables);
    }

    CompensationResult CompensateContext.Failed()
    {
        return _context.Failed();
    }

    CompensationResult CompensateContext.Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return _context.Failed(exception);
    }

    CompensationResult? CompensateContext.Result
    {
        get => _context.Result;
        set => _context.Result = value;
    }

    TLog CompensateContext<TLog>.Log => _context.Log;

    CompensateActivityContext<TActivity, TLog> CompensateContext<TLog>.CreateActivityContext<TActivity>(TActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return new HostCompensateActivityContext<TActivity, TLog>(activity, this);
    }
}
