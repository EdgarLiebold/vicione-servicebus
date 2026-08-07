// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;


    public interface TransactionContext
    {
        /// <summary>
        /// Returns the current transaction scope, creating a dependent scope if a thread switch
        /// occurred
        /// </summary>
        Transaction Transaction { get; }

        /// <summary>
        /// Complete the transaction scope
        /// </summary>
        Task Commit();

        /// <summary>
        /// Rollback the transaction
        /// </summary>
        void Rollback();

        /// <summary>
        /// Rollback the transaction
        /// </summary>
        /// <param name="exception">The exception that caused the rollback</param>
        void Rollback(Exception exception);
    }
}
