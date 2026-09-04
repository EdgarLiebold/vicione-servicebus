using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DynamoDbIntegration.Saga;

public interface DatabaseContext<TSaga> :
    IDisposable
    where TSaga : class, ISagaVersion
{
    Task Add(TSaga instance, CancellationToken cancellationToken);

    Task Insert(TSaga instance, CancellationToken cancellationToken);

    Task<TSaga> Load(Guid correlationId, CancellationToken cancellationToken);

    Task Update(TSaga instance, CancellationToken cancellationToken);

    Task Delete(TSaga instance, CancellationToken cancellationToken);
}
