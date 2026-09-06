using System;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Used to format a message type into a MessageName, which can be used as a valid
/// queue name on the transport.
/// </summary>
public interface IMessageNameFormatter
{
    /// <summary>Gets message name.</summary>
    /// <param name="type">The runtime type to inspect or use.</param>
    /// <returns>The message name.</returns>
    string GetMessageName(Type type);
}
