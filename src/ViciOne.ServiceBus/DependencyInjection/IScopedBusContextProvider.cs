namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Defines the contract for scoped bus context provider.
/// </summary>
/// <typeparam name="TBus">The t bus type.</typeparam>
public interface IScopedBusContextProvider<TBus>
    where TBus : class, IBus
{
    /// <summary>
    /// Gets the context value.
    /// </summary>
    ScopedBusContext Context { get; }
}
