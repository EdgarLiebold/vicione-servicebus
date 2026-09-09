using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Context;

/// <summary>Adds scoped payloads to an execute activity context.</summary>
/// <typeparam name="TArguments">The activity argument contract.</typeparam>
public class ExecuteContextScope<TArguments> :
    ActivityContextScope,
    ExecuteContext<TArguments>
    where TArguments : class
{
    readonly ExecuteContext<TArguments> _context;

    /// <summary>Creates an execute scope with additional payloads.</summary>
    /// <param name="context">The execute context to wrap.</param>
    /// <param name="payloads">The payloads visible within this execute scope.</param>
    public ExecuteContextScope(ExecuteContext<TArguments> context, params object[] payloads)
        : base(context, payloads)
    {
        _context = context;
        Arguments = context.Arguments;
    }

    /// <summary>Gets the activity arguments.</summary>
    public TArguments Arguments { get; }

    /// <summary>Gets or sets the result; the value is unset until the activity completes.</summary>
    public ExecutionResult? Result
    {
        get => _context.Result;
        set => _context.Result = value;
    }

    /// <summary>Creates activity context.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <param name="activity">The activity.</param>
    /// <returns>The created activity context.</returns>
    public ExecuteActivityContext<TActivity, TArguments> CreateActivityContext<TActivity>(TActivity activity)
        where TActivity : class
    {
        return new HostExecuteActivityContext<TActivity, TArguments>(activity, this);
    }

    /// <summary>Reports successful completion.</summary>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult Completed()
    {
        return _context.Completed();
    }

    /// <summary>Reports successful completion.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult Completed(ConfigureCompletedActivityOptionsCallback callback)
    {
        return _context.Completed(callback);
    }

    /// <summary>Completes the activity and updates routing-slip variables.</summary>
    /// <param name="variables">The variables.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult CompletedWithVariables(IEnumerable<KeyValuePair<string, object>> variables)
    {
        return _context.CompletedWithVariables(variables);
    }

    /// <summary>Completes the activity and updates routing-slip variables.</summary>
    /// <param name="variables">The variables.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult CompletedWithVariables(object variables)
    {
        return _context.CompletedWithVariables(variables);
    }

    /// <summary>Reports successful completion.</summary>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="log">The log.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult Completed<TLog>(TLog log)
        where TLog : class
    {
        return _context.Completed(log);
    }

    /// <summary>Reports successful completion.</summary>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="log">The log.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult Completed<TLog>(TLog log, ConfigureCompletedActivityOptionsCallback callback)
        where TLog : class
    {
        return _context.Completed(log, callback);
    }

    /// <summary>Reports successful completion.</summary>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="logValues">The log values.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult Completed<TLog>(object logValues)
        where TLog : class
    {
        return _context.Completed<TLog>(logValues);
    }

    /// <summary>Reports successful completion.</summary>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="logValues">The log values.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult Completed<TLog>(object logValues, ConfigureCompletedActivityOptionsCallback callback)
        where TLog : class
    {
        return _context.Completed<TLog>(logValues, callback);
    }

    /// <summary>Completes the activity with a compensation log and updated routing-slip variables.</summary>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="log">The log.</param>
    /// <param name="variables">The variables.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult CompletedWithVariables<TLog>(TLog log, object variables)
        where TLog : class
    {
        return _context.CompletedWithVariables(log, variables);
    }

    /// <summary>Completes the activity with a mapped compensation log and updated routing-slip variables.</summary>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="logValues">The log values.</param>
    /// <param name="variables">The variables.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult CompletedWithVariables<TLog>(object logValues, object variables)
        where TLog : class
    {
        return _context.CompletedWithVariables<TLog>(logValues, variables);
    }

    /// <summary>Completes the activity with a compensation log and updated routing-slip variables.</summary>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="log">The log.</param>
    /// <param name="variables">The variables.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult CompletedWithVariables<TLog>(TLog log, IEnumerable<KeyValuePair<string, object>> variables)
        where TLog : class
    {
        return _context.CompletedWithVariables(log, variables);
    }

    /// <summary>Replaces the remaining itinerary through the supplied builder callback.</summary>
    /// <param name="buildItinerary">The build itinerary.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult ReviseItinerary(Action<IItineraryBuilder> buildItinerary)
    {
        return _context.ReviseItinerary(buildItinerary);
    }

    /// <summary>Replaces the remaining itinerary and records a compensation log.</summary>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="log">The log.</param>
    /// <param name="buildItinerary">The build itinerary.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult ReviseItinerary<TLog>(TLog log, Action<IItineraryBuilder> buildItinerary)
        where TLog : class
    {
        return _context.ReviseItinerary(log, buildItinerary);
    }

    /// <summary>Replaces the remaining itinerary, records a compensation log, and updates variables.</summary>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="log">The log.</param>
    /// <param name="variables">The variables.</param>
    /// <param name="buildItinerary">The build itinerary.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult ReviseItinerary<TLog>(TLog log, object variables, Action<IItineraryBuilder> buildItinerary)
        where TLog : class
    {
        return _context.ReviseItinerary(log, variables, buildItinerary);
    }

    /// <summary>Replaces the remaining itinerary, records a compensation log, and updates variables.</summary>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="log">The log.</param>
    /// <param name="variables">The variables.</param>
    /// <param name="buildItinerary">The build itinerary.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult ReviseItinerary<TLog>(TLog log, IEnumerable<KeyValuePair<string, object>> variables, Action<IItineraryBuilder> buildItinerary)
        where TLog : class
    {
        return _context.ReviseItinerary(log, variables, buildItinerary);
    }

    /// <summary>Terminates the routing slip successfully and discards the remaining itinerary.</summary>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult Terminate()
    {
        return _context.Terminate();
    }

    /// <summary>Terminates the routing slip successfully, updates variables, and discards the remaining itinerary.</summary>
    /// <param name="variables">The variables.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult Terminate(object variables)
    {
        return _context.Terminate(variables);
    }

    /// <summary>Terminates the routing slip successfully, updates variables, and discards the remaining itinerary.</summary>
    /// <param name="variables">The variables.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult Terminate(IEnumerable<KeyValuePair<string, object>> variables)
    {
        return _context.Terminate(variables);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult Faulted()
    {
        return _context.Faulted();
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <param name="exception">The failure that faulted the activity execution.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult Faulted(Exception exception)
    {
        return _context.Faulted(exception);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <param name="exception">The failure that faulted the activity execution.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult Faulted(Exception exception, ConfigureFaultedActivityOptionsCallback callback)
    {
        return _context.Faulted(exception, callback);
    }

    /// <summary>Reports a fault together with the supplied routing-slip variables.</summary>
    /// <param name="exception">The failure that faulted the activity execution.</param>
    /// <param name="variables">The variables.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult FaultedWithVariables(Exception exception, object variables)
    {
        return _context.FaultedWithVariables(exception, variables);
    }

    /// <summary>Reports a fault together with the supplied routing-slip variables.</summary>
    /// <param name="exception">The failure that faulted the activity execution.</param>
    /// <param name="variables">The variables.</param>
    /// <returns>The execution result produced by the operation.</returns>
    public ExecutionResult FaultedWithVariables(Exception exception, IEnumerable<KeyValuePair<string, object>> variables)
    {
        return _context.FaultedWithVariables(exception, variables);
    }
}
