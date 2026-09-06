using System;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Configures an Event Hubs processor-backed receive endpoint.</summary>
public interface IEventHubReceiveEndpointConfigurator :
    IReceiveEndpointConfigurator
{
    /// <summary>Sets the Blob Storage container name used for partition ownership and checkpoints.</summary>
    string ContainerName { set; }

    /// <summary>Sets the maximum time between completed partition checkpoints. The default is one minute.</summary>
    TimeSpan CheckpointInterval { set; }

    /// <summary>Sets how many completed events trigger a partition checkpoint. The default is 5,000.</summary>
    ushort CheckpointMessageCount { set; }

    /// <summary>Sets the maximum number of uncheckpointed events retained for one partition. The default is 10,000.</summary>
    ushort CheckpointMessageLimit { set; }

    /// <summary>Sets the maximum concurrent deliveries for events with the same partition key. Values above one permit reordering. The default is one.</summary>
    int ConcurrentDeliveryLimit { set; }

    /// <summary>Sets the callback applied when the processor client options are created.</summary>
    Action<EventProcessorClientOptions> ConfigureOptions { set; }

    /// <summary>Registers a handler invoked after processing has stopped for a partition.</summary>
    /// <param name="handler">The asynchronous partition-closing handler.</param>
    void OnPartitionClosing(Func<PartitionClosingEventArgs, Task> handler);

    /// <summary>Registers a handler invoked before processing begins for a partition.</summary>
    /// <param name="handler">The asynchronous partition-initializing handler.</param>
    void OnPartitionInitializing(Func<PartitionInitializingEventArgs, Task> handler);
}
