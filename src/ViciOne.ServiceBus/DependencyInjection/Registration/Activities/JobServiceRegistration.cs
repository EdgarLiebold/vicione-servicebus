using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.NewIdFormatters;
using JobServiceState = ViciOne.ServiceBus.JobService.JobService;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Registers job service services.</summary>
public class JobServiceRegistration :
    IJobServiceRegistration
{
    readonly List<Action<JobConsumerOptions>> _configureActions;
    readonly List<IReceiveEndpointConfigurator> _dependencies;
    readonly EndpointRegistrationConfigurator<JobServiceState> _endpointConfigurator;
    readonly Lazy<InstanceJobServiceSettings> _settings;

    /// <summary>Initializes a new instance.</summary>
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

    InstanceJobServiceSettings Settings => _settings.Value;

    /// <summary>Gets the type.</summary>
    public Type Type => typeof(JobServiceState);

    /// <summary>Gets or sets the include in configure endpoints.</summary>
    public bool IncludeInConfigureEndpoints { get; set; }

    /// <summary>Gets the endpoint registration configurator.</summary>
    public IEndpointRegistrationConfigurator EndpointRegistrationConfigurator => _endpointConfigurator;
    /// <summary>Gets the endpoint definition.</summary>
    public IEndpointDefinition EndpointDefinition => new JobServiceEndpointDefinition(_endpointConfigurator.Settings, _settings.Value);

    /// <summary>Adds configure action to the configuration.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    public void AddConfigureAction(Action<JobConsumerOptions>? configure)
    {
        if (_settings.IsValueCreated)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Job service", "unknown", "The settings were already computed", "Correct the named configuration before starting the host"));

        if (configure != null)
            _configureActions.Add(configure);
    }

    /// <summary>Adds receive endpoint dependency to the configuration.</summary>
    /// <param name="dependency">The dependency.</param>
    public void AddReceiveEndpointDependency(IReceiveEndpointConfigurator dependency)
    {
        _dependencies.Add(dependency);
    }

    /// <summary>Applies the supplied configuration.</summary>
    /// <param name="instanceConfigurator">The instance configurator.</param>
    /// <param name="context">The context associated with the operation.</param>
    public void Configure(IServiceInstanceConfigurator instanceConfigurator, IRegistrationContext context)
    {
        Settings.ApplyConfiguration(instanceConfigurator.InstanceEndpointConfigurator);
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
