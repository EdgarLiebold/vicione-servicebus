using System;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Provides Azure Service Bus queue declaration and sender settings.</summary>
public class QueueSendSettings :
    SendSettings,
    IServiceBusEntityConfigurator
{
    readonly CreateQueueOptions _createQueueOptions;

    /// <summary>Creates sender settings from Azure queue declaration options.</summary>
    /// <param name="createQueueOptions">The queue declaration options.</param>
    public QueueSendSettings(CreateQueueOptions createQueueOptions)
    {
        _createQueueOptions = createQueueOptions;
    }

    /// <summary>Sets the idle interval after which the queue is deleted.</summary>
    public TimeSpan? AutoDeleteOnIdle
    {
        set
        {
            if (value.HasValue)
                _createQueueOptions.AutoDeleteOnIdle = value.Value == TimeSpan.Zero
                    ? TimeSpan.MaxValue
                    : value.Value;
        }
    }

    /// <summary>Sets the default time to live for messages sent to the queue.</summary>
    public TimeSpan? DefaultMessageTimeToLive
    {
        set
        {
            if (value.HasValue)
                _createQueueOptions.DefaultMessageTimeToLive = value.Value;
        }
    }

    /// <summary>Enables or disables broker-side batching.</summary>
    public bool? EnableBatchedOperations
    {
        set
        {
            if (value.HasValue)
                _createQueueOptions.EnableBatchedOperations = value.Value;
        }
    }

    /// <summary>Sets provider metadata stored with the queue.</summary>
    public string UserMetadata
    {
        set => _createQueueOptions.UserMetadata = value;
    }

    /// <summary>Gets the namespace-relative queue path.</summary>
    public string EntityPath => _createQueueOptions.Name;

    /// <summary>Builds broker topology containing the destination queue.</summary>
    /// <returns>The queue declaration topology.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        var builder = new SendEndpointBrokerTopologyBuilder();

        builder.Queue = builder.CreateQueue(_createQueueOptions);

        return builder.BuildBrokerTopology();
    }
}
