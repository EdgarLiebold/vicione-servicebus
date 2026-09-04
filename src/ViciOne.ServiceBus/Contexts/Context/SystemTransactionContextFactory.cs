using System.Transactions;

namespace ViciOne.ServiceBus.Context;

internal sealed class SystemTransactionContextFactory : ITransactionContextFactory
{
    public static SystemTransactionContextFactory Instance { get; } = new();

    private SystemTransactionContextFactory()
    {
    }

    public IManagedTransactionContext Create(TransactionOptions options) => new SystemTransactionContext(options);
}
