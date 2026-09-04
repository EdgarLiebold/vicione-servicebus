using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Saga;

/// <summary>
/// Defines the contract for saga repository query context.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="T">The t type.</typeparam>
public interface SagaRepositoryQueryContext<TSaga, T> :
    SagaRepositoryContext<TSaga, T>,
    IEnumerable<Guid>
    where TSaga : class, ISaga
    where T : class
{
    /// <summary>
    /// The number of matching saga instances
    /// </summary>
    int Count { get; }
}


/// <summary>
/// Defines the contract for saga repository query context.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface SagaRepositoryQueryContext<TSaga> :
    QuerySagaRepositoryContext<TSaga>,
    IEnumerable<Guid>
    where TSaga : class, ISaga
{
    /// <summary>
    /// The number of matching saga instances
    /// </summary>
    int Count { get; }
}
