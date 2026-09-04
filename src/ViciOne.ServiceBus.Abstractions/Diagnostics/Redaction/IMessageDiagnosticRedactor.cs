namespace ViciOne.ServiceBus.Diagnostics;

using System;
using System.ComponentModel;

/// <summary>Renders bounded values for ServiceBus-owned diagnostics.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMessageDiagnosticRedactor
{
    /// <summary>Renders a value without exposing classified content or invoking application formatting code.</summary>
    string RenderValue(Type messageType, string? memberName, object? value);
}
