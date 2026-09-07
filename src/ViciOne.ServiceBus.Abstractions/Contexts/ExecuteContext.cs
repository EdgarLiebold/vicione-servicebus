using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes state for execute operations.</summary>
public interface ExecuteContext :
    ActivityContext
{
    /// <summary>Gets or sets the result; the value is unset until the activity completes.</summary>
    ExecutionResult? Result { get; set; }

    /// <summary>Completes the execution, without passing a compensating log entry.</summary>
    /// <returns>The execution result produced by the operation.</returns>
    ExecutionResult Completed();

    /// <summary>Completes the execution, without passing a compensating log entry.</summary>
    /// <param name="callback">The action that configures completion options.</param>
    /// <returns>The execution result produced by the operation.</returns>
    ExecutionResult Completed(ConfigureCompletedActivityOptionsCallback callback);

    /// <summary>Completes the execution, passing updated variables to the routing slip.</summary>
    /// <param name="variables">The variables.</param>
    /// <returns>The execution result produced by the operation.</returns>
    ExecutionResult CompletedWithVariables(IEnumerable<KeyValuePair<string, object>> variables);

    /// <summary>Completes the execution, passing updated variables to the routing slip.</summary>
    /// <param name="variables">The variables.</param>
    /// <returns>The execution result produced by the operation.</returns>
    ExecutionResult CompletedWithVariables(object variables);

    /// <summary>Completes the activity, passing a compensation log entry.</summary>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="log">The log.</param>
    /// <returns>The execution result produced by the operation.</returns>
    ExecutionResult Completed<TLog>(TLog log)
        where TLog : class;

    /// <summary>Completes the activity, passing a compensation log entry.</summary>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="log">The log.</param>
    /// <param name="callback">The action that configures completion options.</param>
    /// <returns>The execution result produced by the operation.</returns>
    ExecutionResult Completed<TLog>(TLog log, ConfigureCompletedActivityOptionsCallback callback)
        where TLog : class;

    /// <summary>Completes the activity, passing a compensation log entry.</summary>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="logValues">An object to initialize the log properties.</param>
    /// <returns>The execution result produced by the operation.</returns>
    ExecutionResult Completed<TLog>(object logValues)
        where TLog : class;

    /// <summary>Completes the activity, passing a compensation log entry.</summary>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="logValues">An object to initialize the log properties.</param>
    /// <param name="callback">The action that configures completion options.</param>
    /// <returns>The execution result produced by the operation.</returns>
    ExecutionResult Completed<TLog>(object logValues, ConfigureCompletedActivityOptionsCallback callback)
        where TLog : class;

    /// <summary>
    /// Completes the activity, passing a compensation log entry and additional variables to set on
    /// the routing slip.
    /// </summary>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="log">The log.</param>
    /// <param name="variables">An anonymous object of values to add/set as variables on the routing slip.</param>
    /// <returns>The execution result produced by the operation.</returns>
    ExecutionResult CompletedWithVariables<TLog>(TLog log, object variables)
        where TLog : class;

    /// <summary>
    /// Completes the activity, passing a compensation log entry and additional variables to set on
    /// the routing slip.
    /// </summary>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="logValues">The log values.</param>
    /// <param name="variables">An anonymous object of values to add/set as variables on the routing slip.</param>
    /// <returns>The execution result produced by the operation.</returns>
    ExecutionResult CompletedWithVariables<TLog>(object logValues, object variables)
        where TLog : class;

    /// <summary>
    /// Completes the activity, passing a compensation log entry and additional variables to set on
    /// the routing slip.
    /// </summary>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="log">The log.</param>
    /// <param name="variables">A sequence of variables to add, update, or remove on the routing slip.</param>
    /// <returns>The execution result produced by the operation.</returns>
    ExecutionResult CompletedWithVariables<TLog>(TLog log, IEnumerable<KeyValuePair<string, object>> variables)
        where TLog : class;

    /// <summary>Revises the remaining itinerary.</summary>
    /// <param name="buildItinerary">The action that builds the replacement itinerary.</param>
    /// <returns>The execution result produced by the operation.</returns>
    ExecutionResult ReviseItinerary(Action<IItineraryBuilder> buildItinerary);

    /// <summary>Revises the remaining itinerary and records a compensation log.</summary>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="log">The log.</param>
    /// <param name="buildItinerary">The action that builds the replacement itinerary.</param>
    /// <returns>The execution result produced by the operation.</returns>
    ExecutionResult ReviseItinerary<TLog>(TLog log, Action<IItineraryBuilder> buildItinerary)
        where TLog : class;

    /// <summary>Revises the remaining itinerary and records a compensation log and variables.</summary>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="log">The log.</param>
    /// <param name="variables">The variables.</param>
    /// <param name="buildItinerary">The action that builds the replacement itinerary.</param>
    /// <returns>The execution result produced by the operation.</returns>
    ExecutionResult ReviseItinerary<TLog>(TLog log, object variables, Action<IItineraryBuilder> buildItinerary)
        where TLog : class;

    /// <summary>Revises the remaining itinerary and records a compensation log and variables.</summary>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="log">The log.</param>
    /// <param name="variables">The variables.</param>
    /// <param name="buildItinerary">The action that builds the replacement itinerary.</param>
    /// <returns>The execution result produced by the operation.</returns>
    ExecutionResult ReviseItinerary<TLog>(TLog log, IEnumerable<KeyValuePair<string, object>> variables,
        Action<IItineraryBuilder> buildItinerary)
        where TLog : class;

    /// <summary>Terminates the routing slip successfully and discards all remaining activities.</summary>
    /// <returns>The execution result produced by the operation.</returns>
    ExecutionResult Terminate();

    /// <summary>Terminates the routing slip successfully, updates variables, and discards all remaining activities.</summary>
    /// <param name="variables">The variables.</param>
    /// <returns>The execution result produced by the operation.</returns>
    ExecutionResult Terminate(object variables);

    /// <summary>Terminates the routing slip successfully, updates variables, and discards all remaining activities.</summary>
    /// <param name="variables">The variables.</param>
    /// <returns>The execution result produced by the operation.</returns>
    ExecutionResult Terminate(IEnumerable<KeyValuePair<string, object>> variables);

    /// <summary>Faults the activity without a specific exception and starts compensation.</summary>
    /// <returns>The execution result produced by the operation.</returns>
    ExecutionResult Faulted();

    /// <summary>Faults the activity with an exception and starts compensation.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The execution result produced by the operation.</returns>
    ExecutionResult Faulted(Exception exception);

    /// <summary>Faults the activity, configures fault options, and starts compensation.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="callback">The action that configures fault and compensation options.</param>
    /// <returns>The execution result produced by the operation.</returns>
    ExecutionResult Faulted(Exception exception, ConfigureFaultedActivityOptionsCallback callback);

    /// <summary>Faults the activity, updates routing-slip variables, and starts compensation.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="variables">An anonymous object of values to add/set as variables on the routing slip.</param>
    /// <returns>The execution result produced by the operation.</returns>
    ExecutionResult FaultedWithVariables(Exception exception, object variables);

    /// <summary>Faults the activity, updates routing-slip variables, and starts compensation.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="variables">A sequence of variables to add, update, or remove on the routing slip.</param>
    /// <returns>The execution result produced by the operation.</returns>
    ExecutionResult FaultedWithVariables(Exception exception, IEnumerable<KeyValuePair<string, object>> variables);
}


/// <summary>Exposes state for execute operations.</summary>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public interface ExecuteContext<out TArguments> :
    ExecuteContext
    where TArguments : class
{
    /// <summary>The arguments from the routing slip for this activity.</summary>
    TArguments Arguments { get; }

    /// <summary>Creates activity context.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <param name="activity">The activity.</param>
    /// <returns>The created activity context.</returns>
    ExecuteActivityContext<TActivity, TArguments> CreateActivityContext<TActivity>(TActivity activity)
        where TActivity : class;
}
