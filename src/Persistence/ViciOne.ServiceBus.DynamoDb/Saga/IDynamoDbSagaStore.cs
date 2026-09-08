using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Sagas;

namespace ViciOne.ServiceBus.DynamoDb.Saga;

internal interface IDynamoDbSagaStore<TSaga> :
    IDisposable
    where TSaga : class, ISagaVersion
{
    Task CreateAsync(TSaga instance, CancellationToken cancellationToken);

    Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken);

    Task UpdateAsync(TSaga instance, CancellationToken cancellationToken);

    Task DeleteAsync(TSaga instance, CancellationToken cancellationToken);
}
