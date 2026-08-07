// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SqlTransport
{
    using System.Threading.Tasks;


    public interface IQueueNotificationListener
    {
        Task MessageReady(string queueName);
    }
}
