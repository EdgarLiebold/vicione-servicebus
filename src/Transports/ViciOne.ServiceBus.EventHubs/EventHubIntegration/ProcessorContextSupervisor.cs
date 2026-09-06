using System;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Supervises an Event Hubs processor context and registers it as a dependent receive agent of the shared connection.</summary>
public class ProcessorContextSupervisor :
    TransportPipeContextSupervisor<ProcessorContext>,
    IProcessorContextSupervisor
{
    /// <summary>Creates a processor supervisor for one receive endpoint.</summary>
    /// <param name="supervisor">The shared Event Hubs connection supervisor.</param>
    /// <param name="hostConfiguration">The bus host configuration used for logging.</param>
    /// <param name="clientFactory">Creates the Azure SDK event processor client.</param>
    /// <param name="partitionClosingHandler">The optional application partition-closing handler.</param>
    /// <param name="partitionInitializingHandler">The optional application partition-initializing handler.</param>
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
