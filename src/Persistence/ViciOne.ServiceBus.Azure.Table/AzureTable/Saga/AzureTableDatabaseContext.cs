using System;
using Azure.Data.Tables;

namespace ViciOne.ServiceBus.AzureTable.Saga;

/// <summary>
/// Provides an azure table database context implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class AzureTableDatabaseContext<TSaga> :
    DatabaseContext<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="keyFormatter">The key formatter value.</param>
    public AzureTableDatabaseContext(TableClient table, ISagaKeyFormatter<TSaga> keyFormatter)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(keyFormatter);

        Table = table;
        Formatter = keyFormatter;

        Converter = EntityConverterFactory.CreateConverter<TSaga>();
    }

    /// <summary>
    /// Gets the formatter value.
    /// </summary>
    public ISagaKeyFormatter<TSaga> Formatter { get; }
    /// <summary>
    /// Gets the table value.
    /// </summary>
    public TableClient Table { get; }
    /// <summary>
    /// Gets the converter value.
    /// </summary>
    public IEntityConverter<TSaga> Converter { get; }

    /// <summary>
    /// Performs the format operation.
    /// </summary>
    /// <param name="correlationId">The correlation id value.</param>
    /// <returns>The result of the operation.</returns>
    public (string partitionKey, string rowKey) Format(Guid correlationId)
    {
        AzureTableKeyValidator.ValidateCorrelationId(correlationId, nameof(correlationId));
        (string partitionKey, string rowKey) = Formatter.Format(correlationId);

        return (
            AzureTableKeyValidator.Validate(partitionKey, nameof(partitionKey)),
            AzureTableKeyValidator.Validate(rowKey, nameof(rowKey)));
    }
}
