namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Builds bus instance components.</summary>
public interface IBusInstanceBuilder
{
    /// <summary>Gets bus instance type.</summary>
    /// <typeparam name="TBus">The bus type.</typeparam>
    /// <typeparam name="TResult">The result produced by the operation.</typeparam>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The bus instance type.</returns>
    TResult GetBusInstanceType<TBus, TResult>(IBusInstanceBuilderCallback<TBus, TResult> callback)
        where TBus : class, IBus;
}
