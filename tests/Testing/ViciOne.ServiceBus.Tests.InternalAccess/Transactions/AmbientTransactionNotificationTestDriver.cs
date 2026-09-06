using System.Transactions;
using ViciOne.ServiceBus.Transactions;

namespace ViciOne.ServiceBus.Tests.InternalAccess.Transactions;

public sealed class AmbientTransactionNotificationTestDriver
{
    private readonly AmbientTransactionNotification _notification = new();

    public Task EnqueueAsync(Func<CancellationToken, Task> action)
    {
        _notification.Add(action);
        return Task.CompletedTask;
    }

    public void CompleteInDoubtThroughARealEnlistment()
    {
        using var transaction = new CommittableTransaction();
        transaction.EnlistVolatile(new InDoubtForwarder(_notification), EnlistmentOptions.None);
        transaction.Rollback();
    }

    private sealed class InDoubtForwarder(AmbientTransactionNotification notification) : IEnlistmentNotification
    {
        public void Prepare(PreparingEnlistment preparingEnlistment) => preparingEnlistment.Prepared();

        public void Commit(Enlistment enlistment) => enlistment.Done();

        public void Rollback(Enlistment enlistment) => notification.InDoubt(enlistment);

        public void InDoubt(Enlistment enlistment) => notification.InDoubt(enlistment);
    }
}
