using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a job service consumer configuration observer implementation.
/// </summary>
public class JobServiceConsumerConfigurationObserver :
    IConsumerConfigurationObserver
{
    readonly IReceiveEndpointConfigurator _configurator;
    readonly Action<IReceiveEndpointConfigurator> _configureEndpoint;
    readonly Dictionary<Type, IConsumeConfigurator> _consumerConfigurators;
    readonly JobServiceSettings _settings;
    bool _endpointConfigured;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="settings">The settings value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    public JobServiceConsumerConfigurationObserver(IReceiveEndpointConfigurator configurator, JobServiceSettings settings,
        Action<IReceiveEndpointConfigurator> configureEndpoint)
    {
        _configurator = configurator;
        _configureEndpoint = configureEndpoint;

        _settings = settings;

        _consumerConfigurators = new Dictionary<Type, IConsumeConfigurator>();
    }

    /// <summary>
    /// Consumes r configured.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void ConsumerConfigured<T>(IConsumerConfigurator<T> configurator)
        where T : class
    {
        if (typeof(T).ImplementsInterface(typeof(IJobConsumer<>)))
        {
            _consumerConfigurators.Add(typeof(T), configurator);

            configurator.Options(_settings);

            if (_endpointConfigured)
                return;

            _configureEndpoint(_configurator);

            _endpointConfigured = true;
        }
    }

    /// <summary>
    /// Consumes r message configured.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void ConsumerMessageConfigured<T, TMessage>(IConsumerMessageConfigurator<T, TMessage> configurator)
        where T : class
        where TMessage : class
    {
        if (typeof(T).ImplementsInterface<IJobConsumer<TMessage>>()
            && _consumerConfigurators.TryGetValue(typeof(T), out var value)
            && value is IConsumerConfigurator<T> consumerConfigurator)
        {
            var options = consumerConfigurator.Options<JobOptions<TMessage>>();

            var jobTypeId = JobMetadataCache<T, TMessage>.GenerateJobTypeId(_configurator.InputAddress.GetEndpointName());
            var jobTypeName = JobMetadataCache<T, TMessage>.GenerateJobTypeName(_configurator.InputAddress.GetEndpointName());

            _settings.JobService.RegisterJobType(_configurator, options, jobTypeId, jobTypeName);
        }
    }
}
