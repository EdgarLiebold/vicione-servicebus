// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public interface IMessageSendTopologyConvention<TMessage> :
        IMessageSendTopologyConvention
        where TMessage : class
    {
        bool TryGetMessageSendTopology(out IMessageSendTopology<TMessage> messageSendTopology);
    }


    public interface IMessageSendTopologyConvention
    {
        bool TryGetMessageSendTopologyConvention<T>(out IMessageSendTopologyConvention<T> convention)
            where T : class;
    }
}
