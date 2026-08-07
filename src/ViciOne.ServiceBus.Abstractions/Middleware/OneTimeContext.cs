// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus;

public interface OneTimeContext<TPayload>
    where TPayload : class
{
    void Evict();
}
