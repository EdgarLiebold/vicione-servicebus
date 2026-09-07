using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides a base definition for a routing-slip execution activity and its endpoint.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public abstract class ExecuteActivityDefinition<TActivity, TArguments> :
    IExecuteActivityDefinition<TActivity, TArguments>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    int? _concurrentMessageLimit;
    string? _executeEndpointName;

    /// <summary>Initializes a new instance.</summary>
    protected ExecuteActivityDefinition()
    {
    }

    /// <summary>
    /// Sets the execution endpoint name. When omitted, the configured
    /// <see cref="IEndpointNameFormatter" /> derives the name from the activity and argument types.
    /// </summary>
    protected string ExecuteEndpointName
    {
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _executeEndpointName = value;
        }
    }

    /// <summary>Gets or sets the execute endpoint definition.</summary>
    public IEndpointDefinition<IExecuteActivity<TArguments>>? ExecuteEndpointDefinition { get; set; }

    IEndpointDefinition? IExecuteActivityDefinition.ExecuteEndpointDefinition => ExecuteEndpointDefinition;

    /// <summary>Gets or sets the maximum number of activity executions processed concurrently.</summary>
    public int? ConcurrentMessageLimit
    {
        get => _concurrentMessageLimit;
        protected set
        {
            if (value is <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), value, "The concurrent message limit must be greater than zero.");

            _concurrentMessageLimit = value;
        }
    }

    void IExecuteActivityDefinition<TActivity, TArguments>.Configure(IReceiveEndpointConfigurator endpointConfigurator,
        IExecuteActivityConfigurator<TActivity, TArguments> executeActivityConfigurator, IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(endpointConfigurator);
        ArgumentNullException.ThrowIfNull(executeActivityConfigurator);
        ArgumentNullException.ThrowIfNull(context);

        if (_concurrentMessageLimit.HasValue)
            executeActivityConfigurator.ConcurrentMessageLimit = _concurrentMessageLimit;
        ConfigureExecuteActivity(endpointConfigurator, executeActivityConfigurator, context);
    }

    string IExecuteActivityDefinition.GetExecuteEndpointName(IEndpointNameFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        return string.IsNullOrWhiteSpace(_executeEndpointName)
            ? _executeEndpointName = ExecuteEndpointDefinition?.GetEndpointName(formatter) ?? formatter.ExecuteActivity<TActivity, TArguments>()
            : _executeEndpointName!;
    }

    Type IExecuteActivityDefinition.ActivityType => typeof(TActivity);
    Type IExecuteActivityDefinition.ArgumentType => typeof(TArguments);

    /// <summary>Overrides the generated execution endpoint settings.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    protected void ExecuteEndpoint(Action<IEndpointRegistrationConfigurator>? configure = null)
    {
        var configurator = new EndpointRegistrationConfigurator<IExecuteActivity<TArguments>> { ConfigureConsumeTopology = false };

        configure?.Invoke(configurator);

        ExecuteEndpointDefinition = new ExecuteActivityEndpointDefinition<TActivity, TArguments>(configurator.Settings);
    }

    /// <summary>Configures the activity execution pipeline on its receive endpoint.</summary>
    /// <param name="endpointConfigurator">The receive endpoint configurator for the consumer.</param>
    /// <param name="executeActivityConfigurator">The execute activity configurator.</param>
    /// <param name="context">The context associated with the operation.</param>
    protected virtual void ConfigureExecuteActivity(IReceiveEndpointConfigurator endpointConfigurator,
        IExecuteActivityConfigurator<TActivity, TArguments> executeActivityConfigurator, IRegistrationContext context)
    {
    }
}
