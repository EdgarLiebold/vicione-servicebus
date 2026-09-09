using System;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Renders bounded values for ServiceBus-owned diagnostics.</summary>
public interface IMessageDiagnosticRedactor
{
    /// <summary>Renders a value without exposing classified content or invoking application formatting code.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="memberName">The payload member name, or <see langword="null" /> when rendering the complete value.</param>
    /// <param name="value">The diagnostic value.</param>
    /// <returns>A bounded diagnostic representation or a redaction marker.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="messageType" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="memberName" /> is empty or contains only white-space characters.</exception>
    string RenderValue(Type messageType, string? memberName, object? value);
}
