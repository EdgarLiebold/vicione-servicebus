namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Defines the contract for bus instance builder callback.
/// </summary>
/// <typeparam name="TBus">The t bus type.</typeparam>
/// <typeparam name="TResult">The t result type.</typeparam>
public interface IBusInstanceBuilderCallback<TBus, out TResult>
    where TBus : class, IBus
{
    /// <summary>
    /// Gets result.
    /// </summary>
    /// <typeparam name="TBusInstance">The t bus instance type.</typeparam>
    /// <returns>The result of the operation.</returns>
    TResult GetResult<TBusInstance>()
        where TBusInstance : BusInstance<TBus>, TBus;
}
