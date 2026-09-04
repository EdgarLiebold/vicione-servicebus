using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

public interface ILoadSagaRepository<TSaga> :
    IProbeSite
    where TSaga : class, ISaga
{
    Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default);
}
