using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Releases an ambient-context restoration handle and an optional owned dependency-injection scope exactly once.</summary>
internal sealed class ConsumeScopeLifetime :
    IAsyncDisposable
{
    readonly IDisposable _restoreContext;
    readonly IServiceScope? _scope;
    int _disposed;

    public ConsumeScopeLifetime(IDisposable restoreContext, IServiceScope? scope = null)
    {
        _restoreContext = restoreContext ?? throw new ArgumentNullException(nameof(restoreContext));
        _scope = scope;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        Exception? restoreFailure = null;
        try
        {
            _restoreContext.Dispose();
        }
        catch (Exception exception)
        {
            restoreFailure = exception;
        }

        Exception? scopeFailure = null;
        try
        {
            if (_scope is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            else
                _scope?.Dispose();
        }
        catch (Exception exception)
        {
            scopeFailure = exception;
        }

        if (restoreFailure is not null && scopeFailure is not null)
        {
            throw new AggregateException(
                "The consume context could not be restored and its dependency-injection scope could not be released.",
                restoreFailure,
                scopeFailure);
        }

        if (restoreFailure is not null)
            ExceptionDispatchInfo.Capture(restoreFailure).Throw();

        if (scopeFailure is not null)
            ExceptionDispatchInfo.Capture(scopeFailure).Throw();
    }
}
