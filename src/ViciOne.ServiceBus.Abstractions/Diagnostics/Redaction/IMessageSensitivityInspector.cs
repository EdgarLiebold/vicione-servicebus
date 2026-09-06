using System;

namespace ViciOne.ServiceBus.Advanced.Serialization;
/// <summary>Inspects immutable ServiceBus diagnostic-sensitivity metadata.</summary>
public interface IMessageSensitivityInspector
{
    /// <summary>Returns the descriptor for a runtime message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns>The message sensitivity descriptor produced by the operation.</returns>
    MessageSensitivityDescriptor Inspect(Type messageType);
}
