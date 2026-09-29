using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Publishes an asynchronously created pipe context through a supervised lifecycle.</summary>
/// <typeparam name="TContext">The pipe context type.</typeparam>
public sealed class AsyncPipeContextAgent<TContext> :
    IAsyncPipeContextAgent<TContext>
    where TContext : class, PipeContext
{
    readonly IPipeContextAgent<TContext> _agent;
    readonly TaskCompletionSource<TContext> _context;

    /// <summary>Initializes an agent that awaits a single pipe-context creation outcome.</summary>
    public AsyncPipeContextAgent()
    {
        _context = TaskCompletionSources.Create<TContext>();

        _agent = new PipeContextAgent<TContext>(_context.Task);
    }

    bool IPipeContextHandle<TContext>.IsDisposed => _agent.IsDisposed;

    Task<TContext> IPipeContextHandle<TContext>.Context => _agent.Context;

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        _context.TrySetCanceled();

        return _agent.DisposeAsync();
    }

    Task IAgent.Ready => _agent.Ready;
    Task IAgent.Completed => _agent.Completed;

    CancellationToken IAgent.Stopping => _agent.Stopping;
    CancellationToken IAgent.Stopped => _agent.Stopped;

    Task IAgent.StopAsync(StopContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context.TrySetCanceled(context.CancellationToken);

        return _agent.StopAsync(context, cancellationToken: cancellationToken);
    }

    Task IAsyncPipeContextHandle<TContext>.CreatedAsync(TContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_context.TrySetResult(context))
            return PipeContextDisposer.DisposeAsync(context);

        return _agent.Context;
    }

    Task IAsyncPipeContextHandle<TContext>.CreateCanceledAsync(CancellationToken cancellationToken)
    {
        if (!_context.TrySetCanceled(cancellationToken))
            return Task.CompletedTask;

        return _agent.StopAsync("Create Canceled", CancellationToken.None);
    }

    Task IAsyncPipeContextHandle<TContext>.CreateFaultedAsync(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (!_context.TrySetException(exception))
            return Task.CompletedTask;

        return _agent.StopAsync($"Create Faulted: {GetFailureMessage(exception)}", CancellationToken.None);
    }

    Task IAsyncPipeContextHandle<TContext>.FaultedAsync(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        _context.TrySetException(exception);

        return _agent.StopAsync($"Faulted: {GetFailureMessage(exception)}", CancellationToken.None);
    }

    static string GetFailureMessage(Exception exception)
    {
        try
        {
            exception = exception.GetBaseException() ?? exception;
        }
        catch
        {
            // An exception's custom base lookup cannot prevent the owned agent from stopping.
        }

        return ExceptionUtil.GetMessage(exception);
    }

    /// <inheritdoc />
    public override string? ToString()
    {
        return _agent.ToString();
    }
}
