namespace ViciOne.ServiceBus.DependencyInjection
{
    public interface IScopedBusContextProvider<TBus>
        where TBus : class, IBus
    {
        ScopedBusContext Context { get; }
    }
}
