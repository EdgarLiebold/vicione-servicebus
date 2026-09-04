using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Results;

namespace ViciOne.ServiceBus.Courier;

/// <summary>
/// Provides a host execute context implementation.
/// </summary>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
public class HostExecuteContext<TArguments> :
    BaseCourierContext,
    ExecuteContext<TArguments>
    where TArguments : class
{
    readonly Activity _activity;
    readonly Uri _compensationAddress;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="compensationAddress">The compensation address value.</param>
    /// <param name="context">The operation context.</param>
    public HostExecuteContext(Uri compensationAddress, ConsumeContext<RoutingSlip> context)
        : base(context)
    {
        _compensationAddress = compensationAddress;

        if (RoutingSlip.Itinerary.Count == 0)
            throw new ArgumentException("The routingSlip must contain at least one activity");

        _activity = RoutingSlip.Itinerary[0];
        Arguments = RoutingSlip.GetActivityArguments<TArguments>();
    }

    /// <summary>
    /// Gets the activity name value.
    /// </summary>
    public override string ActivityName => _activity.Name;
    /// <summary>
    /// Gets the arguments value.
    /// </summary>
    public TArguments Arguments { get; }

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

    /// <summary>
    /// Gets or sets the result value.
    /// </summary>
    public ExecutionResult Result { get; set; } = null!;
    /// <summary>
    /// Performs the completed operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult Completed()
    {
        return new CompletedExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);
    }

    /// <summary>
    /// Performs the completed operation.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult Completed(ConfigureCompletedActivityOptionsCallback callback)
    {
        var result = new CompletedExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);

        callback(result);

        return result;
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
        if (log == null)
            throw new ArgumentNullException(nameof(log));

        if (_compensationAddress == null)
            throw new InvalidCompensationAddressException(_compensationAddress);

        var result = new CompletedExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);

        result.SetLog(log);

        return result;
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
        if (log == null)
            throw new ArgumentNullException(nameof(log));

        if (_compensationAddress == null)
            throw new InvalidCompensationAddressException(_compensationAddress);

        var result = new CompletedExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);

        result.SetLog(log);

        callback(result);

        return result;
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
        if (logValues == null)
            throw new ArgumentNullException(nameof(logValues));

        if (_compensationAddress == null)
            throw new InvalidCompensationAddressException(_compensationAddress);

        var result = new CompletedExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);

        result.SetLog(logValues);

        return result;
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
        if (logValues == null)
            throw new ArgumentNullException(nameof(logValues));

        if (_compensationAddress == null)
            throw new InvalidCompensationAddressException(_compensationAddress);

        var result = new CompletedExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);

        result.SetLog(logValues);

        callback(result);

        return result;
    }

    /// <summary>
    /// Performs the completed with variables operation.
    /// </summary>
    /// <param name="variables">The variables value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult CompletedWithVariables(IEnumerable<KeyValuePair<string, object>> variables)
    {
        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        var result = new CompletedExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);

        result.SetVariables(variables);

        return result;
    }

    /// <summary>
    /// Performs the completed with variables operation.
    /// </summary>
    /// <param name="variables">The variables value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult CompletedWithVariables(object variables)
    {
        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        var result = new CompletedExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);

        result.SetVariables(variables);

        return result;
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

    /// <summary>
    /// Performs the revise itinerary operation.
    /// </summary>
    /// <param name="buildItinerary">The build itinerary value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult ReviseItinerary(Action<IItineraryBuilder> buildItinerary)
    {
        if (buildItinerary == null)
            throw new ArgumentNullException(nameof(buildItinerary));

        return new ReviseItineraryExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress, buildItinerary);
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

    /// <summary>
    /// Performs the terminate operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult Terminate()
    {
        return new TerminateExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);
    }

    /// <summary>
    /// Performs the terminate operation.
    /// </summary>
    /// <param name="variables">The variables value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult Terminate(object variables)
    {
        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        var result = new TerminateExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);

        result.SetVariables(variables);

        return result;
    }

    /// <summary>
    /// Performs the terminate operation.
    /// </summary>
    /// <param name="variables">The variables value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult Terminate(IEnumerable<KeyValuePair<string, object>> variables)
    {
        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        var result = new TerminateExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, _compensationAddress);

        result.SetVariables(variables);

        return result;
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult Faulted()
    {
        return Faulted(new ActivityExecutionFaultedException());
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult Faulted(Exception exception)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        return new FaultedExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, exception);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult Faulted(Exception exception, ConfigureFaultedActivityOptionsCallback callback)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        var result = new FaultedExecutionResult<TArguments>(this, Publisher, _activity, RoutingSlip, exception);

        callback?.Invoke(result);

        return result;
    }

    /// <summary>
    /// Performs the faulted with variables operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="variables">The variables value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult FaultedWithVariables(Exception exception, object variables)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));
        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        return Faulted(exception, x => x.SetVariables(variables));
    }

    /// <summary>
    /// Performs the faulted with variables operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="variables">The variables value.</param>
    /// <returns>The result of the operation.</returns>
    public ExecutionResult FaultedWithVariables(Exception exception, IEnumerable<KeyValuePair<string, object>> variables)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));
        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        return Faulted(exception, x => x.SetVariables(variables));
    }
}
