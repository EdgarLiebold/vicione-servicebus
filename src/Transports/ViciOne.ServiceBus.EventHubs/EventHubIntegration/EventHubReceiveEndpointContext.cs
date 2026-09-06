using System;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.EventHubs.Configuration;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Owns receive-pipeline state, processor supervision, and bus endpoint providers for an Event Hubs endpoint.</summary>
public class EventHubReceiveEndpointContext :
    BaseReceiveEndpointContext,
    IEventHubReceiveEndpointContext
{
    readonly IBusInstance _busInstance;
    readonly Recycle<IProcessorContextSupervisor> _contextSupervisor;

    /// <summary>Creates a receive context with a recyclable processor supervisor.</summary>
    /// <param name="hostConfiguration">The Event Hubs rider host configuration.</param>
    /// <param name="busInstance">The bus instance that owns the endpoint.</param>
    /// <param name="endpointConfiguration">The endpoint's receive-pipeline configuration.</param>
    /// <param name="clientFactory">Creates the Azure SDK event processor client.</param>
    /// <param name="partitionClosingHandler">The optional application partition-closing handler.</param>
    /// <param name="partitionInitializingHandler">The optional application partition-initializing handler.</param>
    public EventHubReceiveEndpointContext(IEventHubHostConfiguration hostConfiguration, IBusInstance busInstance,
        IReceiveEndpointConfiguration endpointConfiguration,
        Func<EventProcessorClient> clientFactory,
        Func<PartitionClosingEventArgs, Task>? partitionClosingHandler,
        Func<PartitionInitializingEventArgs, Task>? partitionInitializingHandler)
        : base(busInstance.HostConfiguration, endpointConfiguration)
    {
        _busInstance = busInstance;
        _contextSupervisor = new Recycle<IProcessorContextSupervisor>(() =>
            new ProcessorContextSupervisor(hostConfiguration.ConnectionContextSupervisor, busInstance.HostConfiguration, clientFactory,
                partitionClosingHandler, partitionInitializingHandler));
    }

    /// <summary>Adds a send agent to the processor supervisor's lifetime.</summary>
    /// <param name="agent">The send agent to supervise.</param>
    public override void AddSendAgent(IAgent agent)
    {
        _contextSupervisor.Supervisor.AddSendAgent(agent);
    }

    /// <summary>Adds a consume agent to the processor supervisor's lifetime.</summary>
    /// <param name="agent">The consume agent to supervise.</param>
    public override void AddConsumeAgent(IAgent agent)
    {
        _contextSupervisor.Supervisor.AddConsumeAgent(agent);
    }

    /// <summary>Wraps a transport failure as an Event Hubs connection exception.</summary>
    /// <param name="exception">The underlying transport failure.</param>
    /// <param name="message">The contextual error message.</param>
    /// <returns>The Event Hubs connection exception.</returns>
    public override Exception ConvertException(Exception exception, string message)
    {
        return new EventHubConnectionException(message, exception);
    }

    /// <summary>Gets the recyclable supervisor that owns the processor context.</summary>
    public IProcessorContextSupervisor ContextSupervisor => _contextSupervisor.Supervisor;

    /// <summary>Rejects receive-endpoint-local send transport creation, which Event Hubs does not support.</summary>
    /// <returns>This method does not return.</returns>
    protected override ISendTransportProvider CreateSendTransportProvider()
    {
        throw new NotSupportedException();
    }

    /// <summary>Rejects receive-endpoint-local publish transport creation, which Event Hubs does not support.</summary>
    /// <returns>This method does not return.</returns>
    protected override IPublishTransportProvider CreatePublishTransportProvider()
    {
        throw new NotSupportedException();
    }

    /// <summary>Uses the owning bus as the publish endpoint provider.</summary>
    /// <returns>The bus instance.</returns>
    protected override IPublishEndpointProvider CreatePublishEndpointProvider()
    {
        return _busInstance.Bus;
    }

    /// <summary>Uses the owning bus as the send endpoint provider.</summary>
    /// <returns>The bus instance.</returns>
    protected override ISendEndpointProvider CreateSendEndpointProvider()
    {
        return _busInstance.Bus;
    }

    /// <summary>Completes immediately because this context does not own the bus send endpoint provider.</summary>
    /// <param name="provider">The bus-owned provider returned by <see cref="CreateSendEndpointProvider" />.</param>
    /// <returns>A completed value task.</returns>
    protected override ValueTask ReleaseSendEndpointProviderAsync(ISendEndpointProvider provider)
    {
        return default;
    }

    /// <summary>Completes immediately because this context does not own the bus publish endpoint provider.</summary>
    /// <param name="provider">The bus-owned provider returned by <see cref="CreatePublishEndpointProvider" />.</param>
    /// <returns>A completed value task.</returns>
    protected override ValueTask ReleasePublishEndpointProviderAsync(IPublishEndpointProvider provider)
    {
        return default;
    }
}
