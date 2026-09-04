using System;

namespace ViciOne.ServiceBus.Context;

internal interface IManagedTransactionContext : TransactionContext, IDisposable
{
    bool IsActive { get; }
}
