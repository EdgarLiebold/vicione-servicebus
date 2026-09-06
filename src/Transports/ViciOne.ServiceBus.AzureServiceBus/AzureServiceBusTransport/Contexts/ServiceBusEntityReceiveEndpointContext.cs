using System;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Owns the broker topology and recyclable client supervisor for an Azure Service Bus receive endpoint.</summary>
public sealed class ServiceBusEntityReceiveEndpointContext :
    BaseReceiveEndpointContext,
    ServiceBusReceiveEndpointContext
{
    readonly Recycle<IClientContextSupervisor> _clientContext;
    readonly IServiceBusHostConfiguration _hostConfiguration;

    /// <summary>Initializes an endpoint context and its lazily created client supervisor.</summary>
    /// <param name="hostConfiguration">The namespace host configuration.</param>
    /// <param name="configuration">The entity endpoint configuration.</param>
    /// <param name="brokerTopology">The topology deployed before receiving starts.</param>
    /// <param name="supervisorFactory">The factory used to create or recycle the client supervisor.</param>
    public ServiceBusEntityReceiveEndpointContext(IServiceBusHostConfiguration hostConfiguration, IServiceBusEntityEndpointConfiguration configuration,
        BrokerTopology brokerTopology, Func<IClientContextSupervisor> supervisorFactory)
        : base(hostConfiguration, configuration)
    {
        _hostConfiguration = hostConfiguration;

        BrokerTopology = brokerTopology;

        GetOrAddPayload(() => _hostConfiguration.Topology);

        _clientContext = new Recycle<IClientContextSupervisor>(supervisorFactory);
    }

    /// <summary>Gets the topology deployed for this receive endpoint.</summary>
    public BrokerTopology BrokerTopology { get; }

    /// <summary>Gets the recyclable processor-client supervisor.</summary>
    public IClientContextSupervisor ClientContextSupervisor => _clientContext.Supervisor;

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The probe section to populate.</param>
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

    /// <summary>Registers a send agent with the endpoint's client supervisor.</summary>
    /// <param name="agent">The send agent to supervise.</param>
    public override void AddSendAgent(IAgent agent)
    {
        _clientContext.Supervisor.AddSendAgent(agent);
    }

    /// <summary>Registers a consume agent with the endpoint's client supervisor.</summary>
    /// <param name="agent">The consume agent to supervise.</param>
    public override void AddConsumeAgent(IAgent agent)
    {
        _clientContext.Supervisor.AddConsumeAgent(agent);
    }

    /// <summary>Wraps a transport failure with the endpoint address.</summary>
    /// <param name="exception">The transport failure.</param>
    /// <param name="message">The failure description.</param>
    /// <returns>An Azure Service Bus connection exception.</returns>
    public override Exception ConvertException(Exception exception, string message)
    {
        return new ServiceBusConnectionException(message + InputAddress, exception);
    }

    /// <summary>Creates the provider that resolves queue and topic send transports.</summary>
    /// <returns>The Azure Service Bus send-transport provider.</returns>
    protected override ISendTransportProvider CreateSendTransportProvider()
    {
        return new ServiceBusSendTransportProvider(_hostConfiguration.ConnectionContextSupervisor, this);
    }

    /// <summary>Creates the provider that resolves publish transports from message topology.</summary>
    /// <returns>The Azure Service Bus publish-transport provider.</returns>
    protected override IPublishTransportProvider CreatePublishTransportProvider()
    {
        return new ServiceBusPublishTransportProvider(_hostConfiguration.ConnectionContextSupervisor, this);
    }
}
