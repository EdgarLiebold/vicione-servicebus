using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Context;

/// <summary>Defines the lifetime scope for compensate context.</summary>
/// <typeparam name="TLog">The log type.</typeparam>
public class CompensateContextScope<TLog> :
    ActivityContextScope,
    CompensateContext<TLog>
    where TLog : class
{
    readonly CompensateContext<TLog> _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="payloads">The payloads.</param>
    public CompensateContextScope(CompensateContext<TLog> context, params object[] payloads)
        : base(context, payloads)
    {
        _context = context;
    }

    /// <summary>Gets the log.</summary>
    public TLog Log => _context.Log;

    /// <summary>Creates activity context.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <param name="activity">The activity.</param>
    /// <returns>The created activity context.</returns>
    public CompensateActivityContext<TActivity, TLog> CreateActivityContext<TActivity>(TActivity activity)
        where TActivity : class
    {
        return new HostCompensateActivityContext<TActivity, TLog>(activity, this);
    }

    /// <summary>Completes compensation successfully.</summary>
    /// <returns>The compensation result produced by the operation.</returns>
    public CompensationResult Compensated()
    {
        return _context.Compensated();
    }

    /// <summary>Completes compensation successfully and updates routing-slip variables.</summary>
    /// <param name="values">The values.</param>
    /// <returns>The compensation result produced by the operation.</returns>
    public CompensationResult Compensated(object values)
    {
        return _context.Compensated(values);
    }

    /// <summary>Completes compensation successfully and updates routing-slip variables.</summary>
    /// <param name="variables">The variables.</param>
    /// <returns>The compensation result produced by the operation.</returns>
    public CompensationResult Compensated(IDictionary<string, object> variables)
    {
        return _context.Compensated(variables);
    }

    /// <summary>Reports a failed result.</summary>
    /// <returns>The compensation result produced by the operation.</returns>
    public CompensationResult Failed()
    {
        return _context.Failed();
    }

    /// <summary>Reports a failed result.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The compensation result produced by the operation.</returns>
    public CompensationResult Failed(Exception exception)
    {
        return _context.Failed(exception);
    }

    /// <summary>Gets or sets the result; the value is unset until compensation completes.</summary>
    public CompensationResult? Result
    {
        get => _context.Result;
        set => _context.Result = value;
    }
}
