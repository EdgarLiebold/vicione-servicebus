using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Connects discovered job consumers to their runtime options and stable job-type identity.</summary>
internal sealed class JobServiceConsumerConfigurationObserver :
    IConsumerConfigurationObserver
{
    readonly IReceiveEndpointConfigurator _configurator;
    readonly Action<IReceiveEndpointConfigurator> _configureEndpoint;
    readonly Dictionary<Type, IConsumeConfigurator> _consumerConfigurators;
    readonly JobServiceSettings _settings;
    bool _endpointConfigured;

    /// <summary>Creates an observer for one receive endpoint.</summary>
    /// <param name="configurator">The receive endpoint that owns discovered job consumers.</param>
    /// <param name="settings">The job-service runtime settings.</param>
    /// <param name="configureEndpoint">The endpoint configuration applied once when the first job consumer is discovered.</param>
    public JobServiceConsumerConfigurationObserver(IReceiveEndpointConfigurator configurator, JobServiceSettings settings,
        Action<IReceiveEndpointConfigurator> configureEndpoint)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(configureEndpoint);

        _configurator = configurator;
        _configureEndpoint = configureEndpoint;

        _settings = settings;

        _consumerConfigurators = new Dictionary<Type, IConsumeConfigurator>();
    }

    /// <summary>Captures the options of a discovered job consumer and applies its endpoint configuration once.</summary>
    /// <typeparam name="TConsumer">The configured consumer type.</typeparam>
    /// <param name="configurator">The consumer configurator.</param>
    public void ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator)
        where TConsumer : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        if (typeof(TConsumer).ImplementsInterface(typeof(IJobConsumer<>)))
        {
            _consumerConfigurators.Add(typeof(TConsumer), configurator);

            configurator.Options(_settings);

            if (_endpointConfigured)
                return;

            _configureEndpoint(_configurator);

            _endpointConfigured = true;
        }
    }

    /// <summary>Registers the stable identity and execution options of a discovered job contract.</summary>
    /// <typeparam name="TConsumer">The configured consumer type.</typeparam>
    /// <typeparam name="TJob">The job contract consumed by <typeparamref name="TConsumer" />.</typeparam>
    /// <param name="configurator">The consumer-message configurator.</param>
    public void ConsumerMessageConfigured<TConsumer, TJob>(IConsumerMessageConfigurator<TConsumer, TJob> configurator)
        where TConsumer : class
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        if (typeof(TConsumer).ImplementsInterface<IJobConsumer<TJob>>()
            && _consumerConfigurators.TryGetValue(typeof(TConsumer), out var value)
            && value is IConsumerConfigurator<TConsumer> consumerConfigurator)
        {
            var options = consumerConfigurator.Options<JobOptions<TJob>>();
            string? endpointName = _configurator.InputAddress.GetEndpointName();
            if (string.IsNullOrWhiteSpace(endpointName))
            {
                throw new ConfigurationException(
                    global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                        "Job consumer endpoint",
                        "unknown",
                        "The receive endpoint name is missing.",
                        "Configure a non-empty endpoint name before adding a job consumer"));
            }

            var jobTypeId = JobTypeIdentity<TConsumer, TJob>.CreateId(endpointName);
            var jobTypeName = JobTypeIdentity<TConsumer, TJob>.CreateName(endpointName);

            _settings.Runtime.RegisterJobType(options, jobTypeId, jobTypeName);
        }
    }
}
