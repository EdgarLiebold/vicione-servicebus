// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface IGroupKeyProvider<in TMessage, TKey>
        where TMessage : class
    {
        bool TryGetKey(ConsumeContext<TMessage> context, out TKey key);
    }
}
