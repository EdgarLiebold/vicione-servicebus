using System;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Inspects immutable ServiceBus diagnostic-sensitivity metadata.</summary>
public interface IMessageSensitivityInspector
{
    /// <summary>Returns the descriptor for a runtime message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns>The sensitivity classification for the message payload and its members.</returns>
    MessageSensitivityDescriptor Inspect(Type messageType);
}
