namespace ViciOne.ServiceBus.Initializers.Conventions;

/// <summary>Creates convention type cache instances.</summary>
/// <typeparam name="TValue">The value stored by the member.</typeparam>
public interface IConventionTypeCacheFactory<out TValue>
    where TValue : class
{
    /// <summary>Creates the requested value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="convention">The convention.</param>
    /// <returns>The newly created instance.</returns>
    TValue Create<T>(IInitializerConvention convention)
        where T : class;
}
