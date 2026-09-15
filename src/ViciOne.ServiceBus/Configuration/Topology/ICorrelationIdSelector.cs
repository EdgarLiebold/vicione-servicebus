using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Locates a correlation resolver for a message contract.</summary>
/// <typeparam name="T">The message contract type.</typeparam>
interface ICorrelationIdSelector<T>
    where T : class
{
    /// <summary>Attempts to locate a correlation resolver.</summary>
    /// <param name="messageCorrelationId">Receives the resolver when one is available.</param>
    /// <returns><see langword="true" /> when a resolver is available; otherwise, <see langword="false" />.</returns>
    bool TryGetCorrelationIdResolver([NotNullWhen(true)] out IMessageCorrelationId<T>? messageCorrelationId);
}
