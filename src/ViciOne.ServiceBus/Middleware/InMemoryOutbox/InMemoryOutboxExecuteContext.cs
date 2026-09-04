using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>
/// Provides an in memory outbox execute context implementation.
/// </summary>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
public class InMemoryOutboxExecuteContext<TArguments> :
    InMemoryOutboxCourierContextProxy,
    ExecuteContext<TArguments>
    where TArguments : class
{
    readonly ExecuteContext<TArguments> _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public InMemoryOutboxExecuteContext(ExecuteContext<TArguments> context)
        : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Performs the completed operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult Completed()
    {
        return _context.Completed();
    }

    /// <summary>
    /// Performs the completed operation.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult Completed(ConfigureCompletedActivityOptionsCallback callback)
    {
        return _context.Completed(callback);
    }

    /// <summary>
    /// Performs the completed with variables operation.
    /// </summary>
    /// <param name="variables">The variables value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult CompletedWithVariables(IEnumerable<KeyValuePair<string, object>> variables)
    {
        return _context.CompletedWithVariables(variables);
    }

    /// <summary>
    /// Performs the completed with variables operation.
    /// </summary>
    /// <param name="variables">The variables value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult CompletedWithVariables(object variables)
    {
        return _context.CompletedWithVariables(variables);
    }

    /// <summary>
    /// Performs the completed operation.
    /// </summary>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="log">The log value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult Completed<TLog>(TLog log)
        where TLog : class
    {
        return _context.Completed(log);
    }

    /// <summary>
    /// Performs the completed operation.
    /// </summary>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="log">The log value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult Completed<TLog>(TLog log, ConfigureCompletedActivityOptionsCallback callback)
        where TLog : class
    {
        return _context.Completed(log, callback);
    }

    /// <summary>
    /// Performs the completed operation.
    /// </summary>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="logValues">The log values value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult Completed<TLog>(object logValues)
        where TLog : class
    {
        return _context.Completed<TLog>(logValues);
    }

    /// <summary>
    /// Performs the completed operation.
    /// </summary>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="logValues">The log values value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult Completed<TLog>(object logValues, ConfigureCompletedActivityOptionsCallback callback)
        where TLog : class
    {
        return _context.Completed<TLog>(logValues, callback);
    }

    /// <summary>
    /// Performs the completed with variables operation.
    /// </summary>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="log">The log value.</param>
    /// <param name="variables">The variables value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult CompletedWithVariables<TLog>(TLog log, object variables)
        where TLog : class
    {
        return _context.CompletedWithVariables(log, variables);
    }

    /// <summary>
    /// Performs the completed with variables operation.
    /// </summary>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="logValues">The log values value.</param>
    /// <param name="variables">The variables value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult CompletedWithVariables<TLog>(object logValues, object variables)
        where TLog : class
    {
        return _context.CompletedWithVariables<TLog>(logValues, variables);
    }

    /// <summary>
    /// Performs the completed with variables operation.
    /// </summary>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="log">The log value.</param>
    /// <param name="variables">The variables value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult CompletedWithVariables<TLog>(TLog log, IEnumerable<KeyValuePair<string, object>> variables)
        where TLog : class
    {
        return _context.CompletedWithVariables(log, variables);
    }

    /// <summary>
    /// Performs the revise itinerary operation.
    /// </summary>
    /// <param name="buildItinerary">The build itinerary value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult ReviseItinerary(Action<IItineraryBuilder> buildItinerary)
    {
        return _context.ReviseItinerary(buildItinerary);
    }

    /// <summary>
    /// Performs the revise itinerary operation.
    /// </summary>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="log">The log value.</param>
    /// <param name="buildItinerary">The build itinerary value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult ReviseItinerary<TLog>(TLog log, Action<IItineraryBuilder> buildItinerary)
        where TLog : class
    {
        return _context.ReviseItinerary(log, buildItinerary);
    }

    /// <summary>
    /// Performs the revise itinerary operation.
    /// </summary>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="log">The log value.</param>
    /// <param name="variables">The variables value.</param>
    /// <param name="buildItinerary">The build itinerary value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult ReviseItinerary<TLog>(TLog log, object variables, Action<IItineraryBuilder> buildItinerary)
        where TLog : class
    {
        return _context.ReviseItinerary(log, variables, buildItinerary);
    }

    /// <summary>
    /// Performs the revise itinerary operation.
    /// </summary>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="log">The log value.</param>
    /// <param name="variables">The variables value.</param>
    /// <param name="buildItinerary">The build itinerary value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult ReviseItinerary<TLog>(TLog log, IEnumerable<KeyValuePair<string, object>> variables, Action<IItineraryBuilder> buildItinerary)
        where TLog : class
    {
        return _context.ReviseItinerary(log, variables, buildItinerary);
    }

    /// <summary>
    /// Performs the terminate operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult Terminate()
    {
        return _context.Terminate();
    }

    /// <summary>
    /// Performs the terminate operation.
    /// </summary>
    /// <param name="variables">The variables value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult Terminate(object variables)
    {
        return _context.Terminate(variables);
    }

    /// <summary>
    /// Performs the terminate operation.
    /// </summary>
    /// <param name="variables">The variables value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult Terminate(IEnumerable<KeyValuePair<string, object>> variables)
    {
        return _context.Terminate(variables);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult Faulted()
    {
        return _context.Faulted();
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult Faulted(Exception exception)
    {
        return _context.Faulted(exception);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult Faulted(Exception exception, ConfigureFaultedActivityOptionsCallback callback)
    {
        return _context.Faulted(exception, callback);
    }

    /// <summary>
    /// Performs the faulted with variables operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="variables">The variables value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult FaultedWithVariables(Exception exception, object variables)
    {
        return _context.FaultedWithVariables(exception, variables);
    }

    /// <summary>
    /// Performs the faulted with variables operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="variables">The variables value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult FaultedWithVariables(Exception exception, IEnumerable<KeyValuePair<string, object>> variables)
    {
        return _context.FaultedWithVariables(exception, variables);
    }

    /// <summary>
    /// Gets or sets the result value.
    /// </summary>
    public ExecutionResult Result
    {
        get => _context.Result;
        set => _context.Result = value;
    }

    /// <summary>
    /// Gets the arguments value.
    /// </summary>
    public TArguments Arguments => _context.Arguments;

    /// <summary>
    /// Creates activity context.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <param name="activity">The activity value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecuteActivityContext<TActivity, TArguments> CreateActivityContext<TActivity>(TActivity activity)
        where TActivity : class, IExecuteActivity<TArguments>
    {
        return new HostExecuteActivityContext<TActivity, TArguments>(activity, this);
    }
}
