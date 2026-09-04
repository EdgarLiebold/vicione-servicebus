using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for correlation id selector.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface ICorrelationIdSelector<T>
    where T : class
{
    /// <summary>
    /// Attempts to get set correlation id.
    /// </summary>
    /// <param name="messageCorrelationId">The message correlation id value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetSetCorrelationId([NotNullWhen(true)] out IMessageCorrelationId<T>? messageCorrelationId);
}
