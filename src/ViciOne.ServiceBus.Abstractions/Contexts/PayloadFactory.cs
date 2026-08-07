// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public delegate TPayload PayloadFactory<out TPayload>()
        where TPayload : class;
}
