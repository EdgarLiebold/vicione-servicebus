using System;

namespace ViciOne.ServiceBus.Transactions;

internal interface IManagedTransactionContext : ITransactionContext, IDisposable
{
    bool IsActive { get; }
}
