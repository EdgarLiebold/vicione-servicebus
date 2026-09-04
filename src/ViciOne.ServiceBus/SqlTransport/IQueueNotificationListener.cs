using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SqlTransport;

public interface IQueueNotificationListener
{
    Task MessageReady(string queueName);
}
