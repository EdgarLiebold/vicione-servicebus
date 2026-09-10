using ViciOne.ServiceBus.SqlTransport;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Defines the operations required by sql send topology.</summary>
public interface ISqlSendTopology :
    ISendTopology
{
    /// <summary>Gets message topology.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message topology.</returns>
    new ISqlMessageSendTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Return the send settings for the specified <paramref name="address" />.</summary>
    /// <param name="address">The address.</param>
    /// <returns>The send settings.</returns>
    SendSettings GetSendSettings(SqlEndpointAddress address);

    /// <summary>Return the error settings for the queue.</summary>
    /// <param name="settings">The settings that control the operation.</param>
    /// <returns>The error settings.</returns>
    SendSettings GetErrorSettings(ReceiveSettings settings);

    /// <summary>Return the dead letter settings for the queue.</summary>
    /// <param name="settings">The settings that control the operation.</param>
    /// <returns>The dead letter settings.</returns>
    SendSettings GetDeadLetterSettings(ReceiveSettings settings);
}
