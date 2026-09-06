namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Provides scoped bus context services.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
public interface IScopedBusContextProvider<TBus>
    where TBus : class, IBus
{
    /// <summary>Gets the context.</summary>
    ScopedBusContext Context { get; }
}
