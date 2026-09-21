using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.SqlTransport.Middleware;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Builds and validates one SQL transport receive endpoint.</summary>
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

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="settings">The settings that control the operation.</param>
    /// <param name="endpointConfiguration">The endpoint configuration.</param>
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

    /// <summary>Gets the settings.</summary>
    public ReceiveSettings Settings => _settings;

    /// <summary>Gets the host address.</summary>
    public override Uri HostAddress => _hostConfiguration.HostAddress;
    /// <summary>Gets the input address.</summary>
    public override Uri InputAddress => _inputAddress.Value;

    /// <summary>Creates receive endpoint context.</summary>
    /// <returns>The created receive endpoint context.</returns>
    public override ReceiveEndpointContext CreateReceiveEndpointContext()
    {
        return CreateSqlReceiveEndpointContext();
    }

    ISqlTopologyConfiguration ISqlEndpointConfiguration.Topology => _endpointConfiguration.Topology;

    /// <summary>Builds the configured component.</summary>
    /// <param name="host">The host.</param>
    public void Build(IHost host)
    {
        var context = CreateSqlReceiveEndpointContext();

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

    /// <summary>Validates the queue identity, delivery limits, polling, locking, and maintenance settings.</summary>
    /// <returns>Every configuration failure and startup warning detected for this endpoint.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        foreach (ValidationResult result in ValidateQueueSettings())
            yield return result;

        foreach (ValidationResult result in ValidateDeliverySettings())
            yield return result;

        foreach (ValidationResult result in base.Validate())
            yield return result.WithParentKey(_settings.QueueName);
    }

    IEnumerable<ValidationResult> ValidateQueueSettings()
    {
        if (!IsValidEntityName(_settings.QueueName))
            yield return this.Failure(_settings.QueueName, "Must be a valid queue name");

        if (_settings.PurgeOnStartup)
            yield return this.Warning(_settings.QueueName, "Existing messages will be purged on service start");

        if (_settings.MaintenanceBatchSize <= 0)
            yield return this.Failure(_settings.QueueName, nameof(_settings.MaintenanceBatchSize), "Must be >= 1");

        if (_settings.AutoDeleteOnIdle.HasValue && _settings.AutoDeleteOnIdle <= TimeSpan.Zero)
            yield return this.Failure(_settings.QueueName, nameof(_settings.AutoDeleteOnIdle), "Must be greater than zero when specified");

        if (_settings.PollingInterval <= TimeSpan.Zero)
            yield return this.Failure(_settings.QueueName, nameof(_settings.PollingInterval), "Must be greater than zero");
    }

    IEnumerable<ValidationResult> ValidateDeliverySettings()
    {
        if (_settings.LockDuration < TimeSpan.FromSeconds(1))
            yield return this.Failure(_settings.QueueName, nameof(_settings.LockDuration), "Must be >= 1 second");

        if (_settings.MaxLockDuration < _settings.LockDuration)
            yield return this.Failure(_settings.QueueName, nameof(_settings.MaxLockDuration), "Must be greater than or equal to LockDuration");

        if (_settings.MaxDeliveryCount is <= 0)
            yield return this.Failure(_settings.QueueName, nameof(_settings.MaxDeliveryCount), "Must be greater than zero when specified");

        if (_settings.UnlockDelay.HasValue && _settings.UnlockDelay < TimeSpan.Zero)
            yield return this.Failure(_settings.QueueName, nameof(_settings.UnlockDelay), "Must not be less than TimeSpan.Zero");

        if (_settings.ConcurrentDeliveryLimit <= 0)
            yield return this.Failure(_settings.QueueName, nameof(_settings.ConcurrentDeliveryLimit), "Must be greater than zero");

        if (!Enum.IsDefined(_settings.ReceiveMode))
            yield return this.Failure(_settings.QueueName, nameof(_settings.ReceiveMode), "Must be a defined SQL receive mode");
    }

    /// <summary>Gets or sets the auto delete on idle.</summary>
    public TimeSpan? AutoDeleteOnIdle
    {
        set
        {
            _settings.AutoDeleteOnIdle = value;

            Changed("AutoDelete");
        }
    }

    /// <summary>Gets or sets the polling interval.</summary>
    public TimeSpan PollingInterval
    {
        set => _settings.PollingInterval = value;
    }

    /// <summary>Gets or sets the lock duration.</summary>
    public TimeSpan LockDuration
    {
        set => _settings.LockDuration = value;
    }

    /// <summary>Gets or sets the max lock duration.</summary>
    public TimeSpan MaxLockDuration
    {
        set => _settings.MaxLockDuration = value;
    }

    /// <summary>Gets or sets the max delivery count.</summary>
    public int? MaxDeliveryCount
    {
        set => _settings.MaxDeliveryCount = value;
    }

    /// <summary>Gets or sets the purge on startup.</summary>
    public bool PurgeOnStartup
    {
        set => _settings.PurgeOnStartup = value;
    }

    /// <summary>Gets or sets the maintenance batch size.</summary>
    public int MaintenanceBatchSize
    {
        set => _settings.MaintenanceBatchSize = value;
    }

    /// <summary>Gets or sets the dead letter expired messages.</summary>
    public bool DeadLetterExpiredMessages
    {
        set => _settings.DeadLetterExpiredMessages = value;
    }

    /// <summary>Subscribes to the configured event source.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    public void Subscribe(string topicName, Action<ISqlTopicSubscriptionConfigurator>? callback)
    {
        if (topicName == null)
            throw new ArgumentNullException(nameof(topicName));

        _endpointConfiguration.Topology.Consume.Subscribe(topicName, callback);
    }

    /// <summary>Gets or sets the unlock delay.</summary>
    public TimeSpan? UnlockDelay
    {
        set => _settings.UnlockDelay = value;
    }

    /// <summary>Gets or sets the concurrent delivery limit.</summary>
    public int ConcurrentDeliveryLimit
    {
        set => _settings.ConcurrentDeliveryLimit = value;
    }

    /// <summary>Subscribes to the configured event source.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="callback">The callback invoked by the operation.</param>
    public void Subscribe<T>(Action<ISqlTopicSubscriptionConfigurator>? callback)
        where T : class
    {
        _endpointConfiguration.Topology.Consume.GetMessageTopology<T>().Subscribe(callback);
    }

    /// <summary>Sets receive mode.</summary>
    /// <param name="mode">The mode.</param>
    /// <param name="concurrentDeliveryLimit">The concurrent delivery limit.</param>
    public void SetReceiveMode(SqlReceiveMode mode, int? concurrentDeliveryLimit = default)
    {
        if (concurrentDeliveryLimit != null)
            _settings.ConcurrentDeliveryLimit = concurrentDeliveryLimit.Value;

        _settings.ReceiveMode = mode;
    }

    void IPartitionedReceiveEndpointConfigurator.SetPartitionedReceive()
    {
        SetReceiveMode(SqlReceiveMode.Partitioned);
    }

    /// <summary>Configures client.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    public void ConfigureClient(Action<IPipeConfigurator<ClientContext>>? configure)
    {
        configure?.Invoke(_clientConfigurator);
    }

    static bool IsValidEntityName(string name)
    {
        return _regex.Match(name).Success;
    }

    SqlReceiveEndpointContext CreateSqlReceiveEndpointContext()
    {
        var builder = new SqlReceiveEndpointBuilder(_hostConfiguration, this);

        ApplySpecifications(builder);

        return builder.CreateReceiveEndpointContext();
    }

    Uri FormatInputAddress()
    {
        return _settings.GetInputAddress(_hostConfiguration.HostAddress);
    }

    /// <summary>Determines whether already configured.</summary>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    protected override bool IsAlreadyConfigured()
    {
        return _inputAddress.IsValueCreated || base.IsAlreadyConfigured();
    }
}
