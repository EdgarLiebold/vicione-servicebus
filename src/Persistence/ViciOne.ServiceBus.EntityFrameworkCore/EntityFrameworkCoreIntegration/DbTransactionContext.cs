using System;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Implemented when a filter/context has already started and is managing the transaction
/// </summary>
public interface DbTransactionContext
{
    /// <summary>
    /// Gets the transaction id value.
    /// </summary>
    Guid TransactionId { get; }
}
