using System;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus;

/// <summary>Immutable runtime catalog that maps CLR message types to stable application contract identities and back.</summary>
public interface IMessageContractCatalog
{
    /// <summary>Gets the stable contract identity registered for a runtime message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns>The registered stable identity.</returns>
    MessageContractIdentity GetIdentity(Type messageType);

    /// <summary>Attempts to get the stable contract identity registered for a runtime message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="identity">Receives the registered identity when the type is known.</param>
    /// <returns><see langword="true" /> when the type is registered; otherwise, <see langword="false" />.</returns>
    bool TryGetIdentity(Type messageType, out MessageContractIdentity identity);

    /// <summary>Gets the runtime message type registered for a stable contract identity.</summary>
    /// <param name="identity">The stable contract identity to resolve.</param>
    /// <returns>The registered runtime message type.</returns>
    Type GetMessageType(MessageContractIdentity identity);

    /// <summary>Attempts to get the runtime message type registered for a stable contract identity.</summary>
    /// <param name="identity">The stable contract identity to resolve.</param>
    /// <param name="messageType">Receives the registered runtime message type when the identity is known.</param>
    /// <returns><see langword="true" /> when the identity is registered; otherwise, <see langword="false" />.</returns>
    bool TryGetMessageType(MessageContractIdentity identity, [NotNullWhen(true)] out Type? messageType);
}
