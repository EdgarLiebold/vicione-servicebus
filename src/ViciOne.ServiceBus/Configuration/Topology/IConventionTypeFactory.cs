namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates a convention for a requested message contract.</summary>
/// <typeparam name="TValue">The common convention contract.</typeparam>
public interface IConventionTypeFactory<out TValue>
    where TValue : class
{
    /// <summary>Creates a convention for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <returns>The created convention.</returns>
    TValue Create<T>()
        where T : class;
}
