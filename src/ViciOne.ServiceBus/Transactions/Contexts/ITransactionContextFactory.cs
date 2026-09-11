using System.Transactions;

namespace ViciOne.ServiceBus.Transactions;

internal interface ITransactionContextFactory
{
    IManagedTransactionContext Create(TransactionOptions options);
}
