using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>A default convention that looks for IConsumerOfT message types.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class AsyncConsumerMessageConvention<T> :
    IConsumerMessageConvention
    where T : class
{
    /// <summary>Gets message types.</summary>
    /// <returns>The message types.</returns>
    public IEnumerable<IMessageInterfaceType> GetMessageTypes()
    {
        var consumerType = typeof(T);
        if (consumerType.IsGenericType && consumerType.GetGenericTypeDefinition() == typeof(IConsumer<>))
        {
            var interfaceType = new ConsumerInterfaceType(consumerType.GetGenericArguments()[0], consumerType);
            if (MessageTypeCache.IsValidMessageType(interfaceType.MessageType))
                yield return interfaceType;
        }

        IEnumerable<IMessageInterfaceType> types = consumerType.GetInterfaces()
            .Where(x => x.IsGenericType)
            .Where(x => x.GetGenericTypeDefinition() == typeof(IConsumer<>))
            .Select(x => new ConsumerInterfaceType(x.GetGenericArguments()[0], consumerType))
            .Where(x => MessageTypeCache.IsValidMessageType(x.MessageType));

        foreach (var type in types)
            yield return type;
    }
}
