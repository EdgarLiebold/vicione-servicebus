using System;
using System.ComponentModel;

namespace ViciOne.ServiceBus.Diagnostics;
/// <summary>Inspects immutable ServiceBus diagnostic-sensitivity metadata.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMessageSensitivityInspector
{
    /// <summary>Returns the descriptor for a runtime message type.</summary>
    MessageSensitivityDescriptor Inspect(Type messageType);
}
