using System;

namespace ViciOne.ServiceBus.Transactions;

internal interface IManagedTransactionContext : TransactionContext, IDisposable
{
    bool IsActive { get; }
}
