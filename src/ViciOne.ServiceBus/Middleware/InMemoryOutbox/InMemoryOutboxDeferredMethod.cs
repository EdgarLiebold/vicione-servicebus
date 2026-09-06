using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>Invokes the registered method for in memory outbox deferred.</summary>
public class InMemoryOutboxDeferredMethod :
    IDisposable
{
    readonly Func<Task> _method;
    ExecutionContext? _executionContext;
    int _claimed;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="executionContext">The execution context.</param>
    /// <param name="method">The method.</param>
    public InMemoryOutboxDeferredMethod(ExecutionContext? executionContext, Func<Task> method)
    {
        _executionContext = executionContext;
        _method = method;
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
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); if (Interlocked.Exchange(ref _claimed, 1) != 0)
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

            await task!.ConfigureAwait(false);
        }
        finally
        {
            executionContext.Dispose();
        }
    }
}
