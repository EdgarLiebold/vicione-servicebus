namespace ViciOne.ServiceBus.SqlTransport
{
    using System.Threading.Tasks;


    public interface IQueueNotificationListener
    {
        Task MessageReady(string queueName);
    }
}
