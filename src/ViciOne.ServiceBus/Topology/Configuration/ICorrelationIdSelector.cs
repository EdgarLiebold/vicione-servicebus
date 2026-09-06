using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by correlation id selector.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface ICorrelationIdSelector<T>
    where T : class
{
    /// <summary>Attempts to get set correlation id.</summary>
    /// <param name="messageCorrelationId">Receives the message correlation id produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetSetCorrelationId([NotNullWhen(true)] out IMessageCorrelationId<T>? messageCorrelationId);
}
