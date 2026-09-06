using System;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>Represents an instance of saga.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class SagaInstance<T> :
    ISagaInstance<T>
    where T : class, ISaga
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="saga">The saga.</param>
    public SagaInstance(T saga)
    {
        Saga = saga;
    }

    /// <summary>Gets the saga.</summary>
    public T Saga { get; }

    /// <summary>Gets the element id.</summary>
    public Guid? ElementId => Saga.CorrelationId;
}
