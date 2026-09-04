using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a transform specification configurator implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class TransformSpecificationConfigurator<TMessage> :
    ITransformSpecificationConfigurator<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Performs the get operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public IConsumeTransformSpecification<TMessage> Get<T>()
        where T : IConsumeTransformSpecification<TMessage>, new()
    {
        return new T();
    }

    /// <summary>
    /// Performs the get operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="transformFactory">The transform factory value.</param>
    /// <returns>The result of the operation.</returns>
    public IConsumeTransformSpecification<TMessage> Get<T>(Func<T> transformFactory)
        where T : IConsumeTransformSpecification<TMessage>
    {
        return transformFactory();
    }
}
