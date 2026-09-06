namespace ViciOne.ServiceBus.Configuration;

/// <summary>A consumer convention is used to find message types inside a consumer class.</summary>
public interface IConsumerConvention
{
    /// <summary>Returns the message convention for the type of T.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The consumer message convention.</returns>
    IConsumerMessageConvention GetConsumerMessageConvention<T>()
        where T : class;
}
