namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates convention type instances.</summary>
/// <typeparam name="TValue">The value stored by the member.</typeparam>
public interface IConventionTypeFactory<out TValue>
    where TValue : class
{
    /// <summary>Creates the requested value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The newly created instance.</returns>
    TValue Create<T>()
        where T : class;
}
