using System;
using ViciOne.ServiceBus.Initializers.Factories;
using ViciOne.ServiceBus.Internals.Reflection;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>Creates and caches the factory for one message contract.</summary>
internal static class MessageFactoryCache<TMessage>
    where TMessage : class
{
    static readonly Lazy<IMessageFactory<TMessage>> _factory = new(CreateMessageFactory);

    internal static IMessageFactory<TMessage> Factory => _factory.Value;


    static IMessageFactory<TMessage> CreateMessageFactory()
    {
        if (!MessageTypeCache<TMessage>.IsValidMessageType)
            throw new ArgumentException(MessageTypeCache<TMessage>.InvalidMessageTypeReason, nameof(TMessage));

        Type implementationType = typeof(TMessage).IsInterface
            ? MessageImplementationCache<TMessage>.ImplementationType
            : typeof(TMessage);

        if (implementationType.GetConstructor(Type.EmptyTypes) == null)
            throw new ArgumentException($"The message implementation '{implementationType}' must have a public parameterless constructor.", nameof(TMessage));

        Type factoryType = typeof(DynamicMessageFactory<,>).MakeGenericType(typeof(TMessage), implementationType);

        return (IMessageFactory<TMessage>)(Activator.CreateInstance(factoryType)
            ?? throw new InvalidOperationException($"The message factory '{factoryType}' could not be activated."));
    }
}
