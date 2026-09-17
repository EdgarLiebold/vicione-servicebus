using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Restores one Courier activity context and releases its optional owned scope at most once.</summary>
internal sealed class ActivityScopeLifetime :
    IAsyncDisposable
{
    readonly IDisposable _restoreContext;
    readonly IServiceScope? _scope;
    readonly object _stateLock = new();
    Task? _activeDisposal;
    bool _disposed;

    public ActivityScopeLifetime(IDisposable restoreContext, IServiceScope? scope = null)
    {
        _restoreContext = restoreContext ?? throw new ArgumentNullException(nameof(restoreContext));
        _scope = scope;
    }

    /// <summary>Restores the prior context and releases the owned scope while preserving every cleanup failure.</summary>
    /// <returns>A task that completes after all owned cleanup operations have been attempted.</returns>
    /// <remarks>Concurrent calls made during cleanup share its completion; calls made after cleanup are no-ops.</remarks>
    public ValueTask DisposeAsync()
    {
        TaskCompletionSource completion;
        Task disposal;
        lock (_stateLock)
        {
            if (_disposed)
                return default;

            if (_activeDisposal is not null)
                return new ValueTask(_activeDisposal);

            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            disposal = completion.Task;
            _activeDisposal = disposal;
        }

        _ = DisposeAndCompleteAsync(completion);
        return new ValueTask(disposal);
    }

    async Task DisposeAndCompleteAsync(TaskCompletionSource completion)
    {
        Exception? failure = null;
        try
        {
            if (_scope is null)
                _restoreContext.Dispose();
            else
                await DisposeOwnedScopeAsync(_restoreContext, _scope).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        lock (_stateLock)
        {
            _disposed = true;
            _activeDisposal = null;
        }

        if (failure is null)
            completion.TrySetResult();
        else
            completion.TrySetException(failure);
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
