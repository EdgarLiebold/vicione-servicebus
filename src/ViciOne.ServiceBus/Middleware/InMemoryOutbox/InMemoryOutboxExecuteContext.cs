using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>Buffers outgoing activity operations while preserving execution arguments and results.</summary>
/// <typeparam name="TArguments">The activity argument contract.</typeparam>
internal sealed class InMemoryOutboxExecuteContext<TArguments> :
    InMemoryOutboxActivityContextProxy,
    ExecuteContext<TArguments>
    where TArguments : class
{
    readonly ExecuteContext<TArguments> _context;

    /// <summary>Initializes an in-memory outbox over an activity execution context.</summary>
    /// <param name="context">The execution context whose outgoing operations are buffered.</param>
    public InMemoryOutboxExecuteContext(ExecuteContext<TArguments> context)
        : base(context ?? throw new ArgumentNullException(nameof(context)))
    {
        _context = context;
    }

    /// <summary>Reports successful completion.</summary>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult Completed()
    {
        return _context.Completed();
    }

    /// <summary>Reports successful completion.</summary>
    /// <param name="callback">The callback that configures completion behavior.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult Completed(ConfigureCompletedActivityOptionsCallback callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return _context.Completed(callback);
    }

    /// <summary>Completes the activity and updates routing-slip variables.</summary>
    /// <param name="variables">The routing-slip variables to add or update.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult CompletedWithVariables(IEnumerable<KeyValuePair<string, object>> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);
        return _context.CompletedWithVariables(variables);
    }

    /// <summary>Completes the activity and updates routing-slip variables.</summary>
    /// <param name="variables">An object whose properties add or update routing-slip variables.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult CompletedWithVariables(object variables)
    {
        ArgumentNullException.ThrowIfNull(variables);
        return _context.CompletedWithVariables(variables);
    }

    /// <summary>Reports successful completion.</summary>
    /// <typeparam name="TLog">The compensation log contract.</typeparam>
    /// <param name="log">The compensation log to record.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult Completed<TLog>(TLog log)
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(log);
        return _context.Completed(log);
    }

    /// <summary>Reports successful completion.</summary>
    /// <typeparam name="TLog">The compensation log contract.</typeparam>
    /// <param name="log">The compensation log to record.</param>
    /// <param name="callback">The callback that configures completion behavior.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult Completed<TLog>(TLog log, ConfigureCompletedActivityOptionsCallback callback)
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(callback);
        return _context.Completed(log, callback);
    }

    /// <summary>Reports successful completion.</summary>
    /// <typeparam name="TLog">The compensation log contract.</typeparam>
    /// <param name="logValues">The values used to initialize the compensation log.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult Completed<TLog>(object logValues)
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(logValues);
        return _context.Completed<TLog>(logValues);
    }

    /// <summary>Reports successful completion.</summary>
    /// <typeparam name="TLog">The compensation log contract.</typeparam>
    /// <param name="logValues">The values used to initialize the compensation log.</param>
    /// <param name="callback">The callback that configures completion behavior.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult Completed<TLog>(object logValues, ConfigureCompletedActivityOptionsCallback callback)
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(logValues);
        ArgumentNullException.ThrowIfNull(callback);
        return _context.Completed<TLog>(logValues, callback);
    }

    /// <summary>Completes the activity with a compensation log and updated routing-slip variables.</summary>
    /// <typeparam name="TLog">The compensation log contract.</typeparam>
    /// <param name="log">The compensation log to record.</param>
    /// <param name="variables">An object whose properties add or update routing-slip variables.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult CompletedWithVariables<TLog>(TLog log, object variables)
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(variables);
        return _context.CompletedWithVariables(log, variables);
    }

    /// <summary>Completes the activity with a mapped compensation log and updated routing-slip variables.</summary>
    /// <typeparam name="TLog">The compensation log contract.</typeparam>
    /// <param name="logValues">The values used to initialize the compensation log.</param>
    /// <param name="variables">An object whose properties add or update routing-slip variables.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult CompletedWithVariables<TLog>(object logValues, object variables)
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(logValues);
        ArgumentNullException.ThrowIfNull(variables);
        return _context.CompletedWithVariables<TLog>(logValues, variables);
    }

    /// <summary>Completes the activity with a compensation log and updated routing-slip variables.</summary>
    /// <typeparam name="TLog">The compensation log contract.</typeparam>
    /// <param name="log">The compensation log to record.</param>
    /// <param name="variables">The routing-slip variables to add or update.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult CompletedWithVariables<TLog>(TLog log, IEnumerable<KeyValuePair<string, object>> variables)
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(variables);
        return _context.CompletedWithVariables(log, variables);
    }

    /// <summary>Replaces the remaining itinerary through the supplied builder callback.</summary>
    /// <param name="buildItinerary">The callback that rebuilds the remaining itinerary.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult ReviseItinerary(Action<IItineraryBuilder> buildItinerary)
    {
        ArgumentNullException.ThrowIfNull(buildItinerary);
        return _context.ReviseItinerary(buildItinerary);
    }

    /// <summary>Replaces the remaining itinerary and records a compensation log.</summary>
    /// <typeparam name="TLog">The compensation log contract.</typeparam>
    /// <param name="log">The compensation log to record.</param>
    /// <param name="buildItinerary">The callback that rebuilds the remaining itinerary.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult ReviseItinerary<TLog>(TLog log, Action<IItineraryBuilder> buildItinerary)
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(buildItinerary);
        return _context.ReviseItinerary(log, buildItinerary);
    }

    /// <summary>Replaces the remaining itinerary, records a compensation log, and updates variables.</summary>
    /// <typeparam name="TLog">The compensation log contract.</typeparam>
    /// <param name="log">The compensation log to record.</param>
    /// <param name="variables">An object whose properties add or update routing-slip variables.</param>
    /// <param name="buildItinerary">The callback that rebuilds the remaining itinerary.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult ReviseItinerary<TLog>(TLog log, object variables, Action<IItineraryBuilder> buildItinerary)
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(variables);
        ArgumentNullException.ThrowIfNull(buildItinerary);
        return _context.ReviseItinerary(log, variables, buildItinerary);
    }

    /// <summary>Replaces the remaining itinerary, records a compensation log, and updates variables.</summary>
    /// <typeparam name="TLog">The compensation log contract.</typeparam>
    /// <param name="log">The compensation log to record.</param>
    /// <param name="variables">The routing-slip variables to add or update.</param>
    /// <param name="buildItinerary">The callback that rebuilds the remaining itinerary.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult ReviseItinerary<TLog>(TLog log, IEnumerable<KeyValuePair<string, object>> variables, Action<IItineraryBuilder> buildItinerary)
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(variables);
        ArgumentNullException.ThrowIfNull(buildItinerary);
        return _context.ReviseItinerary(log, variables, buildItinerary);
    }

    /// <summary>Terminates the routing slip successfully and discards the remaining itinerary.</summary>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult Terminate()
    {
        return _context.Terminate();
    }

    /// <summary>Terminates the routing slip successfully, updates variables, and discards the remaining itinerary.</summary>
    /// <param name="variables">An object whose properties add or update routing-slip variables.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult Terminate(object variables)
    {
        ArgumentNullException.ThrowIfNull(variables);
        return _context.Terminate(variables);
    }

    /// <summary>Terminates the routing slip successfully, updates variables, and discards the remaining itinerary.</summary>
    /// <param name="variables">The routing-slip variables to add or update.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult Terminate(IEnumerable<KeyValuePair<string, object>> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);
        return _context.Terminate(variables);
    }

    /// <summary>Marks the activity as faulted using the context's current exception state.</summary>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult Faulted()
    {
        return _context.Faulted();
    }

    /// <summary>Marks the activity as faulted with a specific exception.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult Faulted(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return _context.Faulted(exception);
    }

    /// <summary>Marks the activity as faulted and applies fault-result configuration.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="callback">The callback that configures fault behavior.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult Faulted(Exception exception, ConfigureFaultedActivityOptionsCallback callback)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(callback);
        return _context.Faulted(exception, callback);
    }

    /// <summary>Reports a fault together with the supplied routing-slip variables.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="variables">An object whose properties add or update routing-slip variables.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult FaultedWithVariables(Exception exception, object variables)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(variables);
        return _context.FaultedWithVariables(exception, variables);
    }

    /// <summary>Reports a fault together with the supplied routing-slip variables.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="variables">The routing-slip variables to add or update.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult FaultedWithVariables(Exception exception, IEnumerable<KeyValuePair<string, object>> variables)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(variables);
        return _context.FaultedWithVariables(exception, variables);
    }

    /// <summary>Gets or sets the result; the value is unset until the activity completes.</summary>
    public ExecutionResult? Result
    {
        get => _context.Result;
        set => _context.Result = value;
    }

    /// <summary>Gets the arguments supplied to the activity.</summary>
    public TArguments Arguments => _context.Arguments;

    /// <summary>Creates the host context for a concrete activity implementation.</summary>
    /// <typeparam name="TActivity">The activity implementation type.</typeparam>
    /// <param name="activity">The activity instance to execute.</param>
    /// <returns>A host context backed by this outbox execution context.</returns>
    public ExecuteActivityContext<TActivity, TArguments> CreateActivityContext<TActivity>(TActivity activity)
        where TActivity : class
    {
        ArgumentNullException.ThrowIfNull(activity);
        return new HostExecuteActivityContext<TActivity, TArguments>(activity, this);
    }
}
