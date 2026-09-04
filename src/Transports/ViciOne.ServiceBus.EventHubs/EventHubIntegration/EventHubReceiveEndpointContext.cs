using System;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.EventHubs.Configuration;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides an event hub receive endpoint context implementation.
/// </summary>
public class EventHubReceiveEndpointContext :
    BaseReceiveEndpointContext,
    IEventHubReceiveEndpointContext
{
    readonly IBusInstance _busInstance;
    readonly Recycle<IProcessorContextSupervisor> _contextSupervisor;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="busInstance">The bus instance value.</param>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
    /// <param name="clientFactory">The client factory value.</param>
    /// <param name="partitionClosingHandler">The partition closing handler value.</param>
    /// <param name="partitionInitializingHandler">The partition initializing handler value.</param>
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

    /// <summary>
    /// Adds send agent to the configuration.
    /// </summary>
    /// <param name="agent">The agent value.</param>
    public override void AddSendAgent(IAgent agent)
    {
        _contextSupervisor.Supervisor.AddSendAgent(agent);
    }

    /// <summary>
    /// Adds consume agent to the configuration.
    /// </summary>
    /// <param name="agent">The agent value.</param>
    public override void AddConsumeAgent(IAgent agent)
    {
        _contextSupervisor.Supervisor.AddConsumeAgent(agent);
    }

    /// <summary>
    /// Performs the convert exception operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public override Exception ConvertException(Exception exception, string message)
    {
        return new EventHubConnectionException(message, exception);
    }

    /// <summary>
    /// Gets the context supervisor value.
    /// </summary>
    public IProcessorContextSupervisor ContextSupervisor => _contextSupervisor.Supervisor;

    /// <summary>
    /// Creates send transport provider.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override ISendTransportProvider CreateSendTransportProvider()
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Creates publish transport provider.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override IPublishTransportProvider CreatePublishTransportProvider()
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Creates publish endpoint provider.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override IPublishEndpointProvider CreatePublishEndpointProvider()
    {
        return _busInstance.Bus;
    }

    /// <summary>
    /// Creates send endpoint provider.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override ISendEndpointProvider CreateSendEndpointProvider()
    {
        return _busInstance.Bus;
    }

    /// <summary>
    /// Performs the release send endpoint provider operation.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <returns>The result of the operation.</returns>
    protected override ValueTask ReleaseSendEndpointProviderAsync(ISendEndpointProvider provider)
    {
        return default;
    }

    /// <summary>
    /// Performs the release publish endpoint provider operation.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <returns>The result of the operation.</returns>
    protected override ValueTask ReleasePublishEndpointProviderAsync(IPublishEndpointProvider provider)
    {
        return default;
    }
}
