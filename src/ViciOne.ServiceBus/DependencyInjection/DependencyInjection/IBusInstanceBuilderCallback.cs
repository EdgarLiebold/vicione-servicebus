// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.DependencyInjection
{
    public interface IBusInstanceBuilderCallback<TBus, out TResult>
        where TBus : class, IBus
    {
        TResult GetResult<TBusInstance>()
            where TBusInstance : BusInstance<TBus>, TBus;
    }
}
