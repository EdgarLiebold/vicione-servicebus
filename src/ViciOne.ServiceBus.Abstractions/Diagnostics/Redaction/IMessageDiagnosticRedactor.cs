using System;

namespace ViciOne.ServiceBus.Advanced.Serialization;
/// <summary>Renders bounded values for ServiceBus-owned diagnostics.</summary>
public interface IMessageDiagnosticRedactor
{
    /// <summary>Renders a value without exposing classified content or invoking application formatting code.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="memberName">The member name.</param>
    /// <param name="value">The value to process.</param>
    /// <returns>The string produced by the operation.</returns>
    string RenderValue(Type messageType, string? memberName, object? value);
}
