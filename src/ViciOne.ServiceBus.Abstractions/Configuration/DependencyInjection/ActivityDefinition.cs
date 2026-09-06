using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines configuration for activity.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
public class ActivityDefinition<TActivity, TArguments, TLog> :
    ExecuteActivityDefinition<TActivity, TArguments>,
    IActivityDefinition<TActivity, TArguments, TLog>
    where TActivity : class, IActivity<TArguments, TLog>
    where TLog : class
    where TArguments : class
{
    string? _compensateEndpointName;

    /// <summary>
    /// Specify the endpoint name (which may be a queue, or a subscription, depending upon the transport) on which the saga
    /// should be configured. Setting to null will use the supplied <see cref="IEndpointNameFormatter" /> to generate the
    /// endpoint name.
    /// </summary>
    protected string CompensateEndpointName
    {
        set => _compensateEndpointName = value;
    }

    /// <summary>Gets or sets the compensate endpoint definition.</summary>
    public IEndpointDefinition<ICompensateActivity<TLog>>? CompensateEndpointDefinition { get; set; }

    IEndpointDefinition? IActivityDefinition.CompensateEndpointDefinition => CompensateEndpointDefinition;

    void IActivityDefinition<TActivity, TArguments, TLog>.Configure(IReceiveEndpointConfigurator endpointConfigurator,
        ICompensateActivityConfigurator<TActivity, TLog> compensateActivityConfigurator, IRegistrationContext context)
    {
        if (ConcurrentMessageLimit.HasValue)
            compensateActivityConfigurator.ConcurrentMessageLimit = ConcurrentMessageLimit;
        ConfigureCompensateActivity(endpointConfigurator, compensateActivityConfigurator, context);
    }

    string IActivityDefinition.GetCompensateEndpointName(IEndpointNameFormatter formatter)
    {
        return string.IsNullOrWhiteSpace(_compensateEndpointName)
            ? _compensateEndpointName = CompensateEndpointDefinition?.GetEndpointName(formatter) ?? formatter.CompensateActivity<TActivity, TLog>()
            : _compensateEndpointName!;
    }

    Type IActivityDefinition.LogType => typeof(TLog);

    /// <summary>Configure the compensate endpoint.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    protected void CompensateEndpoint(Action<IEndpointRegistrationConfigurator>? configure = null)
    {
        var configurator = new EndpointRegistrationConfigurator<ICompensateActivity<TLog>> { ConfigureConsumeTopology = false };

        configure?.Invoke(configurator);

        CompensateEndpointDefinition = new CompensateActivityEndpointDefinition<TActivity, TLog>(configurator.Settings);
    }

    /// <summary>Called when the compensate activity is being configured on the endpoint.</summary>
    /// <param name="endpointConfigurator">The receive endpoint configurator for the consumer.</param>
    /// <param name="compensateActivityConfigurator">The compensate activity configurator.</param>
    /// <param name="context">The context associated with the operation.</param>
    protected virtual void ConfigureCompensateActivity(IReceiveEndpointConfigurator endpointConfigurator,
        ICompensateActivityConfigurator<TActivity, TLog> compensateActivityConfigurator, IRegistrationContext context)
    {
    }
}
