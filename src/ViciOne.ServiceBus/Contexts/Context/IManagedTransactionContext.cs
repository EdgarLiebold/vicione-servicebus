namespace ViciOne.ServiceBus.Context;

using System;


internal interface IManagedTransactionContext : TransactionContext, IDisposable
{
    bool IsActive { get; }
}
