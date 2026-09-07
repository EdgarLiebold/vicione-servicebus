using System;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Identifies a pipeline context that already owns the active database transaction.</summary>
public interface DbTransactionContext
{
    /// <summary>Gets the provider transaction identifier.</summary>
    Guid TransactionId { get; }
}
