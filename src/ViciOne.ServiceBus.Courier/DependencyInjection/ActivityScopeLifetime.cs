using System.Runtime.ExceptionServices;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Restores one Courier activity context and releases its optional owned scope at most once.</summary>
internal sealed class ActivityScopeLifetime :
    IAsyncDisposable
{
    readonly IDisposable _restoreContext;
    readonly IServiceScope? _scope;
    int _disposed;

    public ActivityScopeLifetime(IDisposable restoreContext, IServiceScope? scope = null)
    {
        _restoreContext = restoreContext ?? throw new ArgumentNullException(nameof(restoreContext));
        _scope = scope;
    }

    /// <summary>Restores the prior context and releases the owned scope while preserving every cleanup failure.</summary>
    /// <returns>A task that completes after all owned cleanup operations have been attempted.</returns>
    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return default;

        if (_scope is null)
        {
            _restoreContext.Dispose();
            return default;
        }

        return DisposeOwnedScopeAsync(_restoreContext, _scope);
    }

    static async ValueTask DisposeOwnedScopeAsync(IDisposable restoreContext, IServiceScope scope)
    {
        Exception? restoreFailure = null;
        try
        {
            restoreContext.Dispose();
        }
        catch (Exception exception)
        {
            restoreFailure = exception;
        }

        try
        {
            if (scope is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            else
                scope.Dispose();
        }
        catch (Exception scopeFailure) when (restoreFailure is not null)
        {
            throw new AggregateException(
                "The Courier activity context could not be restored and its owned scope could not be released.",
                restoreFailure,
                scopeFailure);
        }

        if (restoreFailure is not null)
            ExceptionDispatchInfo.Capture(restoreFailure).Throw();
    }
}
