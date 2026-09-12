using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Discovers every closed <see cref="IConsumer{TMessage}" /> contract whose message is an <see cref="IMessageBatch{TMessage}" />.</summary>
/// <typeparam name="TConsumer">The consumer type to inspect.</typeparam>
internal sealed class BatchConsumerMessageConvention<TConsumer> :
    IConsumerMessageConvention
    where TConsumer : class
{
    /// <summary>Discovers each valid <see cref="IMessageBatch{TMessage}" /> contract declared by the consumer.</summary>
    /// <returns>Batch connector descriptors for the discovered contracts.</returns>
    public IEnumerable<IMessageInterfaceType> GetMessageTypes()
    {
        var consumerType = typeof(TConsumer);
        if (consumerType.IsGenericType && consumerType.GetGenericTypeDefinition() == typeof(IConsumer<>))
        {
            var messageType = consumerType.GetGenericArguments()[0];
            if (messageType.TryGetSingleClosedGenericArguments(typeof(IMessageBatch<>), out Type[] batchTypes))
            {
                var interfaceType = new BatchConsumerInterfaceType(messageType, batchTypes[0], consumerType);
                if (MessageTypeCache.IsValidMessageType(interfaceType.MessageType))
                    yield return interfaceType;
            }
        }

        IEnumerable<IMessageInterfaceType> types = consumerType.GetInterfaces()
            .Where(x => x.IsGenericType)
            .Where(x => x.GetGenericTypeDefinition() == typeof(IConsumer<>))
            .Select(x => new
            {
                Type = x,
                MessageType = x.GetGenericArguments()[0]
            })
            .Where(x => x.MessageType.ClosesGenericType(typeof(IMessageBatch<>)))
            .Select(x => new
            {
                x.Type,
                BatchMessageType = x.MessageType,
                MessageType = x.MessageType.GetSingleClosedGenericArgument(typeof(IMessageBatch<>))
            })
            .Select(x => new BatchConsumerInterfaceType(x.BatchMessageType, x.MessageType, consumerType))
            .Where(x => MessageTypeCache.IsValidMessageType(x.MessageType));

        foreach (var type in types)
            yield return type;
    }
}
