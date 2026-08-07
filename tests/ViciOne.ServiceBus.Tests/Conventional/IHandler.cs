// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests.Conventional
{
    public interface IHandler<in T>
    {
        void Handle(T message);
    }
}
