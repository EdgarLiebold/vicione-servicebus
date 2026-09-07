using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>Owns one deferred operation and the execution context captured when it was queued.</summary>
internal sealed class InMemoryOutboxDeferredMethod :
    IDisposable
{
    readonly Func<Task> _method;
    ExecutionContext? _executionContext;
    int _claimed;

    /// <summary>Initializes a deferred operation.</summary>
    /// <param name="executionContext">The optional execution context to restore during delivery.</param>
    /// <param name="method">The asynchronous operation to invoke exactly once.</param>
    public InMemoryOutboxDeferredMethod(ExecutionContext? executionContext, Func<Task> method)
    {
        _executionContext = executionContext;
        _method = method ?? throw new ArgumentNullException(nameof(method));
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _claimed, 1) != 0)
            return;

        Interlocked.Exchange(ref _executionContext, null)?.Dispose();
    }

    /// <summary>Runs the configured operation.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after the operation has run, or immediately if it was already claimed.</returns>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Interlocked.Exchange(ref _claimed, 1) != 0)
            return;

        ExecutionContext? executionContext = Interlocked.Exchange(ref _executionContext, null);
        if (executionContext == null)
        {
            Task task;
            if (ExecutionContext.IsFlowSuppressed())
                task = Task.Run(_method);
            else
            {
                using (ExecutionContext.SuppressFlow())
                    task = Task.Run(_method);
            }

            await task.ConfigureAwait(false);
            return;
        }

        try
        {
            using var ec = executionContext.CreateCopy();

            Task? task = null;

            ExecutionContext.Run(ec, _ => task = _method(), null);

            if (task == null)
                throw new InvalidOperationException("The deferred outbox operation returned a null task.");

            await task.ConfigureAwait(false);
        }
        finally
        {
            executionContext.Dispose();
        }
    }
}
