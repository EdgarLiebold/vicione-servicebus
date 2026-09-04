using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DynamoDbIntegration.Saga;

public interface DatabaseContext<TSaga> :
    IDisposable
    where TSaga : class, ISagaVersion
{
    Task AddAsync(TSaga instance, CancellationToken cancellationToken);

    Task InsertAsync(TSaga instance, CancellationToken cancellationToken);

    Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken);

    Task UpdateAsync(TSaga instance, CancellationToken cancellationToken);

    Task DeleteAsync(TSaga instance, CancellationToken cancellationToken);
}
