using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Results;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Binds the next routing-slip itinerary entry to its deserialized execution arguments and result factories.</summary>
/// <typeparam name="TArguments">The execution-arguments contract.</typeparam>
internal sealed class HostExecuteContext<TArguments> :
    BaseCourierContext,
    ExecuteContext<TArguments>
    where TArguments : class
{
    readonly Activity _activity;
    readonly Uri? _compensationAddress;

    /// <summary>Creates an execution context for the first activity in the received routing slip.</summary>
    /// <param name="compensationAddress">The endpoint that compensates this activity after successful execution, when available.</param>
    /// <param name="context">The received routing slip and transport context.</param>
    public HostExecuteContext(Uri? compensationAddress, ConsumeContext<RoutingSlip> context)
        : base(context)
    {
        _compensationAddress = compensationAddress;

        if (RoutingSlip.Itinerary.Count == 0)
            throw new ArgumentException("The routing slip must contain at least one activity.", nameof(context));

        _activity = RoutingSlip.Itinerary[0];
        Arguments = RoutingSlip.GetActivityArguments<TArguments>();
    }

    /// <summary>Gets the logical name of the current itinerary entry.</summary>
    public override string ActivityName => _activity.Name;
    /// <summary>Gets the deserialized arguments for the current itinerary entry.</summary>
    public TArguments Arguments { get; }

    /// <summary>Binds a resolved activity instance to this execution context.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <param name="activity">The activity instance to expose to middleware.</param>
    /// <returns>An execution context containing the activity instance and current arguments.</returns>
    public ExecuteActivityContext<TActivity, TArguments> CreateActivityContext<TActivity>(TActivity activity)
        where TActivity : class
    {
        return new HostExecuteActivityContext<TActivity, TArguments>(activity, this);
    }

    /// <summary>Gets or sets the result; the value is unset until the activity completes.</summary>
    public ExecutionResult? Result { get; set; }
    /// <summary>Reports successful completion.</summary>
    /// <returns>A completion result that advances the remaining itinerary.</returns>
    public ExecutionResult Completed()
    {
        return new CompletedExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);
    }

    /// <summary>Reports successful completion.</summary>
    /// <param name="callback">The callback that adds variables or modifies the remaining itinerary.</param>
    /// <returns>The configured completion result.</returns>
    public ExecutionResult Completed(ConfigureCompletedActivityOptionsCallback callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        var result = new CompletedExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);

        callback(result);

        return result;
    }

    /// <summary>Reports successful completion.</summary>
    /// <typeparam name="TLog">The compensation-log contract.</typeparam>
    /// <param name="log">The compensation log recorded for this execution.</param>
    /// <returns>A completion result that appends the compensation log and advances the itinerary.</returns>
    public ExecutionResult Completed<TLog>(TLog log)
        where TLog : class
    {
        if (log == null)
            throw new ArgumentNullException(nameof(log));

        if (_compensationAddress == null)
            throw new InvalidCompensationAddressException(_compensationAddress);

        var result = new CompletedExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);

        result.SetLog(log);

        return result;
    }

    /// <summary>Reports successful completion.</summary>
    /// <typeparam name="TLog">The compensation-log contract.</typeparam>
    /// <param name="log">The compensation log recorded for this execution.</param>
    /// <param name="callback">The callback that adds variables or modifies the remaining itinerary.</param>
    /// <returns>The configured completion result containing the compensation log.</returns>
    public ExecutionResult Completed<TLog>(TLog log, ConfigureCompletedActivityOptionsCallback callback)
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(callback);

        if (_compensationAddress == null)
            throw new InvalidCompensationAddressException(_compensationAddress);

        var result = new CompletedExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);

        result.SetLog(log);

        callback(result);

        return result;
    }

    /// <summary>Reports successful completion.</summary>
    /// <typeparam name="TLog">The compensation-log contract to initialize.</typeparam>
    /// <param name="logValues">The values used to initialize the compensation log.</param>
    /// <returns>A completion result that appends the initialized compensation log and advances the itinerary.</returns>
    public ExecutionResult Completed<TLog>(object logValues)
        where TLog : class
    {
        if (logValues == null)
            throw new ArgumentNullException(nameof(logValues));

        if (_compensationAddress == null)
            throw new InvalidCompensationAddressException(_compensationAddress);

        var result = new CompletedExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);

        result.SetLog(logValues);

        return result;
    }

    /// <summary>Reports successful completion.</summary>
    /// <typeparam name="TLog">The compensation-log contract to initialize.</typeparam>
    /// <param name="logValues">The values used to initialize the compensation log.</param>
    /// <param name="callback">The callback that adds variables or modifies the remaining itinerary.</param>
    /// <returns>The configured completion result containing the initialized compensation log.</returns>
    public ExecutionResult Completed<TLog>(object logValues, ConfigureCompletedActivityOptionsCallback callback)
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(logValues);
        ArgumentNullException.ThrowIfNull(callback);

        if (_compensationAddress == null)
            throw new InvalidCompensationAddressException(_compensationAddress);

        var result = new CompletedExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);

        result.SetLog(logValues);

        callback(result);

        return result;
    }

    /// <summary>Completes the activity and updates routing-slip variables.</summary>
    /// <param name="variables">The variable entries merged into routing-slip state.</param>
    /// <returns>A completion result that applies the variables and advances the itinerary.</returns>
    public ExecutionResult CompletedWithVariables(IEnumerable<KeyValuePair<string, object>> variables)
    {
        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        var result = new CompletedExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);

        result.SetVariables(variables);

        return result;
    }

    /// <summary>Completes the activity and updates routing-slip variables.</summary>
    /// <param name="variables">The object's public values merged into routing-slip state.</param>
    /// <returns>A completion result that applies the variables and advances the itinerary.</returns>
    public ExecutionResult CompletedWithVariables(object variables)
    {
        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        var result = new CompletedExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);

        result.SetVariables(variables);

        return result;
    }

    /// <summary>Completes the activity with a compensation log and updated routing-slip variables.</summary>
    /// <typeparam name="TLog">The compensation-log contract.</typeparam>
    /// <param name="log">The compensation log recorded for this execution.</param>
    /// <param name="variables">The object's public values merged into routing-slip state.</param>
    /// <returns>A completion result containing the compensation log and updated variables.</returns>
    public ExecutionResult CompletedWithVariables<TLog>(TLog log, object variables)
        where TLog : class
    {
        if (log == null)
            throw new ArgumentNullException(nameof(log));

        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        if (_compensationAddress == null)
            throw new InvalidCompensationAddressException(_compensationAddress);

        var result = new CompletedExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);

        result.SetLog(log);
        result.SetVariables(variables);

        return result;
    }

    /// <summary>Completes the activity with a mapped compensation log and updated routing-slip variables.</summary>
    /// <typeparam name="TLog">The compensation-log contract to initialize.</typeparam>
    /// <param name="logValues">The values used to initialize the compensation log.</param>
    /// <param name="variables">The object's public values merged into routing-slip state.</param>
    /// <returns>A completion result containing the initialized log and updated variables.</returns>
    public ExecutionResult CompletedWithVariables<TLog>(object logValues, object variables)
        where TLog : class
    {
        if (logValues == null)
            throw new ArgumentNullException(nameof(logValues));

        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        if (_compensationAddress == null)
            throw new InvalidCompensationAddressException(_compensationAddress);

        var result = new CompletedExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);

        result.SetLog(logValues);
        result.SetVariables(variables);

        return result;
    }

    /// <summary>Completes the activity with a compensation log and updated routing-slip variables.</summary>
    /// <typeparam name="TLog">The compensation-log contract.</typeparam>
    /// <param name="log">The compensation log recorded for this execution.</param>
    /// <param name="variables">The variable entries merged into routing-slip state.</param>
    /// <returns>A completion result containing the compensation log and updated variables.</returns>
    public ExecutionResult CompletedWithVariables<TLog>(TLog log, IEnumerable<KeyValuePair<string, object>> variables)
        where TLog : class
    {
        if (log == null)
            throw new ArgumentNullException(nameof(log));

        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        if (_compensationAddress == null)
            throw new InvalidCompensationAddressException(_compensationAddress);

        var result = new CompletedExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);

        result.SetLog(log);
        result.SetVariables(variables);

        return result;
    }

    /// <summary>Replaces the remaining itinerary through the supplied builder callback.</summary>
    /// <param name="buildItinerary">The callback that builds the replacement itinerary.</param>
    /// <returns>A revision result that substitutes the remaining itinerary.</returns>
    public ExecutionResult ReviseItinerary(Action<IItineraryBuilder> buildItinerary)
    {
        if (buildItinerary == null)
            throw new ArgumentNullException(nameof(buildItinerary));

        return new ReviseItineraryExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress, buildItinerary);
    }

    /// <summary>Replaces the remaining itinerary and records a compensation log.</summary>
    /// <typeparam name="TLog">The compensation-log contract.</typeparam>
    /// <param name="log">The compensation log recorded for this execution.</param>
    /// <param name="buildItinerary">The callback that builds the replacement itinerary.</param>
    /// <returns>A revision result containing the compensation log and replacement itinerary.</returns>
    public ExecutionResult ReviseItinerary<TLog>(TLog log, Action<IItineraryBuilder> buildItinerary)
        where TLog : class
    {
        if (log == null)
            throw new ArgumentNullException(nameof(log));

        if (buildItinerary == null)
            throw new ArgumentNullException(nameof(buildItinerary));

        if (_compensationAddress == null)
            throw new InvalidCompensationAddressException(_compensationAddress);

        var result = new ReviseItineraryExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress, buildItinerary);

        result.SetLog(log);

        return result;
    }

    /// <summary>Replaces the remaining itinerary, records a compensation log, and updates variables.</summary>
    /// <typeparam name="TLog">The compensation-log contract.</typeparam>
    /// <param name="log">The compensation log recorded for this execution.</param>
    /// <param name="variables">The object's public values merged into routing-slip state.</param>
    /// <param name="buildItinerary">The callback that builds the replacement itinerary.</param>
    /// <returns>A revision result containing the log, updated variables, and replacement itinerary.</returns>
    public ExecutionResult ReviseItinerary<TLog>(TLog log, object variables, Action<IItineraryBuilder> buildItinerary)
        where TLog : class
    {
        if (log == null)
            throw new ArgumentNullException(nameof(log));

        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        if (buildItinerary == null)
            throw new ArgumentNullException(nameof(buildItinerary));

        if (_compensationAddress == null)
            throw new InvalidCompensationAddressException(_compensationAddress);

        var result = new ReviseItineraryExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress, buildItinerary);

        result.SetLog(log);
        result.SetVariables(variables);

        return result;
    }

    /// <summary>Replaces the remaining itinerary, records a compensation log, and updates variables.</summary>
    /// <typeparam name="TLog">The compensation-log contract.</typeparam>
    /// <param name="log">The compensation log recorded for this execution.</param>
    /// <param name="variables">The variable entries merged into routing-slip state.</param>
    /// <param name="buildItinerary">The callback that builds the replacement itinerary.</param>
    /// <returns>A revision result containing the log, updated variables, and replacement itinerary.</returns>
    public ExecutionResult ReviseItinerary<TLog>(TLog log, IEnumerable<KeyValuePair<string, object>> variables, Action<IItineraryBuilder> buildItinerary)
        where TLog : class
    {
        if (log == null)
            throw new ArgumentNullException(nameof(log));

        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        if (buildItinerary == null)
            throw new ArgumentNullException(nameof(buildItinerary));

        if (_compensationAddress == null)
            throw new InvalidCompensationAddressException(_compensationAddress);

        var result = new ReviseItineraryExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress, buildItinerary);

        result.SetLog(log);
        result.SetVariables(variables);

        return result;
    }

    /// <summary>Terminates the routing slip successfully and discards the remaining itinerary.</summary>
    /// <returns>A terminal result that publishes termination and discards the remaining itinerary.</returns>
    public ExecutionResult Terminate()
    {
        return new TerminateExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);
    }

    /// <summary>Terminates the routing slip successfully, updates variables, and discards the remaining itinerary.</summary>
    /// <param name="variables">The object's public values merged into routing-slip state.</param>
    /// <returns>A terminal result containing the updated variables and discarded itinerary.</returns>
    public ExecutionResult Terminate(object variables)
    {
        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        var result = new TerminateExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);

        result.SetVariables(variables);

        return result;
    }

    /// <summary>Terminates the routing slip successfully, updates variables, and discards the remaining itinerary.</summary>
    /// <param name="variables">The variable entries merged into routing-slip state.</param>
    /// <returns>A terminal result containing the updated variables and discarded itinerary.</returns>
    public ExecutionResult Terminate(IEnumerable<KeyValuePair<string, object>> variables)
    {
        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        var result = new TerminateExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);

        result.SetVariables(variables);

        return result;
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <returns>A fault result containing the default activity-execution failure.</returns>
    public ExecutionResult Faulted()
    {
        return Faulted(new ActivityExecutionFaultedException());
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <param name="exception">The failure raised while executing the activity.</param>
    /// <returns>A fault result containing the supplied failure.</returns>
    public ExecutionResult Faulted(Exception exception)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        return new FaultedExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, exception);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <param name="exception">The failure raised while executing the activity.</param>
    /// <param name="callback">The callback that adds variables to the fault result.</param>
    /// <returns>The configured fault result.</returns>
    public ExecutionResult Faulted(Exception exception, ConfigureFaultedActivityOptionsCallback callback)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(callback);

        var result = new FaultedExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, exception);

        callback(result);

        return result;
    }

    /// <summary>Reports a fault together with the supplied routing-slip variables.</summary>
    /// <param name="exception">The failure raised while executing the activity.</param>
    /// <param name="variables">The object's public values merged into routing-slip state.</param>
    /// <returns>A fault result containing the failure and updated variables.</returns>
    public ExecutionResult FaultedWithVariables(Exception exception, object variables)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));
        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        return Faulted(exception, x => x.SetVariables(variables));
    }

    /// <summary>Reports a fault together with the supplied routing-slip variables.</summary>
    /// <param name="exception">The failure raised while executing the activity.</param>
    /// <param name="variables">The variable entries merged into routing-slip state.</param>
    /// <returns>A fault result containing the failure and updated variables.</returns>
    public ExecutionResult FaultedWithVariables(Exception exception, IEnumerable<KeyValuePair<string, object>> variables)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));
        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        return Faulted(exception, x => x.SetVariables(variables));
    }
}
