using System;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// Provides a queue send settings implementation.
/// </summary>
public class QueueSendSettings :
    SendSettings,
    IServiceBusEntityConfigurator
{
    readonly CreateQueueOptions _createQueueOptions;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="createQueueOptions">The create queue options value.</param>
    public QueueSendSettings(CreateQueueOptions createQueueOptions)
    {
        _createQueueOptions = createQueueOptions;
    }

    /// <summary>
    /// Gets or sets the auto delete on idle value.
    /// </summary>
    public TimeSpan? AutoDeleteOnIdle
    {
        set
        {
            if (value.HasValue)
                _createQueueOptions.AutoDeleteOnIdle = value.Value;
        }
    }

    /// <summary>
    /// Gets or sets the default message time to live value.
    /// </summary>
    public TimeSpan? DefaultMessageTimeToLive
    {
        set
        {
            if (value.HasValue)
                _createQueueOptions.DefaultMessageTimeToLive = value.Value;
        }
    }

    /// <summary>
    /// Gets or sets the enable batched operations value.
    /// </summary>
    public bool? EnableBatchedOperations
    {
        set
        {
            if (value.HasValue)
                _createQueueOptions.EnableBatchedOperations = value.Value;
        }
    }

    /// <summary>
    /// Gets or sets the user metadata value.
    /// </summary>
    public string UserMetadata
    {
        set => _createQueueOptions.UserMetadata = value;
    }

    /// <summary>
    /// Gets the entity path value.
    /// </summary>
    public string EntityPath => _createQueueOptions.Name;

    /// <summary>
    /// Gets broker topology.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        var builder = new SendEndpointBrokerTopologyBuilder();

        builder.Queue = builder.CreateQueue(_createQueueOptions);

        return builder.BuildBrokerTopology();
    }
}
