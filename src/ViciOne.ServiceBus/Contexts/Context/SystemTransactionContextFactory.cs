namespace ViciOne.ServiceBus.Context;

using System.Transactions;


internal sealed class SystemTransactionContextFactory : ITransactionContextFactory
{
    public static SystemTransactionContextFactory Instance { get; } = new();

    private SystemTransactionContextFactory()
    {
    }

    public IManagedTransactionContext Create(TransactionOptions options) => new SystemTransactionContext(options);
}
