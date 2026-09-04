namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for convention type factory.
/// </summary>
/// <typeparam name="TValue">The t value type.</typeparam>
public interface IConventionTypeFactory<out TValue>
    where TValue : class
{
    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    TValue Create<T>()
        where T : class;
}
