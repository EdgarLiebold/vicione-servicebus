using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.ActiveMq.Middleware;
using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>
/// Provides an active mq receive endpoint configuration implementation.
/// </summary>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="settings">The settings value.</param>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
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

    /// <summary>
    /// Gets the settings value.
    /// </summary>
    public ReceiveSettings Settings => _settings;
    /// <summary>
    /// Gets the host address value.
    /// </summary>
    public override Uri HostAddress => _hostConfiguration.HostAddress;
    /// <summary>
    /// Gets the input address value.
    /// </summary>
    public override Uri InputAddress => _inputAddress.Value;
    IActiveMqTopologyConfiguration IActiveMqEndpointConfiguration.Topology => _endpointConfiguration.Topology;

    /// <summary>
    /// Creates receive endpoint context.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override ReceiveEndpointContext CreateReceiveEndpointContext()
    {
        return CreateActiveMqReceiveEndpointContext();
    }

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <param name="host">The host value.</param>
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

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        var queueName = $"{_settings.EntityName}";

        if (!ActiveMqEntityNameValidator.Validator.IsValidEntityName(_settings.EntityName))
            yield return this.Failure(queueName, "must be a valid queue name");

        foreach (var result in base.Validate())
            yield return result.WithParentKey(queueName);
    }

    /// <summary>
    /// Gets or sets the durable value.
    /// </summary>
    public bool Durable
    {
        set
        {
            _settings.Durable = value;

            Changed("Durable");
        }
    }

    /// <summary>
    /// Gets or sets the auto delete value.
    /// </summary>
    public bool AutoDelete
    {
        set
        {
            _settings.AutoDelete = value;

            Changed("AutoDelete");
        }
    }

    /// <summary>
    /// Performs the bind operation.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void Bind(string topicName, Action<IActiveMqTopicBindingConfigurator>? configure = null)
    {
        if (topicName == null)
            throw new ArgumentNullException(nameof(topicName));

        _endpointConfiguration.Topology.Consume.Bind(topicName, configure);
    }

    /// <summary>
    /// Performs the bind operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    public void Bind<T>(Action<IActiveMqTopicBindingConfigurator>? configure = null)
        where T : class
    {
        _endpointConfiguration.Topology.Consume.GetMessageTopology<T>().Bind(configure);
    }

    /// <summary>
    /// Configures session.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
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

    /// <summary>
    /// Determines whether already configured.
    /// </summary>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    protected override bool IsAlreadyConfigured()
    {
        return _inputAddress.IsValueCreated || base.IsAlreadyConfigured();
    }
}
