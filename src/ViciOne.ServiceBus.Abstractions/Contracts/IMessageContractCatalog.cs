using System;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus;

/// <summary>
/// Immutable runtime catalog that maps CLR message types to stable application contract identities and back.
/// </summary>
public interface IMessageContractCatalog
{
    /// <summary>
    /// Gets identity.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <returns>The result of the operation.</returns>
    MessageContractIdentity GetIdentity(Type messageType);

    /// <summary>
    /// Attempts to get identity.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="identity">The identity value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetIdentity(Type messageType, out MessageContractIdentity identity);

    /// <summary>
    /// Gets message type.
    /// </summary>
    /// <param name="identity">The identity value.</param>
    /// <returns>The result of the operation.</returns>
    Type GetMessageType(MessageContractIdentity identity);

    /// <summary>
    /// Attempts to get message type.
    /// </summary>
    /// <param name="identity">The identity value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetMessageType(MessageContractIdentity identity, [NotNullWhen(true)] out Type? messageType);
}
