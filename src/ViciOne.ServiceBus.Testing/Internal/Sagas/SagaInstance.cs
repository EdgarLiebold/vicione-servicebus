using System;

namespace ViciOne.ServiceBus.Testing.Internal;

/// <summary>Represents a recorded saga instance.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class SagaInstance<TSaga> :
    ISagaInstance<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Creates an observation for a saga state.</summary>
    /// <param name="saga">The recorded saga state.</param>
    public SagaInstance(TSaga saga)
    {
        Saga = saga ?? throw new ArgumentNullException(nameof(saga));
    }

    /// <inheritdoc />
    public TSaga Saga { get; }

    /// <inheritdoc />
    public Guid? ElementId => Saga.CorrelationId;
}
