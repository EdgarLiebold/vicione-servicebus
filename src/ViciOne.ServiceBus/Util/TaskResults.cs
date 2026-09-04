using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace ViciOne.ServiceBus.Util;
/// <summary>
/// Shared completed task results used on allocation-sensitive paths.
/// </summary>
public static class TaskResults
{
    /// <summary>
    /// Gets the completed value.
    /// </summary>
    public static Task Completed => Task.CompletedTask;
    /// <summary>
    /// Gets the false value.
    /// </summary>
    public static Task<bool> False => Cached.False;
    /// <summary>
    /// Gets the true value.
    /// </summary>
    public static Task<bool> True => Cached.True;

    /// <summary>
    /// Performs the default operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<T?> DefaultAsync<T>(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<T?>(cancellationToken);
        return Cached<T>.Default;
    }
    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<T> FaultedAsync<T>(Exception exception, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<T>(cancellationToken); ArgumentNullException.ThrowIfNull(exception);
        return Task.FromException<T>(exception);
    }

    /// <summary>
    /// Determines whether the current value can celed.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<T> CanceledAsync<T>(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<T>(cancellationToken);
        return Cached<T>.Canceled;
    }

    static class Cached
    {
        public static readonly Task<bool> True = Task.FromResult(true);
        public static readonly Task<bool> False = Task.FromResult(false);
    }


    static class Cached<T>
    {
        public static readonly Task<T?> Default = Task.FromResult<T?>(default);
        public static readonly Task<T> Canceled = Task.FromCanceled<T>(new CancellationToken(canceled: true));
    }
}
