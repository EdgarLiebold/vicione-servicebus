namespace ViciOne.ServiceBus.Diagnostics;

using System;

/// <summary>Inspects immutable ServiceBus diagnostic-sensitivity metadata.</summary>
public interface IMessageSensitivityInspector
{
    /// <summary>Returns the descriptor for a runtime message type.</summary>
    MessageSensitivityDescriptor Inspect(Type messageType);
}
