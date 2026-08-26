namespace ViciOne.ServiceBus.Context;

using System.Transactions;


internal interface ITransactionContextFactory
{
    IManagedTransactionContext Create(TransactionOptions options);
}
