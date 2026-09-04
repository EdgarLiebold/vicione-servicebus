using System;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubIntegration;

public class ProcessorContextSupervisor :
    TransportPipeContextSupervisor<ProcessorContext>,
    IProcessorContextSupervisor
{
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
