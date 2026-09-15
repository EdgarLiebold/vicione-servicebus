using System;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides a correlation resolver for contracts that declare <see cref="IMessageCorrelation{T}" />.</summary>
/// <typeparam name="T">The message contract type.</typeparam>
sealed class CorrelatedByCorrelationIdSelector<T> :
    ICorrelationIdSelector<T>
    where T : class
{
    /// <summary>Attempts to create an interface-backed correlation resolver.</summary>
    /// <param name="messageCorrelationId">Receives the resolver when the contract declares correlation.</param>
    /// <returns><see langword="true" /> when the contract declares <see cref="IMessageCorrelation{T}" />; otherwise, <see langword="false" />.</returns>
    public bool TryGetCorrelationIdResolver([NotNullWhen(true)] out IMessageCorrelationId<T>? messageCorrelationId)
    {
        if (typeof(T).ImplementsInterface<IMessageCorrelation<Guid>>())
        {
            var objectType = typeof(InterfaceMessageCorrelationId<>).MakeGenericType(typeof(T));
            messageCorrelationId = (IMessageCorrelationId<T>)(Activator.CreateInstance(objectType)
                ?? throw new InvalidOperationException("The correlation resolver could not be activated."));
            return true;
        }

        messageCorrelationId = null;
        return false;
    }
}
