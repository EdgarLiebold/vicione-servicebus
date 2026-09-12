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

/// <summary>Configures, validates, and builds an Amazon SQS receive endpoint.</summary>
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

    /// <summary>Initializes an Amazon SQS receive-endpoint configuration.</summary>
    /// <param name="hostConfiguration">The owning host configuration.</param>
    /// <param name="settings">The queue and receive settings.</param>
    /// <param name="endpointConfiguration">The endpoint-level pipeline and topology configuration.</param>
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

    /// <summary>Gets the queue and receive settings.</summary>
    public ReceiveSettings Settings => _settings;
    /// <summary>Gets the configured Amazon SQS host address.</summary>
    public override Uri HostAddress => _hostConfiguration.HostAddress;
    /// <summary>Gets the formatted queue input address.</summary>
    public override Uri InputAddress => _inputAddress.Value;

    /// <summary>Builds and returns the Amazon SQS receive-endpoint context.</summary>
    /// <returns>The configured receive-endpoint context.</returns>
    public override ReceiveEndpointContext CreateReceiveEndpointContext()
    {
        return CreateSqsReceiveEndpointContext();
    }

    IAmazonSqsTopologyConfiguration IAmazonSqsEndpointConfiguration.Topology => _endpointConfiguration.Topology;

    /// <summary>Builds the client pipeline and registers the receive endpoint with its host.</summary>
    /// <param name="host">The host that owns the receive endpoint.</param>
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

    /// <summary>Validates queue names, concurrency, polling, visibility, purge, and topology settings.</summary>
    /// <returns>All detected validation failures and warnings.</returns>
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

        if (_settings.QueueSubscriptionAttributes.TryGetValue("RawMessageDelivery", out object? rawMessageDelivery))
        {
            if (rawMessageDelivery is not string value || !bool.TryParse(value, out bool rawDelivery))
                yield return this.Failure("RawMessageDelivery", "must be the string 'true' or 'false'");
            else if (_settings.RequiresSnsNotificationEnvelope == rawDelivery)
            {
                yield return this.Failure(
                    "RawMessageDelivery",
                    "must be configured together with RequireSnsNotificationEnvelope so delivery and receive formats agree");
            }
        }
        else if (_settings.RequiresSnsNotificationEnvelope)
            yield return this.Failure("RawMessageDelivery", "must be 'false' when an Amazon SNS notification envelope is required");

        foreach (var result in base.Validate())
            yield return result.WithParentKey(queueName);
    }

    /// <summary>Sets whether the queue is retained when the endpoint stops.</summary>
    public bool Durable
    {
        set
        {
            _settings.Durable = value;

            Changed("Durable");
        }
    }

    /// <summary>Sets whether the queue is deleted when the endpoint stops.</summary>
    public bool AutoDelete
    {
        set
        {
            _settings.AutoDelete = value;

            Changed("AutoDelete");
        }
    }

    /// <summary>Sets the maximum number of messages delivered concurrently by the endpoint.</summary>
    public int ConcurrentDeliveryLimit
    {
        set => _settings.ConcurrentDeliveryLimit = AmazonSqsReceiveSettingsLimits.PositiveConcurrency(value, nameof(ConcurrentDeliveryLimit));
    }

    /// <summary>Sets the Amazon SQS long-poll wait time, in seconds.</summary>
    public ushort WaitTimeSeconds
    {
        set => _settings.WaitTimeSeconds = AmazonSqsReceiveSettingsLimits.WaitTimeSeconds(value);
    }

    /// <summary>Sets whether available messages are purged when the endpoint starts.</summary>
    public bool PurgeOnStartup
    {
        set => _settings.PurgeOnStartup = value;
    }

    /// <summary>Gets the Amazon SQS queue attributes.</summary>
    public IDictionary<string, object> QueueAttributes => _settings.QueueAttributes;
    /// <summary>Gets the Amazon SNS attributes applied to subscriptions targeting the queue.</summary>
    public IDictionary<string, object> QueueSubscriptionAttributes => _settings.QueueSubscriptionAttributes;
    /// <summary>Gets the tags applied to the queue.</summary>
    public IDictionary<string, string> QueueTags => _settings.QueueTags;

    /// <summary>Subscribes the receive queue to a named Amazon SNS topic.</summary>
    /// <param name="topicName">The source topic name.</param>
    /// <param name="configure">An optional callback that configures the subscription.</param>
    public void Subscribe(string topicName, Action<IAmazonSqsTopicSubscriptionConfigurator>? configure = null)
    {
        if (topicName == null)
            throw new ArgumentNullException(nameof(topicName));

        _endpointConfiguration.Topology.Consume.Bind(topicName, configure);
    }

    /// <summary>Sets the visibility delay applied after message processing faults.</summary>
    public int RedeliverVisibilityTimeout
    {
        set => _settings.RedeliverVisibilityTimeout = AmazonSqsReceiveSettingsLimits.VisibilityTimeoutSeconds(value, nameof(RedeliverVisibilityTimeout));
    }

    /// <summary>Sets the maximum total duration for automatic message-visibility renewal.</summary>
    public TimeSpan MaxVisibilityTimeout
    {
        set => _settings.MaxVisibilityTimeout = AmazonSqsReceiveSettingsLimits.MaximumVisibilityTimeout(value);
    }

    /// <summary>Sets the maximum number of seconds requested by an individual visibility renewal.</summary>
    public int MaxVisibilityTimeoutRenewal
    {
        set => _settings.MaxVisibilityTimeoutRenewal = AmazonSqsReceiveSettingsLimits.VisibilityRenewalSeconds(value);
    }

    /// <summary>Subscribes the receive queue to the Amazon SNS topic for a message type.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="configure">An optional callback that configures the subscription.</param>
    public void Subscribe<T>(Action<IAmazonSqsTopicSubscriptionConfigurator>? configure = null)
        where T : class
    {
        _endpointConfiguration.Topology.Consume.GetMessageTopology<T>().Subscribe(configure);
    }

    /// <summary>Configures filters in the Amazon client-context pipeline.</summary>
    /// <param name="configure">The callback that updates the client-context pipe.</param>
    public void ConfigureClient(Action<IPipeConfigurator<ClientContext>>? configure)
    {
        configure?.Invoke(_clientConfigurator);
    }

    /// <summary>Configures filters in the Amazon connection-context pipeline.</summary>
    /// <param name="configure">The callback that updates the connection-context pipe.</param>
    public void ConfigureConnection(Action<IPipeConfigurator<ConnectionContext>>? configure)
    {
        configure?.Invoke(_connectionConfigurator);
    }

    /// <summary>Disables raw subscription delivery and requires Amazon SNS notification envelopes on this receive endpoint.</summary>
    public void RequireSnsNotificationEnvelope()
    {
        _settings.QueueSubscriptionAttributes["RawMessageDelivery"] = "false";
        _settings.RequiresSnsNotificationEnvelope = true;
    }

    /// <summary>Disables the endpoint's ordered-delivery constraint.</summary>
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

    /// <summary>Determines whether the input address has been observed or the base configuration is already frozen.</summary>
    /// <returns><see langword="true"/> when the configuration can no longer be changed; otherwise, <see langword="false"/>.</returns>
    protected override bool IsAlreadyConfigured()
    {
        return _inputAddress.IsValueCreated || base.IsAlreadyConfigured();
    }
}
