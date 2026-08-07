// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SqlTransport
{
    public interface NotificationContext :
        PipeContext
    {
        ConnectHandle ConnectNotificationSink(string queueName, IQueueNotificationListener listener);
    }
}
