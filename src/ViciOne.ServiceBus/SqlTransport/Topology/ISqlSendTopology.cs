using ViciOne.ServiceBus.SqlTransport;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines the contract for sql send topology.
/// </summary>
public interface ISqlSendTopology :
    ISendTopology
{
    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new ISqlMessageSendTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Return the send settings for the specified <paramref name="address" />
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    SendSettings GetSendSettings(SqlEndpointAddress address);

    /// <summary>
    /// Return the error settings for the queue
    /// </summary>
    /// <param name="settings"></param>
    /// <returns></returns>
    SendSettings GetErrorSettings(ReceiveSettings settings);

    /// <summary>
    /// Return the dead letter settings for the queue
    /// </summary>
    /// <param name="settings"></param>
    /// <returns></returns>
    SendSettings GetDeadLetterSettings(ReceiveSettings settings);
}
