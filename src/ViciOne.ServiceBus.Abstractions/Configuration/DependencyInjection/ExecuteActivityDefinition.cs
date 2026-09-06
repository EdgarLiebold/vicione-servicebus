using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines configuration for execute activity.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public class ExecuteActivityDefinition<TActivity, TArguments> :
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
    /// Specify the endpoint name (which may be a queue, or a subscription, depending upon the transport) on which the saga
    /// should be configured. Setting to null will use the supplied <see cref="IEndpointNameFormatter" /> to generate the
    /// endpoint name.
    /// </summary>
    protected string ExecuteEndpointName
    {
        set => _executeEndpointName = value;
    }

    /// <summary>Gets or sets the execute endpoint definition.</summary>
    public IEndpointDefinition<IExecuteActivity<TArguments>>? ExecuteEndpointDefinition { get; set; }

    IEndpointDefinition? IExecuteActivityDefinition.ExecuteEndpointDefinition => ExecuteEndpointDefinition;

    /// <summary>Specify a concurrency limit, which is applied to the entire consumer, saga, or activity, regardless of message type.</summary>
    public int? ConcurrentMessageLimit
    {
        get => _concurrentMessageLimit;
        protected set => _concurrentMessageLimit = value;
    }

    void IExecuteActivityDefinition<TActivity, TArguments>.Configure(IReceiveEndpointConfigurator endpointConfigurator,
        IExecuteActivityConfigurator<TActivity, TArguments> executeActivityConfigurator, IRegistrationContext context)
    {
        if (_concurrentMessageLimit.HasValue)
            executeActivityConfigurator.ConcurrentMessageLimit = _concurrentMessageLimit;
        ConfigureExecuteActivity(endpointConfigurator, executeActivityConfigurator, context);
    }

    string IExecuteActivityDefinition.GetExecuteEndpointName(IEndpointNameFormatter formatter)
    {
        return string.IsNullOrWhiteSpace(_executeEndpointName)
            ? _executeEndpointName = ExecuteEndpointDefinition?.GetEndpointName(formatter) ?? formatter.ExecuteActivity<TActivity, TArguments>()
            : _executeEndpointName!;
    }

    Type IExecuteActivityDefinition.ActivityType => typeof(TActivity);
    Type IExecuteActivityDefinition.ArgumentType => typeof(TArguments);

    /// <summary>Configure the execute endpoint.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    protected void ExecuteEndpoint(Action<IEndpointRegistrationConfigurator>? configure = null)
    {
        var configurator = new EndpointRegistrationConfigurator<IExecuteActivity<TArguments>> { ConfigureConsumeTopology = false };

        configure?.Invoke(configurator);

        ExecuteEndpointDefinition = new ExecuteActivityEndpointDefinition<TActivity, TArguments>(configurator.Settings);
    }

    /// <summary>Called when the compensate activity is being configured on the endpoint.</summary>
    /// <param name="endpointConfigurator">The receive endpoint configurator for the consumer.</param>
    /// <param name="executeActivityConfigurator">The execute activity configurator.</param>
    /// <param name="context">The context associated with the operation.</param>
    protected virtual void ConfigureExecuteActivity(IReceiveEndpointConfigurator endpointConfigurator,
        IExecuteActivityConfigurator<TActivity, TArguments> executeActivityConfigurator, IRegistrationContext context)
    {
    }
}
