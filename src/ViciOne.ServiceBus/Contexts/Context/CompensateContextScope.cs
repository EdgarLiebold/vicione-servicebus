using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Context;

/// <summary>
/// Provides a compensate context scope implementation.
/// </summary>
/// <typeparam name="TLog">The t log type.</typeparam>
public class CompensateContextScope<TLog> :
    ActivityContextScope,
    CompensateContext<TLog>
    where TLog : class
{
    readonly CompensateContext<TLog> _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="payloads">The payloads value.</param>
    public CompensateContextScope(CompensateContext<TLog> context, params object[] payloads)
        : base(context, payloads)
    {
        _context = context;
    }

    /// <summary>
    /// Gets the log value.
    /// </summary>
    public TLog Log => _context.Log;

    /// <summary>
    /// Creates activity context.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <param name="activity">The activity value.</param>
    /// <returns>The result of the operation.</returns>
    public CompensateActivityContext<TActivity, TLog> CreateActivityContext<TActivity>(TActivity activity)
        where TActivity : class
    {
        return new HostCompensateActivityContext<TActivity, TLog>(activity, this);
    }

    /// <summary>
    /// Performs the compensated operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public CompensationResult Compensated()
    {
        return _context.Compensated();
    }

    /// <summary>
    /// Performs the compensated operation.
    /// </summary>
    /// <param name="values">The values value.</param>
    /// <returns>The result of the operation.</returns>
    public CompensationResult Compensated(object values)
    {
        return _context.Compensated(values);
    }

    /// <summary>
    /// Performs the compensated operation.
    /// </summary>
    /// <param name="variables">The variables value.</param>
    /// <returns>The result of the operation.</returns>
    public CompensationResult Compensated(IDictionary<string, object> variables)
    {
        return _context.Compensated(variables);
    }

    /// <summary>
    /// Performs the failed operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public CompensationResult Failed()
    {
        return _context.Failed();
    }

    /// <summary>
    /// Performs the failed operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public CompensationResult Failed(Exception exception)
    {
        return _context.Failed(exception);
    }

    /// <summary>
    /// Gets or sets the result value.
    /// </summary>
    public CompensationResult Result
    {
        get => _context.Result;
        set => _context.Result = value;
    }
}
