using System;
using Azure.Data.Tables;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Azure.Table.Infrastructure;

namespace ViciOne.ServiceBus.Azure.Table.Saga;

/// <summary>Provides the validated Azure Table access components used by a saga repository operation.</summary>
/// <typeparam name="TSaga">The saga state persisted through the context.</typeparam>
internal sealed class AzureTableSagaStorageContext<TSaga> :
    IAzureTableSagaStorageContext<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Creates a saga storage context and derives the entity converter for <typeparamref name="TSaga"/>.</summary>
    /// <param name="table">The Azure Table client used for saga persistence.</param>
    /// <param name="keyFormatter">The strategy that maps saga identifiers to partition and row keys.</param>
    public AzureTableSagaStorageContext(TableClient table, IAzureTableSagaKeyFormatter keyFormatter)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(keyFormatter);

        Table = table;
        Formatter = keyFormatter;

        Converter = AzureTableEntityConverterFactory.CreateConverter<TSaga>();
    }

    /// <summary>Gets the saga-key formatting strategy.</summary>
    public IAzureTableSagaKeyFormatter Formatter { get; }
    /// <summary>Gets the Azure Table client used by the repository.</summary>
    public TableClient Table { get; }
    /// <summary>Gets the converter between saga instances and Azure Table properties.</summary>
    public IAzureTableEntityConverter<TSaga> Converter { get; }

    /// <summary>Formats and validates the Azure Table keys for a saga correlation identifier.</summary>
    /// <param name="correlationId">The non-empty saga correlation identifier.</param>
    /// <returns>The validated partition key and row key.</returns>
    public (string partitionKey, string rowKey) Format(Guid correlationId)
    {
        AzureTableKeyValidator.ValidateCorrelationId(correlationId, nameof(correlationId));
        (string partitionKey, string rowKey) = Formatter.Format(correlationId);

        return (
            AzureTableKeyValidator.Validate(partitionKey, nameof(partitionKey)),
            AzureTableKeyValidator.Validate(rowKey, nameof(rowKey)));
    }
}
