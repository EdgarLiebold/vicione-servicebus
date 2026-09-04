using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Transactions;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware;

public class TransactionFilter<T> :
    IFilter<T>
    where T : class, PipeContext
{
    readonly ITransactionContextFactory _contextFactory;
    readonly TransactionOptions _options;

    public TransactionFilter(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, TimeSpan timeout = default)
        : this(isolationLevel, timeout, SystemTransactionContextFactory.Instance)
    {
    }

    internal TransactionFilter(IsolationLevel isolationLevel, TimeSpan timeout, ITransactionContextFactory contextFactory)
    {
        if (timeout == default)
            timeout = TimeSpan.FromSeconds(30);
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "The transaction timeout must be greater than zero.");

        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _options = new TransactionOptions
        {
            IsolationLevel = isolationLevel,
            Timeout = timeout
        };
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var step = context.CreateFilterScope("transaction");
        step.Add("isolationLevel", _options.IsolationLevel.ToString());
        step.Add("timeout", _options.Timeout);
    }

    [DebuggerNonUserCode]
    public async Task Send(T context, IPipe<T> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        IManagedTransactionContext managedTransactionContext = null;

        TransactionContext CreateManagedTransactionContext()
        {
            managedTransactionContext = _contextFactory.Create(_options)
                ?? throw new InvalidOperationException("The transaction context factory returned null.");

            return managedTransactionContext;
        }

        context.AddOrUpdatePayload<TransactionContext>(
            CreateManagedTransactionContext,
            existing => existing is IManagedTransactionContext { IsActive: false }
                ? CreateManagedTransactionContext()
                : existing);

        try
        {
            await next.Send(context).ConfigureAwait(false);

            if (managedTransactionContext != null)
                await managedTransactionContext.Commit().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            managedTransactionContext?.Rollback(ex);

            throw;
        }
        finally
        {
            managedTransactionContext?.Dispose();
        }
    }
}
