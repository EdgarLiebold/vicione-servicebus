using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SqlTransport;

public interface IQueueNotificationListener
{
    Task MessageReadyAsync(string queueName, CancellationToken cancellationToken = default);
}
