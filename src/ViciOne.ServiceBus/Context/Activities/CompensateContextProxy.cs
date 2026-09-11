using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Context;

/// <summary>Forwards compensation operations while optionally projecting a replacement log.</summary>
/// <typeparam name="TLog">The compensation log contract.</typeparam>
public class CompensateContextProxy<TLog> :
    ActivityContextProxy,
    CompensateContext<TLog>
    where TLog : class
{
    readonly CompensateContext<TLog> _context;
    readonly TLog _log;

    /// <summary>Creates a compensation-context view with an explicit log.</summary>
    /// <param name="context">The compensation context to wrap.</param>
    /// <param name="log">The compensation log exposed by the proxy.</param>
    public CompensateContextProxy(CompensateContext<TLog> context, TLog log)
        : base(context ?? throw new ArgumentNullException(nameof(context)))
    {
        _context = context;
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    /// <summary>Creates a forwarding view over a compensation context.</summary>
    /// <param name="context">The compensation context to wrap.</param>
    protected CompensateContextProxy(CompensateContext<TLog> context)
        : base(context ?? throw new ArgumentNullException(nameof(context)))
    {
        _context = context;
        _log = context.Log;
    }

    TLog CompensateContext<TLog>.Log => _log;

    CompensateActivityContext<TActivity, TLog> CompensateContext<TLog>.CreateActivityContext<TActivity>(TActivity activity)
    {
        return new HostCompensateActivityContext<TActivity, TLog>(activity, this);
    }

    CompensationResult CompensateContext.Compensated()
    {
        return _context.Compensated();
    }

    CompensationResult CompensateContext.Compensated(object values)
    {
        return _context.Compensated(values);
    }

    CompensationResult CompensateContext.Compensated(IDictionary<string, object> variables)
    {
        return _context.Compensated(variables);
    }

    CompensationResult CompensateContext.Failed()
    {
        return _context.Failed();
    }

    CompensationResult CompensateContext.Failed(Exception exception)
    {
        return _context.Failed(exception);
    }

    CompensationResult? CompensateContext.Result
    {
        get => _context.Result;
        set => _context.Result = value;
    }
}
