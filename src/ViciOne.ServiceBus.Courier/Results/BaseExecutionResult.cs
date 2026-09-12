using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Results;

abstract class BaseExecutionResult<TArguments> :
    ExecutionResult
    where TArguments : class
{
    TimeSpan? _delay;

    protected BaseExecutionResult(ExecuteContext<TArguments> context, IRoutingSlipEventPublisher publisher, Activity activity, RoutingSlip routingSlip)
    {
        Context = context;
        Publisher = publisher;
        Activity = activity;
        RoutingSlip = routingSlip;
        Duration = Context.Elapsed;
    }

    protected ExecuteContext<TArguments> Context { get; }
    protected IRoutingSlipEventPublisher Publisher { get; }
    protected TimeSpan Duration { get; }

    protected RoutingSlip RoutingSlip { get; }

    protected Activity Activity { get; }

    protected Dictionary<string, object> Variables { get; } = new(StringComparer.OrdinalIgnoreCase);
    public TimeSpan? Delay
    {
        get => _delay;
        set
        {
            if (value < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value), value, "The activity delay cannot be negative.");

            _delay = value;
        }
    }

    public abstract Task EvaluateAsync(CancellationToken cancellationToken = default);

    public virtual bool IsFaulted([NotNullWhen(true)] out Exception? exception)
    {
        exception = null;
        return false;
    }

    public void SetVariables(object variables)
    {
        ArgumentNullException.ThrowIfNull(variables);

        IEnumerable<KeyValuePair<string, object>> dictionary = Context.SerializerContext.ToDictionary(variables);

        SetVariables(dictionary);
    }

    public void SetVariables(IEnumerable<KeyValuePair<string, object>> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);

        foreach (KeyValuePair<string, object> value in variables)
            SetVariable(value.Key, value.Value);
    }

    public void SetVariable(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        Variables[key] = value!;
    }
}
