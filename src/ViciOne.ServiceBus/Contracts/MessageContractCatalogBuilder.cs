using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Bootstrap-only builder for the immutable message contract catalog.
/// </summary>
public sealed class MessageContractCatalogBuilder
{
    private readonly Dictionary<Type, MessageContractIdentity> _byType = new();
    private readonly Dictionary<MessageContractIdentity, Type> _byIdentity = new();
    private bool _built;

    /// <summary>
    /// Performs the register operation.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="name">The name value.</param>
    /// <param name="majorVersion">The major version value.</param>
    /// <returns>The result of the operation.</returns>
    public MessageContractCatalogBuilder Register<TMessage>(string name, int majorVersion = 1)
        where TMessage : class
        => Register(typeof(TMessage), new MessageContractIdentity(name, majorVersion));

    /// <summary>
    /// Performs the register operation.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public MessageContractCatalogBuilder Register<TMessage>()
        where TMessage : class
        => Register(typeof(TMessage));

    /// <summary>
    /// Performs the register operation.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <returns>The result of the operation.</returns>
    public MessageContractCatalogBuilder Register(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        ThrowIfBuilt();

        MessageContractAttribute? attribute = messageType
            .GetCustomAttributes(typeof(MessageContractAttribute), inherit: false)
            .OfType<MessageContractAttribute>()
            .SingleOrDefault();

        if (attribute is null)
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Message Contract Catalog", "unknown", $"Message type '{messageType}' has no {nameof(MessageContractAttribute)}. Durable contracts require an explicit stable identity.", "Correct the named configuration before starting the host"));
        }

        return Register(messageType, new MessageContractIdentity(attribute.Name, attribute.MajorVersion));
    }

    /// <summary>
    /// Performs the register operation.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="identity">The identity value.</param>
    /// <returns>The result of the operation.</returns>
    public MessageContractCatalogBuilder Register(Type messageType, MessageContractIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        ThrowIfBuilt();

        if (!messageType.IsClass && !messageType.IsInterface)
        {
            throw new ArgumentException(
                $"'{messageType}' cannot be used as a message contract type. ServiceBus message contracts must be reference types (classes or interfaces).",
                nameof(messageType));
        }

        if (messageType.ContainsGenericParameters)
        {
            throw new ArgumentException(
                $"'{messageType}' is an open generic type. Durable message contracts must be closed runtime types.",
                nameof(messageType));
        }

        if (string.IsNullOrWhiteSpace(identity.Name) || identity.MajorVersion is < 1 or > ushort.MaxValue)
            throw new ArgumentException("A message contract catalog registration requires a valid stable contract identity.", nameof(identity));

        if (_byType.TryGetValue(messageType, out MessageContractIdentity existingIdentity))
        {
            if (existingIdentity == identity)
                return this;

            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Message Contract Catalog", "unknown", $"Message type '{messageType}' is already registered as '{existingIdentity}' and cannot also be '{identity}'.", "Correct the named configuration before starting the host"));
        }

        if (_byIdentity.TryGetValue(identity, out Type? existingType))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Message Contract Catalog", "unknown", $"Message contract identity '{identity}' is already registered for '{existingType}' and cannot also map to '{messageType}'.", "Correct the named configuration before starting the host"));
        }

        _byType.Add(messageType, identity);
        _byIdentity.Add(identity, messageType);
        return this;
    }

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IMessageContractCatalog Build()
    {
        ThrowIfBuilt();
        _built = true;

        return new MessageContractCatalog(_byType.ToFrozenDictionary(), _byIdentity.ToFrozenDictionary());
    }

    private void ThrowIfBuilt()
    {
        if (_built)
            throw new InvalidOperationException("The message contract catalog has already been materialized and is immutable.");
    }

    private sealed class MessageContractCatalog(
        FrozenDictionary<Type, MessageContractIdentity> byType,
        FrozenDictionary<MessageContractIdentity, Type> byIdentity) : IMessageContractCatalog
    {
        public MessageContractIdentity GetIdentity(Type messageType)
        {
            ArgumentNullException.ThrowIfNull(messageType);
            return byType.TryGetValue(messageType, out MessageContractIdentity identity)
                ? identity
                : throw new MessageContractException(
                    $"Message type '{messageType}' has no stable contract identity. Register it during ServiceBus bootstrap before using it in durable infrastructure.");
        }

        public bool TryGetIdentity(Type messageType, out MessageContractIdentity identity)
        {
            ArgumentNullException.ThrowIfNull(messageType);
            return byType.TryGetValue(messageType, out identity);
        }

        public Type GetMessageType(MessageContractIdentity identity)
            => byIdentity.TryGetValue(identity, out Type? messageType)
                ? messageType
                : throw new MessageContractException(
                    $"Message contract identity '{identity}' is not registered in this ServiceBus runtime.");

        public bool TryGetMessageType(MessageContractIdentity identity, [NotNullWhen(true)] out Type? messageType)
            => byIdentity.TryGetValue(identity, out messageType);
    }
}
