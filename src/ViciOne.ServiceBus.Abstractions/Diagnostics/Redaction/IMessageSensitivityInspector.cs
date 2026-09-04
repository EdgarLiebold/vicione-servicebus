using System;

namespace ViciOne.ServiceBus.Advanced.Serialization;
/// <summary>Inspects immutable ServiceBus diagnostic-sensitivity metadata.</summary>
public interface IMessageSensitivityInspector
{
    /// <summary>Returns the descriptor for a runtime message type.</summary>
    MessageSensitivityDescriptor Inspect(Type messageType);
}
