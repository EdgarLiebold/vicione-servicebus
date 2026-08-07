// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Saga
{
    using System;
    using System.Collections.Generic;


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
}
