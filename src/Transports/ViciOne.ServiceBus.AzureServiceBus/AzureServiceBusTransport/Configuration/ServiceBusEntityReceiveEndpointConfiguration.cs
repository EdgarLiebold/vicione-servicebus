using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.AzureServiceBus.Middleware;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Provides a service bus entity receive endpoint configuration implementation.
/// </summary>
public abstract class ServiceBusEntityReceiveEndpointConfiguration :
    ReceiveEndpointConfiguration
{
    readonly IServiceBusEndpointEntityConfigurator _configurator;
    readonly IServiceBusHostConfiguration _hostConfiguration;
    readonly BaseClientSettings _settings;
    /// <summary>
    /// Defines the client pipe configurator value.
    /// </summary>
    protected readonly IBuildPipeConfigurator<ClientContext> ClientPipeConfigurator;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="settings">The settings value.</param>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
    protected ServiceBusEntityReceiveEndpointConfiguration(IServiceBusHostConfiguration hostConfiguration, BaseClientSettings settings,
        IServiceBusEndpointConfiguration endpointConfiguration)
        : base(hostConfiguration, endpointConfiguration)
    {
        _hostConfiguration = hostConfiguration;
        _settings = settings;
        _configurator = settings.Configurator;

        ClientPipeConfigurator = new PipeConfigurator<ClientContext>();
    }

    /// <summary>
    /// Gets or sets the max concurrent calls value.
    /// </summary>
    public int MaxConcurrentCalls
    {
        set => ConcurrentMessageLimit = value;
    }

    /// <summary>
    /// Gets or sets the auto delete on idle value.
    /// </summary>
    public TimeSpan AutoDeleteOnIdle
    {
        set
        {
            _configurator.AutoDeleteOnIdle = value;

            Changed(nameof(AutoDeleteOnIdle));
        }
    }

    /// <summary>
    /// Gets or sets the default message time to live value.
    /// </summary>
    public TimeSpan DefaultMessageTimeToLive
    {
        set => _configurator.DefaultMessageTimeToLive = value;
    }

    /// <summary>
    /// Gets or sets the enable batched operations value.
    /// </summary>
    public bool EnableBatchedOperations
    {
        set => _configurator.EnableBatchedOperations = value;
    }

    /// <summary>
    /// Gets or sets the enable dead lettering on message expiration value.
    /// </summary>
    public bool EnableDeadLetteringOnMessageExpiration
    {
        set => _configurator.EnableDeadLetteringOnMessageExpiration = value;
    }

    /// <summary>
    /// Gets or sets the forward dead lettered messages to value.
    /// </summary>
    public string ForwardDeadLetteredMessagesTo
    {
        set => _configurator.ForwardDeadLetteredMessagesTo = value;
    }

    /// <summary>
    /// Gets or sets the lock duration value.
    /// </summary>
    public TimeSpan LockDuration
    {
        set => _configurator.LockDuration = value;
    }

    /// <summary>
    /// Gets or sets the max delivery count value.
    /// </summary>
    public int MaxDeliveryCount
    {
        set => _configurator.MaxDeliveryCount = value;
    }

    /// <summary>
    /// Gets or sets the requires session value.
    /// </summary>
    public bool RequiresSession
    {
        set => _configurator.RequiresSession = value;
    }

    /// <summary>
    /// Gets or sets the max concurrent sessions value.
    /// </summary>
    public int MaxConcurrentSessions
    {
        set => _configurator.MaxConcurrentSessions = value;
    }

    /// <summary>
    /// Gets or sets the max concurrent calls per session value.
    /// </summary>
    public int MaxConcurrentCallsPerSession
    {
        set => _configurator.MaxConcurrentCallsPerSession = value;
    }

    /// <summary>
    /// Gets or sets the user metadata value.
    /// </summary>
    public string UserMetadata
    {
        set => _configurator.UserMetadata = value;
    }

    /// <summary>
    /// Gets or sets the message wait timeout value.
    /// </summary>
    public TimeSpan MessageWaitTimeout
    {
        set => _settings.SessionIdleTimeout = value;
    }

    /// <summary>
    /// Gets or sets the session idle timeout value.
    /// </summary>
    public TimeSpan? SessionIdleTimeout
    {
        set => _settings.SessionIdleTimeout = value;
    }

    /// <summary>
    /// Gets or sets the max auto renew duration value.
    /// </summary>
    public TimeSpan MaxAutoRenewDuration
    {
        set => _settings.MaxAutoRenewDuration = value;
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return ClientPipeConfigurator.Validate()
            .Concat(ValidateSettings())
            .Concat(base.Validate());
    }

    IEnumerable<ValidationResult> ValidateSettings()
    {
        if (_settings.PrefetchCount < 0)
            yield return this.Failure("PrefetchCount", "must be >= 0");

        if (_settings.MaxConcurrentCalls <= 0)
            yield return this.Failure("MaxConcurrentCalls", "must be > 0");
    }

    /// <summary>
    /// Creates receive endpoint.
    /// </summary>
    /// <param name="host">The host value.</param>
    /// <param name="receiveEndpointContext">The receive endpoint context value.</param>
    protected void CreateReceiveEndpoint(IHost host, ServiceBusReceiveEndpointContext receiveEndpointContext)
    {
        if (_hostConfiguration.DeployTopologyOnly)
            ClientPipeConfigurator.UseFilter(new TransportReadyFilter<ClientContext>(receiveEndpointContext));
        else
        {
            ClientPipeConfigurator.UseFilter(new ReceiveEndpointDependencyFilter<ClientContext>(receiveEndpointContext));
            ClientPipeConfigurator.UseFilter(_settings.RequiresSession
                ? new MessageSessionReceiverFilter(receiveEndpointContext)
                : new MessageReceiverFilter(receiveEndpointContext));
        }

        IPipe<ClientContext> clientPipe = ClientPipeConfigurator.Build();

        var transport = new ReceiveTransport<ClientContext>(_hostConfiguration, receiveEndpointContext, () => receiveEndpointContext
            .ClientContextSupervisor, clientPipe);

        if (IsBusEndpoint && _hostConfiguration.DeployPublishTopology)
        {
            var publishTopology = _hostConfiguration.Topology.PublishTopology;

            var brokerTopology = publishTopology.GetPublishBrokerTopology();

            transport.PreStartPipe = new ConfigureServiceBusTopologyFilter<IPublishTopology>(publishTopology, brokerTopology).ToPipe<ClientContext>();
        }

        var receiveEndpoint = new ReceiveEndpoint(transport, receiveEndpointContext);

        var queueName = _settings.Path ?? NewId.Next().ToString(FormatUtil.Formatter);

        host.AddReceiveEndpoint(queueName, receiveEndpoint);

        ReceiveEndpoint = receiveEndpoint;
    }
}
