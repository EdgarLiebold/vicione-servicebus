using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Exposes state for saga repository query operations.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="T">The value type.</typeparam>
public interface SagaRepositoryQueryContext<TSaga, T> :
    SagaRepositoryContext<TSaga, T>,
    IEnumerable<Guid>
    where TSaga : class, ISaga
    where T : class
{
    /// <summary>The number of matching saga instances.</summary>
    int Count { get; }
}


/// <summary>Exposes state for saga repository query operations.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface SagaRepositoryQueryContext<TSaga> :
    QuerySagaRepositoryContext<TSaga>,
    IEnumerable<Guid>
    where TSaga : class, ISaga
{
    /// <summary>The number of matching saga instances.</summary>
    int Count { get; }
}
