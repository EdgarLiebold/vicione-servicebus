using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.NewIdFormatters;
using JobServiceState = ViciOne.ServiceBus.JobService.JobService;

#nullable enable
namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>
/// Provides a job service registration implementation.
/// </summary>
public class JobServiceRegistration :
    IJobServiceRegistration
{
    readonly List<Action<JobConsumerOptions>> _configureActions;
    readonly List<IReceiveEndpointConfigurator> _dependencies;
    readonly EndpointRegistrationConfigurator<JobServiceState> _endpointConfigurator;
    readonly Lazy<InstanceJobServiceSettings> _settings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public JobServiceRegistration()
    {
        _configureActions = new List<Action<JobConsumerOptions>>();
        _dependencies = new List<IReceiveEndpointConfigurator>(4);

        _settings = new Lazy<InstanceJobServiceSettings>(GetJobServiceSettings);

        _endpointConfigurator = new EndpointRegistrationConfigurator<JobServiceState>
        {
            Name = "Instance",
            InstanceId = NewId.Next().ToString(ZBase32Formatter.LowerCase),
            Temporary = true
        };

        IncludeInConfigureEndpoints = true;
    }

    JobServiceSettings Settings => _settings.Value;

    /// <summary>
    /// Gets the type value.
    /// </summary>
    public Type Type => typeof(JobServiceState);

    /// <summary>
    /// Gets or sets the include in configure endpoints value.
    /// </summary>
    public bool IncludeInConfigureEndpoints { get; set; }

    /// <summary>
    /// Gets the endpoint registration configurator value.
    /// </summary>
    public IEndpointRegistrationConfigurator EndpointRegistrationConfigurator => _endpointConfigurator;
    /// <summary>
    /// Gets the endpoint definition value.
    /// </summary>
    public IEndpointDefinition EndpointDefinition => new JobServiceEndpointDefinition(_endpointConfigurator.Settings, _settings.Value);

    /// <summary>
    /// Adds configure action to the configuration.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void AddConfigureAction(Action<JobConsumerOptions>? configure)
    {
        if (_settings.IsValueCreated)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Job service", "unknown", "The settings were already computed", "Correct the named configuration before starting the host"));

        if (configure != null)
            _configureActions.Add(configure);
    }

    /// <summary>
    /// Adds receive endpoint dependency to the configuration.
    /// </summary>
    /// <param name="dependency">The dependency value.</param>
    public void AddReceiveEndpointDependency(IReceiveEndpointConfigurator dependency)
    {
        _dependencies.Add(dependency);
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="instanceConfigurator">The instance configurator value.</param>
    /// <param name="context">The operation context.</param>
    public void Configure(IServiceInstanceConfigurator instanceConfigurator, IRegistrationContext context)
    {
        AddReceiveEndpointDependency(instanceConfigurator.InstanceEndpointConfigurator);

        Settings.JobService.ConfigureSuperviseJobConsumer(instanceConfigurator.InstanceEndpointConfigurator);

        if (instanceConfigurator.BusConfigurator is IBusObserverConnector connector)
            connector.ConnectBusObserver(new JobServiceBusObserver(Settings.JobService));

        instanceConfigurator.ConnectEndpointConfigurationObserver(new JobServiceEndpointConfigurationObserver(Settings, ConfigureJobConsumerEndpoint));
    }

    void ConfigureJobConsumerEndpoint(IReceiveEndpointConfigurator configurator)
    {
        foreach (var dependency in _dependencies)
            configurator.AddDependency(dependency);
    }

    InstanceJobServiceSettings GetJobServiceSettings()
    {
        var options = new JobConsumerOptions();
        foreach (Action<JobConsumerOptions> configure in _configureActions)
            configure(options);

        return new InstanceJobServiceSettings(options);
    }
}
