using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for message interface type.
/// </summary>
public interface IMessageInterfaceType
{
    /// <summary>
    /// Gets the message type value.
    /// </summary>
    Type MessageType { get; }

    /// <summary>
    /// Gets consumer connector.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    IConsumerMessageConnector<T> GetConsumerConnector<T>()
        where T : class;

    /// <summary>
    /// Gets instance connector.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    IInstanceMessageConnector<T> GetInstanceConnector<T>()
        where T : class;
}
