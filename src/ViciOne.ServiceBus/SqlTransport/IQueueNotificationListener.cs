using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Defines the contract for queue notification listener.
/// </summary>
public interface IQueueNotificationListener
{
    /// <summary>
    /// Performs the message ready operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task MessageReadyAsync(string queueName, CancellationToken cancellationToken = default);
}
