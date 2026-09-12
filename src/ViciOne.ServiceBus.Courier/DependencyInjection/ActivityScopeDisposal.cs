using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Releases a Courier activity's ambient-context handle and owned dependency-injection scope.</summary>
internal static class ActivityScopeDisposal
{
    /// <summary>Restores the prior context and releases the scope while preserving every cleanup failure.</summary>
    /// <param name="restoreContext">The handle that restores the previously active consume context.</param>
    /// <param name="scope">The activity scope to release.</param>
    /// <returns>A task that completes after both cleanup operations have been attempted.</returns>
    public static async ValueTask DisposeAsync(IDisposable restoreContext, IServiceScope scope)
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
