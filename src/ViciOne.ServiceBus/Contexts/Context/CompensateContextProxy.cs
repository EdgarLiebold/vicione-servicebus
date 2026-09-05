using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Context;

/// <summary>
/// Provides a compensate context proxy implementation.
/// </summary>
/// <typeparam name="TLog">The t log type.</typeparam>
public class CompensateContextProxy<TLog> :
    ActivityContextProxy,
    CompensateContext<TLog>
    where TLog : class
{
    readonly CompensateContext<TLog> _context;
    readonly TLog _log;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="log">The log value.</param>
    public CompensateContextProxy(CompensateContext<TLog> context, TLog log)
        : base(context)
    {
        _context = context;
        _log = log;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
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

    CompensationResult CompensateContext.Result
    {
        get => _context.Result;
        set => _context.Result = value;
    }
}
