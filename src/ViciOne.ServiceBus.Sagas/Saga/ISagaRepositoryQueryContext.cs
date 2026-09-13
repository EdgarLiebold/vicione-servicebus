using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Combines a consumed message with the saga correlation identifiers selected by a repository query.</summary>
/// <typeparam name="TSaga">The persisted saga state type.</typeparam>
/// <typeparam name="TMessage">The consumed message type.</typeparam>
public interface ISagaRepositoryQueryContext<TSaga, TMessage> :
    ISagaRepositoryContext<TSaga, TMessage>,
    IEnumerable<Guid>
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>The number of matching saga instances.</summary>
    int Count { get; }
}


/// <summary>Contains the saga correlation identifiers selected by a repository query.</summary>
/// <typeparam name="TSaga">The persisted saga state type.</typeparam>
public interface ISagaRepositoryQueryContext<TSaga> :
    IQuerySagaRepositoryContext<TSaga>,
    IEnumerable<Guid>
    where TSaga : class, ISaga
{
    /// <summary>The number of matching saga instances.</summary>
    int Count { get; }
}
