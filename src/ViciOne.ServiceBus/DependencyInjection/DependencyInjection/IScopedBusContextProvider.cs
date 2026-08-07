// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.DependencyInjection
{
    public interface IScopedBusContextProvider<TBus>
        where TBus : class, IBus
    {
        ScopedBusContext Context { get; }
    }
}
