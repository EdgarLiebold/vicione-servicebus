using ViciOne.ServiceBus.AmazonSqs;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for amazon sqs send topology.
/// </summary>
public interface IAmazonSqsSendTopology :
    ISendTopology
{
    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IAmazonSqsMessageSendTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Gets send settings.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
    SendSettings GetSendSettings(AmazonSqsEndpointAddress address);

    /// <summary>
    /// Return the error settings for the queue
    /// </summary>
    /// <param name="settings"></param>
    /// <returns></returns>
    ErrorSettings GetErrorSettings(ReceiveSettings settings);

    /// <summary>
    /// Return the dead letter settings for the queue
    /// </summary>
    /// <param name="settings"></param>
    /// <returns></returns>
    DeadLetterSettings GetDeadLetterSettings(ReceiveSettings settings);
}
