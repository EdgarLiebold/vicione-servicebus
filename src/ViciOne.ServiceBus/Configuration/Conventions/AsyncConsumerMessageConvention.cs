using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Discovers every closed <see cref="IConsumer{TMessage}" /> contract implemented by a consumer type.</summary>
/// <typeparam name="TConsumer">The consumer type to inspect.</typeparam>
public sealed class AsyncConsumerMessageConvention<TConsumer> :
    IConsumerMessageConvention
    where TConsumer : class
{
    /// <summary>Discovers each valid message contract declared through <see cref="IConsumer{TMessage}" />.</summary>
    /// <returns>Connector descriptors for the discovered message contracts.</returns>
    public IEnumerable<IMessageInterfaceType> GetMessageTypes()
    {
        var consumerType = typeof(TConsumer);
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
