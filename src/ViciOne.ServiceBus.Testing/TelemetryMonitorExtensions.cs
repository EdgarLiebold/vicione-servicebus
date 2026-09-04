using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

#nullable enable
namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides extension methods for telemetry monitor.
/// </summary>
public static class TelemetryMonitorExtensions
{
    /// <summary>
    /// Wraps the call on the <paramref name="publishEndpoint" /> and waits for the published message to be consumed, along with
    /// all subsequently produced messages until the specified timeout.
    /// </summary>
    /// <param name="publishEndpoint"></param>
    /// <param name="callback"></param>
    /// <param name="timeout"></param>
    /// <param name="idleTimeout"></param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static async Task WaitAsync(this IPublishEndpoint publishEndpoint, Func<IPublishEndpoint, Task>? callback, TimeSpan? timeout = null,
        TimeSpan? idleTimeout = null, CancellationToken cancellationToken = default)
    {
        await WaitAsync(publishEndpoint, callback, timeout, idleTimeout, TimeProvider.System, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the wait operation.
    /// </summary>
    /// <param name="publishEndpoint">The publish endpoint value.</param>
    /// <param name="callback">The callback value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="idleTimeout">The idle timeout value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task WaitAsync(this IPublishEndpoint publishEndpoint, Func<IPublishEndpoint, Task>? callback, TimeSpan? timeout,
        TimeSpan? idleTimeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); ArgumentNullException.ThrowIfNull(publishEndpoint);
        ArgumentNullException.ThrowIfNull(timeProvider);
        var methodName = GetTestMethodInfo();

        await using var trackedActivity = new TrackedActivity(methodName, timeout, idleTimeout, timeProvider);

        try
        {
            if (callback != null)
                await callback(publishEndpoint).ConfigureAwait(false);
        }
        catch
        {
            trackedActivity.StopWaiting();
            throw;
        }
    }

    /// <summary>
    /// Wraps the call on the <paramref name="sendEndpoint" /> and waits for the sent message to be consumed, along with
    /// all subsequently produced messages until the specified timeout.
    /// </summary>
    /// <param name="sendEndpoint"></param>
    /// <param name="callback"></param>
    /// <param name="timeout"></param>
    /// <param name="idleTimeout"></param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static async Task WaitAsync(this ISendEndpoint sendEndpoint, Func<ISendEndpoint, Task>? callback, TimeSpan? timeout = null,
        TimeSpan? idleTimeout = null, CancellationToken cancellationToken = default)
    {
        await WaitAsync(sendEndpoint, callback, timeout, idleTimeout, TimeProvider.System, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the wait operation.
    /// </summary>
    /// <param name="sendEndpoint">The send endpoint value.</param>
    /// <param name="callback">The callback value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="idleTimeout">The idle timeout value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task WaitAsync(this ISendEndpoint sendEndpoint, Func<ISendEndpoint, Task>? callback, TimeSpan? timeout,
        TimeSpan? idleTimeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); ArgumentNullException.ThrowIfNull(sendEndpoint);
        ArgumentNullException.ThrowIfNull(timeProvider);
        var methodName = GetTestMethodInfo();

        await using var trackedActivity = new TrackedActivity(methodName, timeout, idleTimeout, timeProvider);

        try
        {
            if (callback != null)
                await callback(sendEndpoint).ConfigureAwait(false);
        }
        catch
        {
            trackedActivity.StopWaiting();
            throw;
        }
    }

    /// <summary>
    /// Wraps the call on the <paramref name="client" /> and waits for the request to be completed, along with
    /// all subsequently produced messages until the specified timeout.
    /// </summary>
    /// <param name="client"></param>
    /// <param name="callback"></param>
    /// <param name="timeout"></param>
    /// <param name="idleTimeout"></param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static async Task<Response<T1>> WaitAsync<T, T1>(this IRequestClient<T> client, Func<IRequestClient<T>, Task<Response<T1>>> callback,
        TimeSpan? timeout = null, TimeSpan? idleTimeout = null, CancellationToken cancellationToken = default)
        where T : class
        where T1 : class
    {
        return await WaitAsync(client, callback, timeout, idleTimeout, TimeProvider.System, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the wait operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="T1">The t1 type.</typeparam>
    /// <param name="client">The client value.</param>
    /// <param name="callback">The callback value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="idleTimeout">The idle timeout value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<Response<T1>> WaitAsync<T, T1>(this IRequestClient<T> client, Func<IRequestClient<T>, Task<Response<T1>>> callback,
        TimeSpan? timeout, TimeSpan? idleTimeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where T : class
        where T1 : class
    {
        cancellationToken.ThrowIfCancellationRequested(); ArgumentNullException.ThrowIfNull(client);
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));
        ArgumentNullException.ThrowIfNull(timeProvider);

        var methodName = GetTestMethodInfo();

        await using var trackedActivity = new TrackedActivity(methodName, timeout, idleTimeout, timeProvider);

        try
        {
            return await callback(client).ConfigureAwait(false);
        }
        catch
        {
            trackedActivity.StopWaiting();
            throw;
        }
    }

    /// <summary>
    /// Wraps the call on the <paramref name="client" /> and waits for the request to be completed, along with
    /// all subsequently produced messages until the specified timeout.
    /// </summary>
    /// <param name="client"></param>
    /// <param name="callback"></param>
    /// <param name="timeout"></param>
    /// <param name="idleTimeout"></param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static async Task<Response<T1, T2>> WaitAsync<T, T1, T2>(this IRequestClient<T> client, Func<IRequestClient<T>, Task<Response<T1, T2>>> callback,
        TimeSpan? timeout = null, TimeSpan? idleTimeout = null, CancellationToken cancellationToken = default)
        where T : class
        where T1 : class
        where T2 : class
    {
        return await WaitAsync(client, callback, timeout, idleTimeout, TimeProvider.System, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the wait operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="T1">The t1 type.</typeparam>
    /// <typeparam name="T2">The t2 type.</typeparam>
    /// <param name="client">The client value.</param>
    /// <param name="callback">The callback value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="idleTimeout">The idle timeout value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<Response<T1, T2>> WaitAsync<T, T1, T2>(this IRequestClient<T> client, Func<IRequestClient<T>, Task<Response<T1, T2>>> callback,
        TimeSpan? timeout, TimeSpan? idleTimeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where T : class
        where T1 : class
        where T2 : class
    {
        cancellationToken.ThrowIfCancellationRequested(); ArgumentNullException.ThrowIfNull(client);
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));
        ArgumentNullException.ThrowIfNull(timeProvider);

        var methodName = GetTestMethodInfo();

        await using var trackedActivity = new TrackedActivity(methodName, timeout, idleTimeout, timeProvider);

        try
        {
            return await callback(client).ConfigureAwait(false);
        }
        catch
        {
            trackedActivity.StopWaiting();
            throw;
        }
    }

    /// <summary>
    /// Wraps the call on the <paramref name="client" /> and waits for the request to be completed, along with
    /// all subsequently produced messages until the specified timeout.
    /// </summary>
    /// <param name="client"></param>
    /// <param name="callback"></param>
    /// <param name="timeout"></param>
    /// <param name="idleTimeout"></param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static async Task<Response<T1, T2, T3>> WaitAsync<T, T1, T2, T3>(this IRequestClient<T> client,
        Func<IRequestClient<T>, Task<Response<T1, T2, T3>>> callback, TimeSpan? timeout = null, TimeSpan? idleTimeout = null, CancellationToken cancellationToken = default)
        where T : class
        where T1 : class
        where T2 : class
        where T3 : class
    {
        return await WaitAsync(client, callback, timeout, idleTimeout, TimeProvider.System, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the wait operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="T1">The t1 type.</typeparam>
    /// <typeparam name="T2">The t2 type.</typeparam>
    /// <typeparam name="T3">The t3 type.</typeparam>
    /// <param name="client">The client value.</param>
    /// <param name="callback">The callback value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="idleTimeout">The idle timeout value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<Response<T1, T2, T3>> WaitAsync<T, T1, T2, T3>(this IRequestClient<T> client,
        Func<IRequestClient<T>, Task<Response<T1, T2, T3>>> callback, TimeSpan? timeout, TimeSpan? idleTimeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where T : class
        where T1 : class
        where T2 : class
        where T3 : class
    {
        cancellationToken.ThrowIfCancellationRequested(); ArgumentNullException.ThrowIfNull(client);
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));
        ArgumentNullException.ThrowIfNull(timeProvider);

        var methodName = GetTestMethodInfo();

        await using var trackedActivity = new TrackedActivity(methodName, timeout, idleTimeout, timeProvider);

        try
        {
            return await callback(client).ConfigureAwait(false);
        }
        catch
        {
            trackedActivity.StopWaiting();
            throw;
        }
    }

    static string? GetTestMethodInfo()
    {
        var stackTrace = new StackTrace(2);
        var frameCount = stackTrace.FrameCount;
        for (var i = 0; i < frameCount; i++)
        {
            var frame = stackTrace.GetFrame(i);
            if (frame == null)
                continue;

            var method = frame.GetMethod();
            if (method == null)
                continue;

            if (method.GetCustomAttributes(false).Any(x =>
                {
                    var name = x.GetType().Name;
                    return name.ToLower().Contains("test") || name.ToLower().Contains("fact");
                }))
                return method.Name;
        }

        return null;
    }
}
