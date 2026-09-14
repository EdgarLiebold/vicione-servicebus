using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Transactions;
using ViciOne.ServiceBus.Transactions;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes transaction pipeline stages.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class TransactionFilter<T> :
    IFilter<T>
    where T : class, PipeContext
{
    readonly ITransactionContextFactory _contextFactory;
    readonly TransactionOptions _options;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="isolationLevel">The isolation level.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
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

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [DebuggerNonUserCode]
    public async Task SendAsync(T context, IPipe<T> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        IManagedTransactionContext? managedTransactionContext = null;

        ITransactionContext CreateManagedTransactionContext()
        {
            managedTransactionContext = _contextFactory.Create(_options)
                ?? throw new InvalidOperationException("The transaction context factory returned null.");

            return managedTransactionContext;
        }

        context.AddOrUpdatePayload<ITransactionContext>(
            CreateManagedTransactionContext,
            existing => existing is IManagedTransactionContext { IsActive: false }
                ? CreateManagedTransactionContext()
                : existing);

        try
        {
            await next.SendAsync(context).ConfigureAwait(false);

            if (managedTransactionContext != null)
                await managedTransactionContext.CommitAsync().ConfigureAwait(false);
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
