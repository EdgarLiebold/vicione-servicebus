namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures a receive endpoint to process messages in independent partition-key streams.</summary>
public interface IPartitionedReceiveEndpointConfigurator :
    IReceiveEndpointConfigurator
{
    /// <summary>Enables partitioned receive processing for the endpoint.</summary>
    void SetPartitionedReceive();
}
