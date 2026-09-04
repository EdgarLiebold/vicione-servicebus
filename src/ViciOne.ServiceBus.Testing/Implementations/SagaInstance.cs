using System;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Provides a saga instance implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class SagaInstance<T> :
    ISagaInstance<T>
    where T : class, ISaga
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="saga">The saga value.</param>
    public SagaInstance(T saga)
    {
        Saga = saga;
    }

    /// <summary>
    /// Gets the saga value.
    /// </summary>
    public T Saga { get; }

    /// <summary>
    /// Gets the element id value.
    /// </summary>
    public Guid? ElementId => Saga.CorrelationId;
}
