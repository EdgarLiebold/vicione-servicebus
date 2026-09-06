using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures transform specification.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface ITransformSpecificationConfigurator<TMessage>
    where TMessage : class
{
    /// <summary>Get a transform specification using the default constructor.</summary>
    /// <typeparam name="T">The transform specification type.</typeparam>
    /// <returns>The requested value.</returns>
    IConsumeTransformSpecification<TMessage> Get<T>()
        where T : IConsumeTransformSpecification<TMessage>, new();

    /// <summary>Get a transform specification using the factory method.</summary>
    /// <typeparam name="T">The transform specification type.</typeparam>
    /// <param name="transformFactory">The transform specification factory method.</param>
    /// <returns>The requested value.</returns>
    IConsumeTransformSpecification<TMessage> Get<T>(Func<T> transformFactory)
        where T : IConsumeTransformSpecification<TMessage>;
}
