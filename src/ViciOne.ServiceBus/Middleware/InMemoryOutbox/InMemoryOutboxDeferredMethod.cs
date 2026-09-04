using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

public class InMemoryOutboxDeferredMethod :
    IDisposable
{
    readonly Func<Task> _method;
    ExecutionContext? _executionContext;
    int _claimed;

    public InMemoryOutboxDeferredMethod(ExecutionContext? executionContext, Func<Task> method)
    {
        _executionContext = executionContext;
        _method = method;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _claimed, 1) != 0)
            return;

        Interlocked.Exchange(ref _executionContext, null)?.Dispose();
    }

    public async Task Run()
    {
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

            await task!.ConfigureAwait(false);
        }
        finally
        {
            executionContext.Dispose();
        }
    }
}
