using System;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>Provides cached access to message initializer data.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessageInitializerCache<TMessage>
    where TMessage : class
{
    /// <summary>Gets initializer.</summary>
    /// <param name="objectType">The runtime object type used by the operation.</param>
    /// <returns>The initializer.</returns>
    IMessageInitializer<TMessage> GetInitializer(Type objectType);
}
