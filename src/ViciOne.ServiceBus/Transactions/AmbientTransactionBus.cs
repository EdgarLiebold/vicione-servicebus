namespace ViciOne.ServiceBus.Transactions
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;


    internal sealed class AmbientTransactionBus :
        DeferredBus,
        IAmbientTransactionBus
    {
        readonly ConcurrentDictionary<Transaction, Lazy<AmbientTransactionNotification>> _pendingActions;

        public AmbientTransactionBus(IBus bus)
            : base(bus)
        {
            _pendingActions = new ConcurrentDictionary<Transaction, Lazy<AmbientTransactionNotification>>();
        }

        internal int PendingTransactionCount => _pendingActions.Count;

        internal override Task Add(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            cancellationToken.ThrowIfCancellationRequested();

            Transaction transaction = Transaction.Current;
            if (transaction == null)
                return action(cancellationToken);

            GetOrCreateEnlistment(transaction).Add(action);
            return Task.CompletedTask;
        }

        void ClearTransaction(Transaction transaction)
        {
            if (_pendingActions.TryRemove(transaction, out _))
                transaction.TransactionCompleted -= TransactionCompleted;
        }

        AmbientTransactionNotification GetOrCreateEnlistment(Transaction transaction)
        {
            Lazy<AmbientTransactionNotification> notification = _pendingActions.GetOrAdd(transaction, current =>
                new Lazy<AmbientTransactionNotification>(() => CreateNotification(current), LazyThreadSafetyMode.ExecutionAndPublication));

            try
            {
                return notification.Value;
            }
            catch
            {
                _pendingActions.TryRemove(transaction, out _);
                throw;
            }
        }

        AmbientTransactionNotification CreateNotification(Transaction transaction)
        {
            var notification = new AmbientTransactionNotification();

            transaction.TransactionCompleted += TransactionCompleted;
            try
            {
                transaction.EnlistVolatile(notification, EnlistmentOptions.None);
                return notification;
            }
            catch
            {
                transaction.TransactionCompleted -= TransactionCompleted;
                throw;
            }
        }

        void TransactionCompleted(object sender, TransactionEventArgs e)
        {
            ClearTransaction(e.Transaction);
        }
    }
}
