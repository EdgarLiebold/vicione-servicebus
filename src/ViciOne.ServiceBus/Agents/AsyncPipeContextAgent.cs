using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Agents;

/// <summary>
/// A PipeContext, which as an agent can be Stopped, which disposes of the context making it unavailable
/// </summary>
/// <typeparam name="TContext"></typeparam>
public class AsyncPipeContextAgent<TContext> :
    IAsyncPipeContextAgent<TContext>
    where TContext : class, PipeContext
{
    readonly IPipeContextAgent<TContext> _agent;
    readonly TaskCompletionSource<TContext> _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public AsyncPipeContextAgent()
    {
        _context = TaskCompletionSources.Create<TContext>();

        _agent = new PipeContextAgent<TContext>(_context.Task);
    }

    bool PipeContextHandle<TContext>.IsDisposed => _agent.IsDisposed;

    Task<TContext> PipeContextHandle<TContext>.Context => _agent.Context;

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        return _agent.DisposeAsync();
    }

    Task IAgent.Ready => _agent.Ready;
    Task IAgent.Completed => _agent.Completed;

    CancellationToken IAgent.Stopping => _agent.Stopping;
    CancellationToken IAgent.Stopped => _agent.Stopped;

    Task IAgent.StopAsync(StopContext context, CancellationToken cancellationToken)
    {
        return _agent.StopAsync(context, cancellationToken: cancellationToken);
    }

    Task IAsyncPipeContextHandle<TContext>.CreatedAsync(TContext context, CancellationToken cancellationToken)
    {
        _context.SetResult(context);

        return Task.CompletedTask;
    }

    Task IAsyncPipeContextHandle<TContext>.CreateCanceledAsync(CancellationToken cancellationToken)
    {
        _context.SetCanceled();

        return _agent.StopAsync("Create Canceled", CancellationToken.None);
    }

    Task IAsyncPipeContextHandle<TContext>.CreateFaultedAsync(Exception exception, CancellationToken cancellationToken)
    {
        _context.SetException(exception);

        return _agent.StopAsync($"Create Faulted: {exception.GetBaseException().Message}", CancellationToken.None);
    }

    Task IAsyncPipeContextHandle<TContext>.FaultedAsync(Exception exception, CancellationToken cancellationToken)
    {
        return _agent.StopAsync($"Faulted: {exception.GetBaseException().Message}", CancellationToken.None);
    }

    /// <inheritdoc />
    public override string? ToString()
    {
        return _agent.ToString();
    }
}
