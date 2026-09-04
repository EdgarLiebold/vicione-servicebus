using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.SqlTransport.Middleware;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>
/// Provides a sql receive endpoint configuration implementation.
/// </summary>
public class SqlReceiveEndpointConfiguration :
    ReceiveEndpointConfiguration,
    ISqlReceiveEndpointConfiguration,
    ISqlReceiveEndpointConfigurator
{
    static readonly Regex _regex = new Regex(@"^[A-Za-z0-9\-_\.:]+$", RegexOptions.Compiled);

    readonly IBuildPipeConfigurator<ClientContext> _clientConfigurator;
    readonly ISqlEndpointConfiguration _endpointConfiguration;
    readonly ISqlHostConfiguration _hostConfiguration;
    readonly Lazy<Uri> _inputAddress;
    readonly SqlReceiveSettings _settings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="settings">The settings value.</param>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
    public SqlReceiveEndpointConfiguration(ISqlHostConfiguration hostConfiguration, SqlReceiveSettings settings,
        ISqlEndpointConfiguration endpointConfiguration)
        : base(hostConfiguration, endpointConfiguration)
    {
        _hostConfiguration = hostConfiguration;
        _settings = settings;

        _endpointConfiguration = endpointConfiguration;

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
        return CreateDbReceiveEndpointContext();
    }

    ISqlTopologyConfiguration ISqlEndpointConfiguration.Topology => _endpointConfiguration.Topology;

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <param name="host">The host value.</param>
    public void Build(IHost host)
    {
        var context = CreateDbReceiveEndpointContext();

        _clientConfigurator.UseFilter(new ConfigureSqlTopologyFilter<ReceiveSettings>(_settings, context.BrokerTopology, context));

        if (_hostConfiguration.DeployTopologyOnly)
            _clientConfigurator.UseFilter(new TransportReadyFilter<ClientContext>(context));
        else
        {
            if (_settings.PurgeOnStartup)
                _clientConfigurator.UseFilter(new PurgeOnStartupFilter(_settings.QueueName));

            _clientConfigurator.UseFilter(new ReceiveEndpointDependencyFilter<ClientContext>(context));
            _clientConfigurator.UseFilter(new SqlConsumerFilter(context));
        }

        IPipe<ClientContext> clientPipe = _clientConfigurator.Build();

        var transport = new ReceiveTransport<ClientContext>(_hostConfiguration, context, () => context.ClientContextSupervisor, clientPipe);

        if (IsBusEndpoint && _hostConfiguration.DeployPublishTopology)
        {
            var publishTopology = _hostConfiguration.Topology.PublishTopology;

            var brokerTopology = publishTopology.GetPublishBrokerTopology();

            transport.PreStartPipe = new ConfigureSqlTopologyFilter<IPublishTopology>(publishTopology, brokerTopology).ToPipe();
        }

        var receiveEndpoint = new ReceiveEndpoint(transport, context);

        var queueName = _settings.QueueName ?? NewId.Next().ToString(FormatUtil.Formatter);

        host.AddReceiveEndpoint(queueName, receiveEndpoint);

        ReceiveEndpoint = receiveEndpoint;
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        if (!IsValidEntityName(_settings.QueueName))
            yield return this.Failure(_settings.QueueName, "Must be a valid queue name");

        if (_settings.PurgeOnStartup)
            yield return this.Warning(_settings.QueueName, "Existing messages will be purged on service start");

        if (_settings.MaintenanceBatchSize <= 0)
            yield return this.Failure(_settings.QueueName, nameof(_settings.MaintenanceBatchSize), "Must be >= 1");

        if (_settings.LockDuration < TimeSpan.FromSeconds(1))
            yield return this.Failure(_settings.QueueName, nameof(_settings.LockDuration), "Must be >= 1 second");

        if (_settings.UnlockDelay.HasValue && _settings.UnlockDelay < TimeSpan.Zero)
            yield return this.Failure(_settings.QueueName, nameof(_settings.UnlockDelay), "Must be > TimeSpan.Zero");

        foreach (var result in base.Validate())
            yield return result.WithParentKey(_settings.QueueName);
    }

    /// <summary>
    /// Gets or sets the auto delete on idle value.
    /// </summary>
    public TimeSpan? AutoDeleteOnIdle
    {
        set
        {
            _settings.AutoDeleteOnIdle = value;

            Changed("AutoDelete");
        }
    }

    /// <summary>
    /// Gets or sets the polling interval value.
    /// </summary>
    public TimeSpan PollingInterval
    {
        set => _settings.PollingInterval = value;
    }

    /// <summary>
    /// Gets or sets the lock duration value.
    /// </summary>
    public TimeSpan LockDuration
    {
        set => _settings.LockDuration = value;
    }

    /// <summary>
    /// Gets or sets the max lock duration value.
    /// </summary>
    public TimeSpan MaxLockDuration
    {
        set => _settings.MaxLockDuration = value;
    }

    /// <summary>
    /// Gets or sets the max delivery count value.
    /// </summary>
    public int? MaxDeliveryCount
    {
        set => _settings.MaxDeliveryCount = value;
    }

    /// <summary>
    /// Gets or sets the purge on startup value.
    /// </summary>
    public bool PurgeOnStartup
    {
        set => _settings.PurgeOnStartup = value;
    }

    /// <summary>
    /// Gets or sets the maintenance batch size value.
    /// </summary>
    public int MaintenanceBatchSize
    {
        set => _settings.MaintenanceBatchSize = value;
    }

    /// <summary>
    /// Gets or sets the dead letter expired messages value.
    /// </summary>
    public bool DeadLetterExpiredMessages
    {
        set => _settings.DeadLetterExpiredMessages = value;
    }

    /// <summary>
    /// Performs the subscribe operation.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="callback">The callback value.</param>
    public void Subscribe(string topicName, Action<ISqlTopicSubscriptionConfigurator>? callback)
    {
        if (topicName == null)
            throw new ArgumentNullException(nameof(topicName));

        _endpointConfiguration.Topology.Consume.Subscribe(topicName, callback);
    }

    /// <summary>
    /// Gets or sets the unlock delay value.
    /// </summary>
    public TimeSpan? UnlockDelay
    {
        set => _settings.UnlockDelay = value;
    }

    /// <summary>
    /// Gets or sets the concurrent delivery limit value.
    /// </summary>
    public int ConcurrentDeliveryLimit
    {
        set => _settings.ConcurrentDeliveryLimit = value;
    }

    /// <summary>
    /// Performs the subscribe operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="callback">The callback value.</param>
    public void Subscribe<T>(Action<ISqlTopicSubscriptionConfigurator>? callback)
        where T : class
    {
        _endpointConfiguration.Topology.Consume.GetMessageTopology<T>().Subscribe(callback);
    }

    /// <summary>
    /// Sets receive mode.
    /// </summary>
    /// <param name="mode">The mode value.</param>
    /// <param name="concurrentDeliveryLimit">The concurrent delivery limit value.</param>
    public void SetReceiveMode(SqlReceiveMode mode, int? concurrentDeliveryLimit = default)
    {
        if (concurrentDeliveryLimit != null)
            _settings.ConcurrentDeliveryLimit = concurrentDeliveryLimit.Value;

        _settings.ReceiveMode = mode;
    }

    /// <summary>
    /// Configures client.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void ConfigureClient(Action<IPipeConfigurator<ClientContext>>? configure)
    {
        configure?.Invoke(_clientConfigurator);
    }

    static bool IsValidEntityName(string name)
    {
        return _regex.Match(name).Success;
    }

    SqlReceiveEndpointContext CreateDbReceiveEndpointContext()
    {
        var builder = new SqlReceiveEndpointBuilder(_hostConfiguration, this);

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
