using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.NewIdFormatters;
using JobServiceState = ViciOne.ServiceBus.JobService.JobService;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Owns local job-runtime registration and endpoint lifecycle dependencies.</summary>
internal sealed class JobServiceRegistration :
    IJobServiceRegistration
{
    readonly List<Action<JobConsumerOptions>> _configureActions;
    readonly List<IReceiveEndpointConfigurator> _dependencies;
    readonly EndpointRegistrationConfigurator<JobServiceState> _endpointConfigurator;
    readonly Lazy<InstanceJobServiceSettings> _settings;

    /// <summary>Creates an isolated registration for one job-service instance endpoint.</summary>
    public JobServiceRegistration()
    {
        _configureActions = [];
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

    /// <summary>Gets the runtime type represented by this registration.</summary>
    public Type Type => typeof(JobServiceState);

    /// <summary>Gets or sets whether endpoint discovery includes the job-service instance endpoint.</summary>
    public bool IncludeInConfigureEndpoints { get; set; }

    /// <summary>Gets the mutable endpoint registration settings.</summary>
    public IEndpointRegistrationConfigurator EndpointRegistrationConfigurator => _endpointConfigurator;
    /// <summary>Gets a definition that configures the local job-execution endpoint.</summary>
    public IEndpointDefinition EndpointDefinition => new JobServiceEndpointDefinition(_endpointConfigurator.Settings, _settings.Value);

    /// <summary>Adds a callback that configures job-consumer runtime options before they are materialized.</summary>
    /// <param name="configure">The configuration callback to append.</param>
    /// <exception cref="ArgumentNullException"><paramref name="configure" /> is <see langword="null" />.</exception>
    public void AddConfigureAction(Action<JobConsumerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        if (_settings.IsValueCreated)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Job service", "unknown", "The settings were already computed", "Correct the named configuration before starting the host"));

        _configureActions.Add(configure);
    }

    /// <summary>Adds an endpoint that must become ready before a discovered job-consumer endpoint starts.</summary>
    /// <param name="dependency">The endpoint dependency.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dependency" /> is <see langword="null" />.</exception>
    public void AddReceiveEndpointDependency(IReceiveEndpointConfigurator dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        _dependencies.Add(dependency);
    }

    /// <summary>Connects the local job runtime to its service-instance endpoint and bus lifecycle.</summary>
    /// <param name="instanceConfigurator">The service-instance configuration that owns the endpoint.</param>
    /// <param name="context">The registration context for the configured bus.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instanceConfigurator" /> or <paramref name="context" /> is <see langword="null" />.</exception>
    public void Configure(IServiceInstanceConfigurator instanceConfigurator, IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(instanceConfigurator);
        ArgumentNullException.ThrowIfNull(context);

        Settings.ApplyConfiguration(instanceConfigurator.InstanceEndpointConfigurator);
        AddReceiveEndpointDependency(instanceConfigurator.InstanceEndpointConfigurator);

        Settings.Runtime.ConfigureSuperviseJobConsumer(instanceConfigurator.InstanceEndpointConfigurator);

        if (instanceConfigurator.BusConfigurator is IBusObserverConnector connector)
            connector.ConnectBusObserver(new JobServiceBusObserver(Settings.Runtime));

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

        ((ISpecification)options).Validate()
            .ThrowIfContainsFailure("The job consumer options are invalid:");

        return new InstanceJobServiceSettings(options);
    }
}
