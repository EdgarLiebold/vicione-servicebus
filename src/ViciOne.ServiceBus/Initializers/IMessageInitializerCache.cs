using System;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>
/// Defines the contract for message initializer cache.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IMessageInitializerCache<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Gets initializer.
    /// </summary>
    /// <param name="objectType">The object type value.</param>
    /// <returns>The result of the operation.</returns>
    IMessageInitializer<TMessage> GetInitializer(Type objectType);
}
