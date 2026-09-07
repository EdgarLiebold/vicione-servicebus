using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Context;

/// <summary>Forwards compensate context operations to an underlying context.</summary>
/// <typeparam name="TLog">The log type.</typeparam>
public class CompensateContextProxy<TLog> :
    ActivityContextProxy,
    CompensateContext<TLog>
    where TLog : class
{
    readonly CompensateContext<TLog> _context;
    readonly TLog _log;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="log">The log.</param>
    public CompensateContextProxy(CompensateContext<TLog> context, TLog log)
        : base(context)
    {
        _context = context;
        _log = log;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    protected CompensateContextProxy(CompensateContext<TLog> context)
        : base(context)
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
