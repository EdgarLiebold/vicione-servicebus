using System;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus;

/// <summary>
/// Immutable runtime catalog that maps CLR message types to stable application contract identities and back.
/// </summary>
public interface IMessageContractCatalog
{
    MessageContractIdentity GetIdentity(Type messageType);

    bool TryGetIdentity(Type messageType, out MessageContractIdentity identity);

    Type GetMessageType(MessageContractIdentity identity);

    bool TryGetMessageType(MessageContractIdentity identity, [NotNullWhen(true)] out Type? messageType);
}
