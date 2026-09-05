using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>
/// Provides an in memory outbox compensate context implementation.
/// </summary>
/// <typeparam name="TLog">The t log type.</typeparam>
public class InMemoryOutboxCompensateContext<TLog> :
    InMemoryOutboxActivityContextProxy,
    CompensateContext<TLog>
    where TLog : class
{
    readonly CompensateContext<TLog> _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
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

    TLog CompensateContext<TLog>.Log => _context.Log;

    CompensateActivityContext<TActivity, TLog> CompensateContext<TLog>.CreateActivityContext<TActivity>(TActivity activity)
    {
        return new HostCompensateActivityContext<TActivity, TLog>(activity, this);
    }
}
