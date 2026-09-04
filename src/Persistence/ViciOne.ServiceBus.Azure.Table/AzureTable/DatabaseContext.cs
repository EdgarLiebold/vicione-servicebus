using System;
using Azure.Data.Tables;

namespace ViciOne.ServiceBus.AzureTable;

/// <summary>
/// Defines the contract for database context.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface DatabaseContext<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>
    /// Gets the formatter value.
    /// </summary>
    ISagaKeyFormatter<TSaga> Formatter { get; }

    /// <summary>
    /// Gets the table value.
    /// </summary>
    TableClient Table { get; }

    /// <summary>
    /// Gets the converter value.
    /// </summary>
    IEntityConverter<TSaga> Converter { get; }

    /// <summary>
    /// Performs the format operation.
    /// </summary>
    /// <param name="correlationId">The correlation id value.</param>
    /// <returns>The result of the operation.</returns>
    (string partitionKey, string rowKey) Format(Guid correlationId);
}
