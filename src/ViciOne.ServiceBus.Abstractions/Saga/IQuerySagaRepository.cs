using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

public interface IQuerySagaRepository<TSaga> :
    IProbeSite
    where TSaga : class, ISaga
{
    Task<IEnumerable<Guid>> FindAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken = default);
}
