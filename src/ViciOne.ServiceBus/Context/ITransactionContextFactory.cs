using System.Transactions;

namespace ViciOne.ServiceBus.Context;

internal interface ITransactionContextFactory
{
    IManagedTransactionContext Create(TransactionOptions options);
}
