namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Defines the operations required by bus instance builder callback.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
public interface IBusInstanceBuilderCallback<TBus, out TResult>
    where TBus : class, IBus
{
    /// <summary>Gets result.</summary>
    /// <typeparam name="TBusInstance">The bus instance type.</typeparam>
    /// <returns>The result.</returns>
    TResult GetResult<TBusInstance>()
        where TBusInstance : BusInstance<TBus>, TBus;
}
