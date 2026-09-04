using System;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a service bus entity receive endpoint context implementation.
/// </summary>
public sealed class ServiceBusEntityReceiveEndpointContext :
    BaseReceiveEndpointContext,
    ServiceBusReceiveEndpointContext
{
    readonly Recycle<IClientContextSupervisor> _clientContext;
    readonly IServiceBusHostConfiguration _hostConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="configuration">The configuration callback.</param>
    /// <param name="brokerTopology">The broker topology value.</param>
    /// <param name="supervisorFactory">The supervisor factory value.</param>
    public ServiceBusEntityReceiveEndpointContext(IServiceBusHostConfiguration hostConfiguration, IServiceBusEntityEndpointConfiguration configuration,
        BrokerTopology brokerTopology, Func<IClientContextSupervisor> supervisorFactory)
        : base(hostConfiguration, configuration)
    {
        _hostConfiguration = hostConfiguration;

        BrokerTopology = brokerTopology;

        GetOrAddPayload(() => _hostConfiguration.Topology);

        _clientContext = new Recycle<IClientContextSupervisor>(supervisorFactory);
    }

    /// <summary>
    /// Gets the broker topology value.
    /// </summary>
    public BrokerTopology BrokerTopology { get; }

    /// <summary>
    /// Gets the client context supervisor value.
    /// </summary>
    public IClientContextSupervisor ClientContextSupervisor => _clientContext.Supervisor;

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public override void Probe(ProbeContext context)
    {
        context.Set(new
        {
            Type = "Azure Service Bus",
            PrefetchCount,
            ConcurrentMessageLimit
        });

        BrokerTopology.Probe(context);
    }

    /// <summary>
    /// Adds send agent to the configuration.
    /// </summary>
    /// <param name="agent">The agent value.</param>
    public override void AddSendAgent(IAgent agent)
    {
        _clientContext.Supervisor.AddSendAgent(agent);
    }

    /// <summary>
    /// Adds consume agent to the configuration.
    /// </summary>
    /// <param name="agent">The agent value.</param>
    public override void AddConsumeAgent(IAgent agent)
    {
        _clientContext.Supervisor.AddConsumeAgent(agent);
    }

    /// <summary>
    /// Performs the convert exception operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public override Exception ConvertException(Exception exception, string message)
    {
        return new ServiceBusConnectionException(message + InputAddress, exception);
    }

    /// <summary>
    /// Creates send transport provider.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override ISendTransportProvider CreateSendTransportProvider()
    {
        return new ServiceBusSendTransportProvider(_hostConfiguration.ConnectionContextSupervisor, this);
    }

    /// <summary>
    /// Creates publish transport provider.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override IPublishTransportProvider CreatePublishTransportProvider()
    {
        return new ServiceBusPublishTransportProvider(_hostConfiguration.ConnectionContextSupervisor, this);
    }
}
