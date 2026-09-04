using System;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides a processor context supervisor implementation.
/// </summary>
public class ProcessorContextSupervisor :
    TransportPipeContextSupervisor<ProcessorContext>,
    IProcessorContextSupervisor
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="supervisor">The supervisor value.</param>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="clientFactory">The client factory value.</param>
    /// <param name="partitionClosingHandler">The partition closing handler value.</param>
    /// <param name="partitionInitializingHandler">The partition initializing handler value.</param>
    public ProcessorContextSupervisor(IConnectionContextSupervisor supervisor, IHostConfiguration hostConfiguration,
        Func<EventProcessorClient> clientFactory, Func<PartitionClosingEventArgs, Task>? partitionClosingHandler,
        Func<PartitionInitializingEventArgs, Task>? partitionInitializingHandler)
        : base(new ProcessorContextFactory(supervisor, hostConfiguration, clientFactory,
            partitionClosingHandler,
            partitionInitializingHandler))
    {
        supervisor.AddConsumeAgent(this);
    }
}
