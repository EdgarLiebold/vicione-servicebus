// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public interface IStateMachineInterfaceType
    {
        ISagaMessageConnector<T> GetConnector<T>()
            where T : class, ISaga;
    }
}
