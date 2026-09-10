using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Discovers the message contracts and connector factories exposed by a consumer type.</summary>
public interface IConsumerMessageConvention
{
    /// <summary>Returns the discovered message-contract descriptors.</summary>
    /// <returns>The message contracts and their connector factories.</returns>
    IEnumerable<IMessageInterfaceType> GetMessageTypes();
}
