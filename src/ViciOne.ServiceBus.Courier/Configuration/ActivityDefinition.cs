using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides a base definition for a compensatable routing-slip activity and its two endpoints.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
public abstract class ActivityDefinition<TActivity, TArguments, TLog> :
    ExecuteActivityDefinition<TActivity, TArguments>,
    IActivityDefinition<TActivity, TArguments, TLog>
    where TActivity : class, IActivity<TArguments, TLog>
    where TLog : class
    where TArguments : class
{
    string? _compensateEndpointName;

    /// <summary>
    /// Sets the compensation endpoint name. When omitted, the configured
    /// <see cref="IEndpointNameFormatter" /> derives the name from the activity and log types.
    /// </summary>
    protected string CompensateEndpointName
    {
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _compensateEndpointName = value;
        }
    }

    /// <summary>Gets or sets the compensate endpoint definition.</summary>
    public IEndpointDefinition<ICompensateActivity<TLog>>? CompensateEndpointDefinition { get; set; }

    IEndpointDefinition? IActivityDefinition.CompensateEndpointDefinition => CompensateEndpointDefinition;

    void IActivityDefinition<TActivity, TArguments, TLog>.Configure(IReceiveEndpointConfigurator endpointConfigurator,
        ICompensateActivityConfigurator<TActivity, TLog> compensateActivityConfigurator, IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(endpointConfigurator);
        ArgumentNullException.ThrowIfNull(compensateActivityConfigurator);
        ArgumentNullException.ThrowIfNull(context);

        if (ConcurrentMessageLimit.HasValue)
            compensateActivityConfigurator.ConcurrentMessageLimit = ConcurrentMessageLimit;
        ConfigureCompensateActivity(endpointConfigurator, compensateActivityConfigurator, context);
    }

    string IActivityDefinition.GetCompensateEndpointName(IEndpointNameFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        return string.IsNullOrWhiteSpace(_compensateEndpointName)
            ? _compensateEndpointName = CompensateEndpointDefinition?.GetEndpointName(formatter) ?? formatter.CompensateActivity<TActivity, TLog>()
            : _compensateEndpointName!;
    }

    Type IActivityDefinition.LogType => typeof(TLog);

    /// <summary>Overrides the generated compensation endpoint settings.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    protected void CompensateEndpoint(Action<IEndpointRegistrationConfigurator>? configure = null)
    {
        var configurator = new EndpointRegistrationConfigurator<ICompensateActivity<TLog>> { ConfigureConsumeTopology = false };

        configure?.Invoke(configurator);

        CompensateEndpointDefinition = new CompensateActivityEndpointDefinition<TActivity, TLog>(configurator.Settings);
    }

    /// <summary>Configures the compensation activity pipeline on its receive endpoint.</summary>
    /// <param name="endpointConfigurator">The receive endpoint configurator for the consumer.</param>
    /// <param name="compensateActivityConfigurator">The compensate activity configurator.</param>
    /// <param name="context">The context associated with the operation.</param>
    protected virtual void ConfigureCompensateActivity(IReceiveEndpointConfigurator endpointConfigurator,
        ICompensateActivityConfigurator<TActivity, TLog> compensateActivityConfigurator, IRegistrationContext context)
    {
    }
}
