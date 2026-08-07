// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Testing
{
    public interface ISagaInstance<out T> :
        IAsyncListElement
        where T : class, ISaga
    {
        T Saga { get; }
    }
}
