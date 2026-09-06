using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.ActiveMq.Middleware;
using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Configures and builds one ActiveMQ queue receive endpoint.</summary>
public class ActiveMqReceiveEndpointConfiguration :
    ReceiveEndpointConfiguration,
    IActiveMqReceiveEndpointConfiguration,
    IActiveMqReceiveEndpointConfigurator
{
    readonly IActiveMqEndpointConfiguration _endpointConfiguration;
    readonly IActiveMqHostConfiguration _hostConfiguration;
    readonly Lazy<Uri> _inputAddress;
    readonly IBuildPipeConfigurator<SessionContext> _sessionConfigurator;
    readonly ActiveMqQueueReceiveSettings _settings;

    /// <summary>Creates a receive-endpoint configuration for an ActiveMQ queue.</summary>
    /// <param name="hostConfiguration">The ActiveMQ host configuration.</param>
    /// <param name="settings">The queue receive settings.</param>
    /// <param name="endpointConfiguration">The endpoint's shared configuration.</param>
    public ActiveMqReceiveEndpointConfiguration(IActiveMqHostConfiguration hostConfiguration, ActiveMqQueueReceiveSettings settings,
        IActiveMqEndpointConfiguration endpointConfiguration)
        : base(hostConfiguration, endpointConfiguration)
    {
        _settings = settings;

        _hostConfiguration = hostConfiguration;
        _endpointConfiguration = endpointConfiguration;

        _sessionConfigurator = new PipeConfigurator<SessionContext>();

        _inputAddress = new Lazy<Uri>(FormatInputAddress);
    }

    /// <summary>Gets the queue receive settings.</summary>
    public ReceiveSettings Settings => _settings;
    /// <summary>Gets the configured broker address.</summary>
    public override Uri HostAddress => _hostConfiguration.HostAddress;
    /// <summary>Gets the lazily formatted queue input address.</summary>
    public override Uri InputAddress => _inputAddress.Value;
    IActiveMqTopologyConfiguration IActiveMqEndpointConfiguration.Topology => _endpointConfiguration.Topology;

    /// <summary>Creates the runtime ActiveMQ receive-endpoint context.</summary>
    /// <returns>The configured receive-endpoint context.</returns>
    public override ReceiveEndpointContext CreateReceiveEndpointContext()
    {
        return CreateActiveMqReceiveEndpointContext();
    }

    /// <summary>Builds and registers the receive transport and endpoint with a host.</summary>
    /// <param name="host">The host that owns the endpoint.</param>
    public void Build(IHost host)
    {
        var context = CreateActiveMqReceiveEndpointContext();

        _sessionConfigurator.UseFilter(new ConfigureActiveMqTopologyFilter<ReceiveSettings>(_settings, context.BrokerTopology, context));

        if (_hostConfiguration.DeployTopologyOnly)
            _sessionConfigurator.UseFilter(new TransportReadyFilter<SessionContext>(context));
        else
        {
            _sessionConfigurator.UseFilter(new ReceiveEndpointDependencyFilter<SessionContext>(context));
            _sessionConfigurator.UseFilter(new ActiveMqConsumerFilter(context));
        }

        IPipe<SessionContext> sessionPipe = _sessionConfigurator.Build();

        var transport = new ReceiveTransport<SessionContext>(_hostConfiguration, context,
            () => context.SessionContextSupervisor, sessionPipe);

        if (IsBusEndpoint && _hostConfiguration.DeployPublishTopology)
        {
            var publishTopology = _hostConfiguration.Topology.PublishTopology;

            var brokerTopology = publishTopology.GetPublishBrokerTopology();

            transport.PreStartPipe = new ConfigureActiveMqTopologyFilter<IPublishTopology>(publishTopology, brokerTopology, context).ToPipe();
        }

        var receiveEndpoint = new ReceiveEndpoint(transport, context);

        var queueName = _settings.EntityName ?? NewId.Next().ToString(FormatUtil.Formatter);

        host.AddReceiveEndpoint(queueName, receiveEndpoint);

        ReceiveEndpoint = receiveEndpoint;
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        var queueName = $"{_settings.EntityName}";

        if (!ActiveMqEntityNameValidator.Validator.IsValidEntityName(_settings.EntityName))
            yield return this.Failure(queueName, "must be a valid queue name");

        foreach (var result in base.Validate())
            yield return result.WithParentKey(queueName);
    }

    /// <summary>Sets whether the queue persists across broker restarts.</summary>
    public bool Durable
    {
        set
        {
            _settings.Durable = value;

            Changed("Durable");
        }
    }

    /// <summary>Sets whether the broker removes the queue when it is no longer used.</summary>
    public bool AutoDelete
    {
        set
        {
            _settings.AutoDelete = value;

            Changed("AutoDelete");
        }
    }

    /// <summary>Binds a named topic to the receive queue.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="configure">An optional callback that configures the topic binding.</param>
    public void Bind(string topicName, Action<IActiveMqTopicBindingConfigurator>? configure = null)
    {
        if (topicName == null)
            throw new ArgumentNullException(nameof(topicName));

        _endpointConfiguration.Topology.Consume.Bind(topicName, configure);
    }

    /// <summary>Binds the publish topic for a message type to the receive queue.</summary>
    /// <typeparam name="T">The message type whose topic is bound.</typeparam>
    /// <param name="configure">An optional callback that configures the topic binding.</param>
    public void Bind<T>(Action<IActiveMqTopicBindingConfigurator>? configure = null)
        where T : class
    {
        _endpointConfiguration.Topology.Consume.GetMessageTopology<T>().Bind(configure);
    }

    /// <summary>Configures the Apache NMS session pipeline used by the endpoint.</summary>
    /// <param name="configure">The callback that configures the session pipeline.</param>
    public void ConfigureSession(Action<IPipeConfigurator<SessionContext>> configure)
    {
        configure?.Invoke(_sessionConfigurator);
    }

    ActiveMqReceiveEndpointContext CreateActiveMqReceiveEndpointContext()
    {
        var builder = new ActiveMqReceiveEndpointBuilder(_hostConfiguration, this);

        ApplySpecifications(builder);

        return builder.CreateReceiveEndpointContext();
    }

    Uri FormatInputAddress()
    {
        return _settings.GetInputAddress(_hostConfiguration.HostAddress);
    }

    /// <summary>Determines whether the endpoint input address or base configuration has already been materialized.</summary>
    /// <returns><see langword="true" /> when configuration can no longer be changed safely; otherwise, <see langword="false" />.</returns>
    protected override bool IsAlreadyConfigured()
    {
        return _inputAddress.IsValueCreated || base.IsAlreadyConfigured();
    }
}
