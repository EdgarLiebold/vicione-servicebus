namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Defines the contract for bus instance builder.
/// </summary>
public interface IBusInstanceBuilder
{
    /// <summary>
    /// Gets bus instance type.
    /// </summary>
    /// <typeparam name="TBus">The t bus type.</typeparam>
    /// <typeparam name="TResult">The t result type.</typeparam>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    TResult GetBusInstanceType<TBus, TResult>(IBusInstanceBuilderCallback<TBus, TResult> callback)
        where TBus : class, IBus;
}
