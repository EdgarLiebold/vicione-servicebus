using ViciOne.ServiceBus.AmazonSqs;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Defines Amazon SQS queue topology for send, error, and skipped-message destinations.</summary>
public interface IAmazonSqsSendTopology :
    ISendTopology
{
    /// <summary>Gets send topology for a message type.</summary>
    /// <typeparam name="T">The sent message type.</typeparam>
    /// <returns>The typed Amazon SQS message send-topology configurator.</returns>
    new IAmazonSqsMessageSendTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Creates queue send settings from an Amazon SQS endpoint address.</summary>
    /// <param name="address">The destination queue address.</param>
    /// <returns>The queue send settings.</returns>
    SendSettings GetSendSettings(AmazonSqsEndpointAddress address);

    /// <summary>Creates error-queue settings from receive settings.</summary>
    /// <param name="settings">The source receive settings.</param>
    /// <returns>The generated error-queue settings.</returns>
    ErrorSettings GetErrorSettings(ReceiveSettings settings);

    /// <summary>Creates skipped-message queue settings from receive settings.</summary>
    /// <param name="settings">The source receive settings.</param>
    /// <returns>The generated skipped-message queue settings.</returns>
    DeadLetterSettings GetDeadLetterSettings(ReceiveSettings settings);
}
