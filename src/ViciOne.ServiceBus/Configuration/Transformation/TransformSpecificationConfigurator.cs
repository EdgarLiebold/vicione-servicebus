using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures transform specification.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class TransformSpecificationConfigurator<TMessage> :
    ITransformSpecificationConfigurator<TMessage>
    where TMessage : class
{
    /// <summary>Retrieves the requested value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The requested value.</returns>
    public IConsumeTransformSpecification<TMessage> Get<T>()
        where T : IConsumeTransformSpecification<TMessage>, new()
    {
        return new T();
    }

    /// <summary>Retrieves the requested value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="transformFactory">The transform factory.</param>
    /// <returns>The requested value.</returns>
    public IConsumeTransformSpecification<TMessage> Get<T>(Func<T> transformFactory)
        where T : IConsumeTransformSpecification<TMessage>
    {
        return transformFactory();
    }
}
