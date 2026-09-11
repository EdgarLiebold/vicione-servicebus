using System;
using ViciOne.ServiceBus.Initializers.Factories;
using ViciOne.ServiceBus.Internals.Reflection;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>Caches message factory data.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public static class MessageFactoryCache<TMessage>
    where TMessage : class
{
    /// <summary>Gets the factory.</summary>
    public static IMessageFactory<TMessage> Factory => Cached.MessageFactory.Value;


    static class Cached
    {
        internal static readonly Lazy<IMessageFactory<TMessage>> MessageFactory = new Lazy<IMessageFactory<TMessage>>(CreateMessageFactory);

        static IMessageFactory<TMessage> CreateMessageFactory()
        {
            if (!MessageTypeCache<TMessage>.IsValidMessageType)
                throw new ArgumentException(MessageTypeCache<TMessage>.InvalidMessageTypeReason, nameof(TMessage));

            var implementationType = typeof(TMessage);
            if (typeof(TMessage).IsInterface)
                implementationType = MessageImplementationCache<TMessage>.ImplementationType;

            Type[] parameterTypes = Type.EmptyTypes;
            if (implementationType.GetConstructor(parameterTypes) == null)
                throw new ArgumentException("No default constructor available for message type", nameof(TMessage));

            return (IMessageFactory<TMessage>)(Activator.CreateInstance(typeof(DynamicMessageFactory<,>).MakeGenericType(typeof(TMessage),
                implementationType)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));
        }
    }
}
