using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Discovers the job-message contracts implemented by a consumer type.</summary>
/// <typeparam name="T">The consumer type to inspect.</typeparam>
internal sealed class JobConsumerMessageConvention<T> :
    IConsumerMessageConvention
    where T : class
{
    /// <summary>Returns each valid job-message contract implemented by the consumer.</summary>
    /// <returns>Descriptors for the discovered job-message contracts.</returns>
    public IEnumerable<IMessageInterfaceType> GetMessageTypes()
    {
        var consumerType = typeof(T);
        if (consumerType.IsGenericType && consumerType.GetGenericTypeDefinition() == typeof(IJobConsumer<>))
        {
            var interfaceType = new JobInterfaceType(consumerType.GetGenericArguments()[0], consumerType);
            if (MessageTypeCache.IsValidMessageType(interfaceType.MessageType))
                yield return interfaceType;
        }

        IEnumerable<IMessageInterfaceType> types = consumerType.GetInterfaces()
            .Where(x => x.IsGenericType)
            .Where(x => x.GetGenericTypeDefinition() == typeof(IJobConsumer<>))
            .Select(x => new JobInterfaceType(x.GetGenericArguments()[0], consumerType))
            .Where(x => MessageTypeCache.IsValidMessageType(x.MessageType))
            .Where(x => !x.MessageType.ClosesGenericType(typeof(IMessageBatch<>)));

        foreach (var type in types)
            yield return type;
    }
}
