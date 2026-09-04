using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Util;

/// <summary>
/// Provides extension methods for dispose async.
/// </summary>
public static class DisposeAsyncExtensions
{
    /// <summary>
    /// Invoke the dispose callback, and then rethrow the exception
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="disposeCallback"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="ViciOneServiceBusException"></exception>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static ValueTask<T> DisposeAsync<T>(this Exception exception, Func<Task> disposeCallback, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled<T>(cancellationToken); var dispatchInfo = ExceptionDispatchInfo.Capture(exception.GetBaseException());

        async ValueTask<T> FaultedAsync()
        {
            await disposeCallback().ConfigureAwait(false);

            dispatchInfo.Throw();

            throw new ViciOneServiceBusException("DisposeAsync", exception);
        }

        return FaultedAsync();
    }

    /// <summary>
    /// Invoke the dispose callback, and then rethrow the exception
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="disposeCallback"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="ViciOneServiceBusException"></exception>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static ValueTask<T> DisposeAsync<T>(this Exception exception, Func<ValueTask> disposeCallback, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled<T>(cancellationToken); var dispatchInfo = ExceptionDispatchInfo.Capture(exception.GetBaseException());

        async ValueTask<T> FaultedAsync()
        {
            await disposeCallback().ConfigureAwait(false);

            dispatchInfo.Throw();

            throw new ViciOneServiceBusException("DisposeAsync", exception);
        }

        return FaultedAsync();
    }
}
