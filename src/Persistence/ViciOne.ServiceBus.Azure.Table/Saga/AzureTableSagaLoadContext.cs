using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Azure.Table.Saga;

/// <summary>Provides load-only Azure Table access outside a message consume context.</summary>
/// <typeparam name="TSaga">The saga state loaded by the repository.</typeparam>
internal sealed class AzureTableSagaLoadContext<TSaga>(
    IAzureTableSagaStorageContext<TSaga> context,
    CancellationToken cancellationToken) :
    BasePipeContext(cancellationToken),
    ILoadSagaRepositoryContext<TSaga>
    where TSaga : class, ISaga
{
    readonly IAzureTableSagaStorageContext<TSaga> _context =
        context ?? throw new ArgumentNullException(nameof(context));

    /// <summary>Loads a saga by its formatted correlation key.</summary>
    /// <param name="correlationId">The non-empty saga correlation identifier.</param>
    /// <param name="cancellationToken">The token checked before loading; Azure I/O uses the load-context lifetime token.</param>
    /// <returns>A task whose result is the saga state, or <see langword="null"/> when no entity exists.</returns>
    public async Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (partitionKey, rowKey) = _context.Format(correlationId);

        NullableResponse<TableEntity> result = await _context.Table
            .GetEntityIfExistsAsync<TableEntity>(
                partitionKey,
                rowKey,
                cancellationToken: CancellationToken)
            .ConfigureAwait(false);

        return result.HasValue
            ? _context.Converter.GetObject(new TableEntity(result.Value))
            : null;
    }
}
