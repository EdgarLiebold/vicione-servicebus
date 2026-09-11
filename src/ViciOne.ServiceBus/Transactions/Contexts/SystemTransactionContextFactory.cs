using System.Transactions;

namespace ViciOne.ServiceBus.Transactions;

internal sealed class SystemTransactionContextFactory : ITransactionContextFactory
{
    public static SystemTransactionContextFactory Instance { get; } = new();

    SystemTransactionContextFactory()
    {
    }

    public IManagedTransactionContext Create(TransactionOptions options) => new SystemTransactionContext(options);
}
