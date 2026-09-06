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

/// <summary>Configures the common entity, processor, and receive pipeline for queue and subscription endpoints.</summary>
public abstract class ServiceBusEntityReceiveEndpointConfiguration :
    ReceiveEndpointConfiguration
{
    readonly IServiceBusEndpointEntityConfigurator _configurator;
    readonly IServiceBusHostConfiguration _hostConfiguration;
    readonly BaseClientSettings _settings;
    /// <summary>Builds the client-context pipeline that owns processor startup and message delivery.</summary>
    protected readonly IBuildPipeConfigurator<ClientContext> ClientPipeConfigurator;

    /// <summary>Initializes common receive-endpoint state for an Azure Service Bus entity.</summary>
    /// <param name="hostConfiguration">The namespace host configuration.</param>
    /// <param name="settings">The entity and processor settings.</param>
    /// <param name="endpointConfiguration">The endpoint pipeline and topology configuration.</param>
    protected ServiceBusEntityReceiveEndpointConfiguration(IServiceBusHostConfiguration hostConfiguration, BaseClientSettings settings,
        IServiceBusEndpointConfiguration endpointConfiguration)
        : base(hostConfiguration, endpointConfiguration)
    {
        _hostConfiguration = hostConfiguration;
        _settings = settings;
        _configurator = settings.Configurator;

        ClientPipeConfigurator = new PipeConfigurator<ClientContext>();
    }

    /// <summary>Sets the endpoint's concurrent message limit.</summary>
    public int MaxConcurrentCalls
    {
        set => ConcurrentMessageLimit = value;
    }

    /// <summary>Sets the idle duration after which Azure Service Bus deletes the entity.</summary>
    public TimeSpan AutoDeleteOnIdle
    {
        set
        {
            _configurator.AutoDeleteOnIdle = value;

            Changed(nameof(AutoDeleteOnIdle));
        }
    }

    /// <summary>Sets the default lifetime of messages sent to the entity.</summary>
    public TimeSpan DefaultMessageTimeToLive
    {
        set => _configurator.DefaultMessageTimeToLive = value;
    }

    /// <summary>Sets whether server-side batched operations are enabled.</summary>
    public bool EnableBatchedOperations
    {
        set => _configurator.EnableBatchedOperations = value;
    }

    /// <summary>Sets whether expired messages are moved to the entity's dead-letter subqueue.</summary>
    public bool EnableDeadLetteringOnMessageExpiration
    {
        set => _configurator.EnableDeadLetteringOnMessageExpiration = value;
    }

    /// <summary>Sets the entity path to which dead-lettered messages are forwarded.</summary>
    public string ForwardDeadLetteredMessagesTo
    {
        set => _configurator.ForwardDeadLetteredMessagesTo = value;
    }

    /// <summary>Sets the initial lock duration for received messages.</summary>
    public TimeSpan LockDuration
    {
        set => _configurator.LockDuration = value;
    }

    /// <summary>Sets the delivery-attempt limit before a message is dead-lettered.</summary>
    public int MaxDeliveryCount
    {
        set => _configurator.MaxDeliveryCount = value;
    }

    /// <summary>Sets whether the entity requires sessions.</summary>
    public bool RequiresSession
    {
        set => _configurator.RequiresSession = value;
    }

    /// <summary>Sets the maximum number of sessions processed concurrently.</summary>
    public int MaxConcurrentSessions
    {
        set => _configurator.MaxConcurrentSessions = value;
    }

    /// <summary>Sets the maximum number of concurrent message callbacks for each session.</summary>
    public int MaxConcurrentCallsPerSession
    {
        set => _configurator.MaxConcurrentCallsPerSession = value;
    }

    /// <summary>Sets application-defined metadata stored with the entity.</summary>
    public string UserMetadata
    {
        set => _configurator.UserMetadata = value;
    }

    /// <summary>Sets the session idle timeout through the <c>MessageWaitTimeout</c> compatibility property.</summary>
    public TimeSpan MessageWaitTimeout
    {
        set => _settings.SessionIdleTimeout = value;
    }

    /// <summary>Sets the maximum idle time to wait for a message from an accepted session.</summary>
    public TimeSpan? SessionIdleTimeout
    {
        set => _settings.SessionIdleTimeout = value;
    }

    /// <summary>Sets the maximum duration for automatic message- or session-lock renewal.</summary>
    public TimeSpan MaxAutoRenewDuration
    {
        set => _settings.MaxAutoRenewDuration = value;
    }

    /// <summary>Combines client-pipeline, entity-setting, and base endpoint validation.</summary>
    /// <returns>All validation failures found in the combined configuration.</returns>
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

    /// <summary>Builds the client pipeline and registers the receive endpoint with its host.</summary>
    /// <param name="host">The host that owns the receive endpoint.</param>
    /// <param name="receiveEndpointContext">The endpoint context used by the receive transport.</param>
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
