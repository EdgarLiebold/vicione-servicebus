using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Context;

/// <summary>Forwards execute context operations to an underlying context.</summary>
/// <typeparam name="TArguments">The activity argument contract.</typeparam>
public class ExecuteContextProxy<TArguments> :
    ActivityContextProxy,
    ExecuteContext<TArguments>
    where TArguments : class
{
    readonly ExecuteContext<TArguments> _context;

    /// <summary>Creates an execute-context view with explicit activity arguments.</summary>
    /// <param name="context">The execute context to wrap.</param>
    /// <param name="arguments">The activity arguments exposed by the proxy.</param>
    public ExecuteContextProxy(ExecuteContext<TArguments> context, TArguments arguments)
        : base(context)
    {
        _context = context;
        Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
    }

    /// <summary>Creates a forwarding view over an execute context.</summary>
    /// <param name="context">The execute context to wrap.</param>
    protected ExecuteContextProxy(ExecuteContext<TArguments> context)
        : base(context)
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

    /// <inheritdoc />
    public ExecuteActivityContext<TActivity, TArguments> CreateActivityContext<TActivity>(TActivity activity)
        where TActivity : class
    {
        return new HostExecuteActivityContext<TActivity, TArguments>(activity, this);
    }

    /// <inheritdoc />
    public ExecutionResult Completed()
    {
        return _context.Completed();
    }

    /// <inheritdoc />
    public ExecutionResult Completed(ConfigureCompletedActivityOptionsCallback callback)
    {
        return _context.Completed(callback);
    }

    /// <inheritdoc />
    public ExecutionResult CompletedWithVariables(IEnumerable<KeyValuePair<string, object>> variables)
    {
        return _context.CompletedWithVariables(variables);
    }

    /// <inheritdoc />
    public ExecutionResult CompletedWithVariables(object variables)
    {
        return _context.CompletedWithVariables(variables);
    }

    /// <inheritdoc />
    public ExecutionResult Completed<TLog>(TLog log)
        where TLog : class
    {
        return _context.Completed(log);
    }

    /// <inheritdoc />
    public ExecutionResult Completed<TLog>(TLog log, ConfigureCompletedActivityOptionsCallback callback)
        where TLog : class
    {
        return _context.Completed(log, callback);
    }

    /// <inheritdoc />
    public ExecutionResult Completed<TLog>(object logValues)
        where TLog : class
    {
        return _context.Completed<TLog>(logValues);
    }

    /// <inheritdoc />
    public ExecutionResult Completed<TLog>(object logValues, ConfigureCompletedActivityOptionsCallback callback)
        where TLog : class
    {
        return _context.Completed<TLog>(logValues, callback);
    }

    /// <inheritdoc />
    public ExecutionResult CompletedWithVariables<TLog>(TLog log, object variables)
        where TLog : class
    {
        return _context.CompletedWithVariables(log, variables);
    }

    /// <inheritdoc />
    public ExecutionResult CompletedWithVariables<TLog>(object logValues, object variables)
        where TLog : class
    {
        return _context.CompletedWithVariables<TLog>(logValues, variables);
    }

    /// <inheritdoc />
    public ExecutionResult CompletedWithVariables<TLog>(TLog log, IEnumerable<KeyValuePair<string, object>> variables)
        where TLog : class
    {
        return _context.CompletedWithVariables(log, variables);
    }

    /// <inheritdoc />
    public ExecutionResult ReviseItinerary(Action<IItineraryBuilder> buildItinerary)
    {
        return _context.ReviseItinerary(buildItinerary);
    }

    /// <inheritdoc />
    public ExecutionResult ReviseItinerary<TLog>(TLog log, Action<IItineraryBuilder> buildItinerary)
        where TLog : class
    {
        return _context.ReviseItinerary(log, buildItinerary);
    }

    /// <inheritdoc />
    public ExecutionResult ReviseItinerary<TLog>(TLog log, object variables, Action<IItineraryBuilder> buildItinerary)
        where TLog : class
    {
        return _context.ReviseItinerary(log, variables, buildItinerary);
    }

    /// <inheritdoc />
    public ExecutionResult ReviseItinerary<TLog>(TLog log, IEnumerable<KeyValuePair<string, object>> variables, Action<IItineraryBuilder> buildItinerary)
        where TLog : class
    {
        return _context.ReviseItinerary(log, variables, buildItinerary);
    }

    /// <inheritdoc />
    public ExecutionResult Terminate()
    {
        return _context.Terminate();
    }

    /// <inheritdoc />
    public ExecutionResult Terminate(object variables)
    {
        return _context.Terminate(variables);
    }

    /// <inheritdoc />
    public ExecutionResult Terminate(IEnumerable<KeyValuePair<string, object>> variables)
    {
        return _context.Terminate(variables);
    }

    /// <inheritdoc />
    public ExecutionResult Faulted()
    {
        return _context.Faulted();
    }

    /// <inheritdoc />
    public ExecutionResult Faulted(Exception exception)
    {
        return _context.Faulted(exception);
    }

    /// <inheritdoc />
    public ExecutionResult Faulted(Exception exception, ConfigureFaultedActivityOptionsCallback callback)
    {
        return _context.Faulted(exception, callback);
    }

    /// <inheritdoc />
    public ExecutionResult FaultedWithVariables(Exception exception, object variables)
    {
        return _context.FaultedWithVariables(exception, variables);
    }

    /// <inheritdoc />
    public ExecutionResult FaultedWithVariables(Exception exception, IEnumerable<KeyValuePair<string, object>> variables)
    {
        return _context.FaultedWithVariables(exception, variables);
    }
}
