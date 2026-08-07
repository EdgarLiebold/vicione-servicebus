// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public interface IConventionTypeFactory<out TValue>
        where TValue : class
    {
        TValue Create<T>()
            where T : class;
    }
}
