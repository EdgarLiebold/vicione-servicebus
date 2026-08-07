// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public interface ISagaQueryPropertySelector<in TData, TProperty>
        where TData : class
    {
        bool TryGetProperty(ConsumeContext<TData> context, out TProperty property);
    }
}
