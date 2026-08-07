// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration
{
    using System;


    /// <summary>
    /// Implemented when a filter/context has already started and is managing the transaction
    /// </summary>
    public interface DbTransactionContext
    {
        Guid TransactionId { get; }
    }
}
