using System;

namespace ViciOne.ServiceBus.AzureTable;

/// <summary>
/// Defines the contract for saga key formatter.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface ISagaKeyFormatter<in TSaga>
    where TSaga : class, ISaga
{
    /// <summary>
    /// Performs the format operation.
    /// </summary>
    /// <param name="correlationId">The correlation id value.</param>
    /// <returns>The result of the operation.</returns>
    (string partitionKey, string rowKey) Format(Guid correlationId);
}
