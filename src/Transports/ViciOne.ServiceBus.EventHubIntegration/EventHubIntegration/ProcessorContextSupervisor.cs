// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EventHubIntegration
{
    using System;
    using System.Threading.Tasks;
    using Azure.Messaging.EventHubs;
    using Azure.Messaging.EventHubs.Processor;
    using ViciOne.ServiceBus.Configuration;
    using Transports;


    public class ProcessorContextSupervisor :
        TransportPipeContextSupervisor<ProcessorContext>,
        IProcessorContextSupervisor
    {
        public ProcessorContextSupervisor(IConnectionContextSupervisor supervisor, IHostConfiguration hostConfiguration,
            Func<EventProcessorClient> clientFactory, Func<PartitionClosingEventArgs, Task> partitionClosingHandler,
            Func<PartitionInitializingEventArgs, Task> partitionInitializingHandler)
            : base(new ProcessorContextFactory(supervisor, hostConfiguration, clientFactory,
                partitionClosingHandler,
                partitionInitializingHandler))
        {
            supervisor.AddConsumeAgent(this);
        }
    }
}
