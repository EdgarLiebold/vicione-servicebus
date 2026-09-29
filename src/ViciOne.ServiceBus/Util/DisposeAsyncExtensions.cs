using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Util;

/// <summary>Provides extension methods for dispose async.</summary>
public static class DisposeAsyncExtensions
{
    /// <summary>Invoke the dispose callback, and then rethrow the exception.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="disposeCallback">The dispose callback.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the dispose outcome.</returns>
    /// <exception cref="ViciOneServiceBusException">Thrown when the operation cannot be completed.</exception>
    public static ValueTask<T> DisposeAsync<T>(this Exception exception, Func<Task> disposeCallback, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return global::System.Threading.Tasks.ValueTask.FromCanceled<T>(cancellationToken);

        var dispatchInfo = CaptureFailure(exception);

        async ValueTask<T> FaultedAsync()
        {
            try
            {
                await disposeCallback().ConfigureAwait(false);
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException(dispatchInfo.SourceException, cleanupException);
            }

            dispatchInfo.Throw();

            throw new ViciOneServiceBusException("DisposeAsync", exception);
        }

        return FaultedAsync();
    }

    /// <summary>Invoke the dispose callback, and then rethrow the exception.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="disposeCallback">The dispose callback.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the dispose outcome.</returns>
    /// <exception cref="ViciOneServiceBusException">Thrown when the operation cannot be completed.</exception>
    public static ValueTask<T> DisposeAsync<T>(this Exception exception, Func<ValueTask> disposeCallback, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return global::System.Threading.Tasks.ValueTask.FromCanceled<T>(cancellationToken);

        var dispatchInfo = CaptureFailure(exception);

        async ValueTask<T> FaultedAsync()
        {
            try
            {
                await disposeCallback().ConfigureAwait(false);
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException(dispatchInfo.SourceException, cleanupException);
            }

            dispatchInfo.Throw();

            throw new ViciOneServiceBusException("DisposeAsync", exception);
        }

        return FaultedAsync();
    }

    static ExceptionDispatchInfo CaptureFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Exception cause = exception;
        try
        {
            cause = exception.GetBaseException() ?? exception;
        }
        catch
        {
            // Cleanup must still run when exception diagnostics fail.
        }

        return ExceptionDispatchInfo.Capture(cause);
    }
}
