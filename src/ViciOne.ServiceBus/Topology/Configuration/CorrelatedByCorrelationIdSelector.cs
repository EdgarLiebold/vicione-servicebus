using System;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Selects correlated by correlation id values.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class CorrelatedByCorrelationIdSelector<T> :
    ICorrelationIdSelector<T>
    where T : class
{
    /// <summary>Attempts to get set correlation id.</summary>
    /// <param name="messageCorrelationId">Receives the message correlation id produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetSetCorrelationId([NotNullWhen(true)] out IMessageCorrelationId<T>? messageCorrelationId)
    {
        if (typeof(T).ImplementsInterface<IMessageCorrelation<Guid>>())
        {
            var objectType = typeof(InterfaceMessageCorrelationId<>).MakeGenericType(typeof(T));
            messageCorrelationId = (IMessageCorrelationId<T>)(Activator.CreateInstance(objectType) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));
            return true;
        }

        messageCorrelationId = null;
        return false;
    }
}
