using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Saga;

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
