using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.AmazonSqs.Middleware;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>
/// Provides an amazon sqs receive endpoint configuration implementation.
/// </summary>
public class AmazonSqsReceiveEndpointConfiguration :
    ReceiveEndpointConfiguration,
    IAmazonSqsReceiveEndpointConfiguration,
    IAmazonSqsReceiveEndpointConfigurator
{
    readonly IBuildPipeConfigurator<ClientContext> _clientConfigurator;
    readonly IBuildPipeConfigurator<ConnectionContext> _connectionConfigurator;
    readonly IAmazonSqsEndpointConfiguration _endpointConfiguration;
    readonly IAmazonSqsHostConfiguration _hostConfiguration;
    readonly Lazy<Uri> _inputAddress;
    readonly QueueReceiveSettings _settings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="settings">The settings value.</param>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
    public AmazonSqsReceiveEndpointConfiguration(IAmazonSqsHostConfiguration hostConfiguration, QueueReceiveSettings settings,
        IAmazonSqsEndpointConfiguration endpointConfiguration)
        : base(hostConfiguration, endpointConfiguration)
    {
        _settings = settings;

        _hostConfiguration = hostConfiguration;
        _endpointConfiguration = endpointConfiguration;

        _connectionConfigurator = new PipeConfigurator<ConnectionContext>();
        _clientConfigurator = new PipeConfigurator<ClientContext>();

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

    /// <summary>
    /// Creates receive endpoint context.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override ReceiveEndpointContext CreateReceiveEndpointContext()
    {
        return CreateSqsReceiveEndpointContext();
    }

    IAmazonSqsTopologyConfiguration IAmazonSqsEndpointConfiguration.Topology => _endpointConfiguration.Topology;

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <param name="host">The host value.</param>
    public void Build(IHost host)
    {
        var context = CreateSqsReceiveEndpointContext();

        _clientConfigurator.UseFilter(new ConfigureAmazonSqsTopologyFilter<ReceiveSettings>(_settings, context.BrokerTopology, context));

        if (_hostConfiguration.DeployTopologyOnly)
            _clientConfigurator.UseFilter(new TransportReadyFilter<ClientContext>(context));
        else
        {
            if (_settings.PurgeOnStartup)
                _clientConfigurator.UseFilter(new PurgeOnStartupFilter(_settings.EntityName));

            _clientConfigurator.UseFilter(new ReceiveEndpointDependencyFilter<ClientContext>(context));
            _clientConfigurator.UseFilter(new AmazonSqsConsumerFilter(context));
        }

        IPipe<ClientContext> clientPipe = _clientConfigurator.Build();

        var transport = new ReceiveTransport<ClientContext>(_hostConfiguration, context,
            () => context.ClientContextSupervisor, clientPipe);

        if (IsBusEndpoint && _hostConfiguration.DeployPublishTopology)
        {
            var publishTopology = _hostConfiguration.Topology.PublishTopology;

            var brokerTopology = publishTopology.GetPublishBrokerTopology();

            transport.PreStartPipe = new ConfigureAmazonSqsTopologyFilter<IPublishTopology>(publishTopology, brokerTopology).ToPipe();
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
        if (_settings.PrefetchCount <= 0)
            yield return this.Failure("PrefetchCount", "must be >= 1");

        if (_settings.ConcurrentMessageLimit <= 0)
            yield return this.Failure("ConcurrentMessageLimit", "must be >= 1");

        if (_settings.ConcurrentDeliveryLimit <= 0)
            yield return this.Failure("ConcurrentDeliveryLimit", "must be >= 1");

        if (_settings.WaitTimeSeconds is < 0 or > AmazonSqsReceiveSettingsLimits.MaximumWaitTimeSeconds)
            yield return this.Failure("WaitTimeSeconds", $"must be between 0 and {AmazonSqsReceiveSettingsLimits.MaximumWaitTimeSeconds}");

        if (_settings.RedeliverVisibilityTimeout is < 0 or > AmazonSqsReceiveSettingsLimits.MaximumVisibilityTimeoutSeconds)
            yield return this.Failure("RedeliverVisibilityTimeout", $"must be between 0 and {AmazonSqsReceiveSettingsLimits.MaximumVisibilityTimeoutSeconds}");

        var queueName = $"{_settings.EntityName}";

        if (!AmazonSqsEntityNameValidator.Validator.IsValidEntityName(_settings.EntityName))
            yield return this.Failure(queueName, "must be a valid queue name");

        if (_settings.PurgeOnStartup)
            yield return this.Warning(queueName, "Existing messages in the queue will be purged on service start");

        var visibilityTimeout = TimeSpan.FromSeconds(_settings.VisibilityTimeout);
        if (_settings.MaxVisibilityTimeout < visibilityTimeout)
            yield return this.Failure("MaxVisibilityTimeout", "Must be greater than or equal to VisibilityTimeout");

        if (_settings.MaxVisibilityTimeoutRenewal < 0)
            yield return this.Failure("MaxVisibilityTimeoutRenewal", "must be >= 0 (values less than 60 will be set to 60)");

        if (_settings.MaxVisibilityTimeoutRenewal > AmazonSqsReceiveSettingsLimits.MaximumVisibilityTimeoutSeconds)
            yield return this.Failure("MaxVisibilityTimeoutRenewal", $"must be <= {AmazonSqsReceiveSettingsLimits.MaximumVisibilityTimeoutSeconds} seconds (12 hours per AWS SQS limits)");

        if (_settings.QueueAttributes.Keys.Any(key => string.Equals(key, global::Amazon.SQS.QueueAttributeName.RedrivePolicy, StringComparison.Ordinal)))
            yield return this.Failure("RedrivePolicy", "must not be configured while ViciOne owns the distinct error and skipped queues");

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
    /// Gets or sets the concurrent delivery limit value.
    /// </summary>
    public int ConcurrentDeliveryLimit
    {
        set => _settings.ConcurrentDeliveryLimit = AmazonSqsReceiveSettingsLimits.PositiveConcurrency(value, nameof(ConcurrentDeliveryLimit));
    }

    /// <summary>
    /// Gets or sets the wait time seconds value.
    /// </summary>
    public ushort WaitTimeSeconds
    {
        set => _settings.WaitTimeSeconds = AmazonSqsReceiveSettingsLimits.WaitTimeSeconds(value);
    }

    /// <summary>
    /// Gets or sets the purge on startup value.
    /// </summary>
    public bool PurgeOnStartup
    {
        set => _settings.PurgeOnStartup = value;
    }

    /// <summary>
    /// Gets the queue attributes value.
    /// </summary>
    public IDictionary<string, object> QueueAttributes => _settings.QueueAttributes;
    /// <summary>
    /// Gets the queue subscription attributes value.
    /// </summary>
    public IDictionary<string, object> QueueSubscriptionAttributes => _settings.QueueSubscriptionAttributes;
    /// <summary>
    /// Gets the queue tags value.
    /// </summary>
    public IDictionary<string, string> QueueTags => _settings.QueueTags;

    /// <summary>
    /// Performs the subscribe operation.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void Subscribe(string topicName, Action<IAmazonSqsTopicSubscriptionConfigurator>? configure = null)
    {
        if (topicName == null)
            throw new ArgumentNullException(nameof(topicName));

        _endpointConfiguration.Topology.Consume.Bind(topicName, configure);
    }

    /// <summary>
    /// Gets or sets the redeliver visibility timeout value.
    /// </summary>
    public int RedeliverVisibilityTimeout
    {
        set => _settings.RedeliverVisibilityTimeout = AmazonSqsReceiveSettingsLimits.VisibilityTimeoutSeconds(value, nameof(RedeliverVisibilityTimeout));
    }

    /// <summary>
    /// Gets or sets the max visibility timeout value.
    /// </summary>
    public TimeSpan MaxVisibilityTimeout
    {
        set => _settings.MaxVisibilityTimeout = AmazonSqsReceiveSettingsLimits.MaximumVisibilityTimeout(value);
    }

    /// <summary>
    /// Gets or sets the max visibility timeout renewal value.
    /// </summary>
    public int MaxVisibilityTimeoutRenewal
    {
        set => _settings.MaxVisibilityTimeoutRenewal = AmazonSqsReceiveSettingsLimits.VisibilityRenewalSeconds(value);
    }

    /// <summary>
    /// Performs the subscribe operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    public void Subscribe<T>(Action<IAmazonSqsTopicSubscriptionConfigurator>? configure = null)
        where T : class
    {
        _endpointConfiguration.Topology.Consume.GetMessageTopology<T>().Subscribe(configure);
    }

    /// <summary>
    /// Configures client.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void ConfigureClient(Action<IPipeConfigurator<ClientContext>>? configure)
    {
        configure?.Invoke(_clientConfigurator);
    }

    /// <summary>
    /// Configures connection.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void ConfigureConnection(Action<IPipeConfigurator<ConnectionContext>>? configure)
    {
        configure?.Invoke(_connectionConfigurator);
    }

    /// <summary>
    /// Performs the disable message ordering operation.
    /// </summary>
    public void DisableMessageOrdering()
    {
        _settings.IsOrdered = false;
    }

    SqsReceiveEndpointContext CreateSqsReceiveEndpointContext()
    {
        var builder = new AmazonSqsReceiveEndpointBuilder(_hostConfiguration, this);

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
