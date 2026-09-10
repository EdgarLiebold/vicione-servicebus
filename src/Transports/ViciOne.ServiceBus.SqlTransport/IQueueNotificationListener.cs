using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Defines the operations required by queue notification listener.</summary>
public interface IQueueNotificationListener
{
    /// <summary>Reports that the message is ready for delivery.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task MessageReadyAsync(string queueName, CancellationToken cancellationToken = default);
}
