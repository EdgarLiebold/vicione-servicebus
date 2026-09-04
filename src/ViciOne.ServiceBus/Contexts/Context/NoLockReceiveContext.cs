using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

#nullable enable
namespace ViciOne.ServiceBus.Context;

public class NoLockReceiveContext :
    ReceiveLockContext
{
    public static readonly ReceiveLockContext Instance = new NoLockReceiveContext();

    NoLockReceiveContext()
    {
    }

    public Task Complete()
    {
        return Task.CompletedTask;
    }

    public Task Faulted(Exception exception)
    {
        return Task.CompletedTask;
    }

    public Task ValidateLockStatus()
    {
        return Task.CompletedTask;
    }
}
