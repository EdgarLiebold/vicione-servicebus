using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by message interface type.</summary>
public interface IMessageInterfaceType
{
    /// <summary>Gets the message type.</summary>
    Type MessageType { get; }

    /// <summary>Gets consumer connector.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The consumer connector.</returns>
    IConsumerMessageConnector<T> GetConsumerConnector<T>()
        where T : class;

    /// <summary>Gets instance connector.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The instance connector.</returns>
    IInstanceMessageConnector<T> GetInstanceConnector<T>()
        where T : class;
}
