namespace ViciOne.ServiceBus.Initializers.Conventions;

/// <summary>
/// Defines the contract for convention type cache factory.
/// </summary>
/// <typeparam name="TValue">The t value type.</typeparam>
public interface IConventionTypeCacheFactory<out TValue>
    where TValue : class
{
    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="convention">The convention value.</param>
    /// <returns>The result of the operation.</returns>
    TValue Create<T>(IInitializerConvention convention)
        where T : class;
}
