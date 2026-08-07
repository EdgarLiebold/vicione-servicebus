// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface EventHubSendContext :
        SendContext,
        PartitionKeySendContext
    {
        string PartitionId { get; set; }
    }


    public interface EventHubSendContext<out T> :
        SendContext<T>,
        EventHubSendContext
        where T : class
    {
    }
}
