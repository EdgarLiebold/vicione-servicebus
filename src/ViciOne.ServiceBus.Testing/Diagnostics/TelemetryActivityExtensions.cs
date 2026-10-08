using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Executes messaging operations and waits for their causally related telemetry activity to become idle.</summary>
public static class TelemetryActivityExtensions
{
    /// <summary>
    /// Executes a publish operation and waits until the resulting telemetry activity becomes idle or the operation timeout expires.
    /// </summary>
    /// <param name="publishEndpoint">The endpoint used by the publish operation.</param>
    /// <param name="action">The publish operation to execute.</param>
    /// <param name="timeout">The maximum time spent observing activity.</param>
    /// <param name="idleTimeout">The required period without active spans.</param>
    /// <param name="cancellationToken">The token checked before the action and used to cancel its subsequent activity wait.</param>
    /// <returns>A task that represents the operation and its activity wait.</returns>
    public static Task ExecuteAndWaitForIdleAsync(this IPublishEndpoint publishEndpoint, Func<IPublishEndpoint, Task> action,
        TimeSpan? timeout = null, TimeSpan? idleTimeout = null, CancellationToken cancellationToken = default)
    {
        return ExecuteAndWaitForIdleAsync(publishEndpoint, action, timeout, idleTimeout, TimeProvider.System, cancellationToken);
    }

    /// <summary>
    /// Executes a publish operation and waits, using the supplied time source, until the resulting telemetry activity becomes idle
    /// or the operation timeout expires.
    /// </summary>
    /// <param name="publishEndpoint">The endpoint used by the publish operation.</param>
    /// <param name="action">The publish operation to execute.</param>
    /// <param name="timeout">The maximum time spent observing activity.</param>
    /// <param name="idleTimeout">The required period without active spans.</param>
    /// <param name="timeProvider">The time source used for timeout measurement.</param>
    /// <param name="cancellationToken">The token checked before the action and used to cancel its subsequent activity wait.</param>
    /// <returns>A task that represents the operation and its activity wait.</returns>
    public static async Task ExecuteAndWaitForIdleAsync(this IPublishEndpoint publishEndpoint, Func<IPublishEndpoint, Task> action,
        TimeSpan? timeout, TimeSpan? idleTimeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publishEndpoint);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(timeProvider);
        cancellationToken.ThrowIfCancellationRequested();

        var trackedActivity = new TrackedActivity(GetTestMethodName(), timeout, idleTimeout, timeProvider);
        bool bodyCompleted = false;

        try
        {
            await action(publishEndpoint).ConfigureAwait(false);
            await trackedActivity.WaitForCompletionAsync(cancellationToken).ConfigureAwait(false);
            bodyCompleted = true;
        }
        catch
        {
            trackedActivity.StopWaiting();
            throw;
        }
        finally
        {
            DisposeTracker(trackedActivity, bodyCompleted);
        }
    }

    /// <summary>
    /// Executes a send operation and waits until the resulting telemetry activity becomes idle or the operation timeout expires.
    /// </summary>
    /// <param name="sendEndpoint">The endpoint used by the send operation.</param>
    /// <param name="action">The send operation to execute.</param>
    /// <param name="timeout">The maximum time spent observing activity.</param>
    /// <param name="idleTimeout">The required period without active spans.</param>
    /// <param name="cancellationToken">The token checked before the action and used to cancel its subsequent activity wait.</param>
    /// <returns>A task that represents the operation and its activity wait.</returns>
    public static Task ExecuteAndWaitForIdleAsync(this ISendEndpoint sendEndpoint, Func<ISendEndpoint, Task> action,
        TimeSpan? timeout = null, TimeSpan? idleTimeout = null, CancellationToken cancellationToken = default)
    {
        return ExecuteAndWaitForIdleAsync(sendEndpoint, action, timeout, idleTimeout, TimeProvider.System, cancellationToken);
    }

    /// <summary>
    /// Executes a send operation and waits, using the supplied time source, until the resulting telemetry activity becomes idle
    /// or the operation timeout expires.
    /// </summary>
    /// <param name="sendEndpoint">The endpoint used by the send operation.</param>
    /// <param name="action">The send operation to execute.</param>
    /// <param name="timeout">The maximum time spent observing activity.</param>
    /// <param name="idleTimeout">The required period without active spans.</param>
    /// <param name="timeProvider">The time source used for timeout measurement.</param>
    /// <param name="cancellationToken">The token checked before the action and used to cancel its subsequent activity wait.</param>
    /// <returns>A task that represents the operation and its activity wait.</returns>
    public static async Task ExecuteAndWaitForIdleAsync(this ISendEndpoint sendEndpoint, Func<ISendEndpoint, Task> action,
        TimeSpan? timeout, TimeSpan? idleTimeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sendEndpoint);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(timeProvider);
        cancellationToken.ThrowIfCancellationRequested();

        var trackedActivity = new TrackedActivity(GetTestMethodName(), timeout, idleTimeout, timeProvider);
        bool bodyCompleted = false;

        try
        {
            await action(sendEndpoint).ConfigureAwait(false);
            await trackedActivity.WaitForCompletionAsync(cancellationToken).ConfigureAwait(false);
            bodyCompleted = true;
        }
        catch
        {
            trackedActivity.StopWaiting();
            throw;
        }
        finally
        {
            DisposeTracker(trackedActivity, bodyCompleted);
        }
    }

    /// <summary>
    /// Executes a request and waits until the complete request activity becomes idle before returning its response.
    /// </summary>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="client">The request client used by the operation.</param>
    /// <param name="action">The request operation to execute.</param>
    /// <param name="timeout">The maximum time spent observing activity.</param>
    /// <param name="idleTimeout">The required period without active spans.</param>
    /// <param name="cancellationToken">The token checked before the action and used to cancel its subsequent activity wait.</param>
    /// <returns>The response produced by <paramref name="action" />.</returns>
    public static Task<Response<TResponse>> ExecuteAndWaitForIdleAsync<TRequest, TResponse>(this IRequestClient<TRequest> client,
        Func<IRequestClient<TRequest>, Task<Response<TResponse>>> action, TimeSpan? timeout = null, TimeSpan? idleTimeout = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        return ExecuteAndWaitForIdleAsync(client, action, timeout, idleTimeout, TimeProvider.System, cancellationToken);
    }

    /// <summary>
    /// Executes a request and waits, using the supplied time source, until the complete request activity becomes idle before
    /// returning its response.
    /// </summary>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="client">The request client used by the operation.</param>
    /// <param name="action">The request operation to execute.</param>
    /// <param name="timeout">The maximum time spent observing activity.</param>
    /// <param name="idleTimeout">The required period without active spans.</param>
    /// <param name="timeProvider">The time source used for timeout measurement.</param>
    /// <param name="cancellationToken">The token checked before the action and used to cancel its subsequent activity wait.</param>
    /// <returns>The response produced by <paramref name="action" />.</returns>
    public static async Task<Response<TResponse>> ExecuteAndWaitForIdleAsync<TRequest, TResponse>(this IRequestClient<TRequest> client,
        Func<IRequestClient<TRequest>, Task<Response<TResponse>>> action, TimeSpan? timeout, TimeSpan? idleTimeout,
        TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(timeProvider);
        cancellationToken.ThrowIfCancellationRequested();

        var trackedActivity = new TrackedActivity(GetTestMethodName(), timeout, idleTimeout, timeProvider);
        bool bodyCompleted = false;

        try
        {
            Response<TResponse> response = await action(client).ConfigureAwait(false);
            await trackedActivity.WaitForCompletionAsync(cancellationToken).ConfigureAwait(false);
            bodyCompleted = true;
            return response;
        }
        catch
        {
            trackedActivity.StopWaiting();
            throw;
        }
        finally
        {
            DisposeTracker(trackedActivity, bodyCompleted);
        }
    }

    /// <summary>
    /// Executes a request and waits until the complete request activity becomes idle before returning one of its responses.
    /// </summary>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse1">The first response message type.</typeparam>
    /// <typeparam name="TResponse2">The second response message type.</typeparam>
    /// <param name="client">The request client used by the operation.</param>
    /// <param name="action">The request operation to execute.</param>
    /// <param name="timeout">The maximum time spent observing activity.</param>
    /// <param name="idleTimeout">The required period without active spans.</param>
    /// <param name="cancellationToken">The token checked before the action and used to cancel its subsequent activity wait.</param>
    /// <returns>The response produced by <paramref name="action" />.</returns>
    public static Task<Response<TResponse1, TResponse2>> ExecuteAndWaitForIdleAsync<TRequest, TResponse1, TResponse2>(
        this IRequestClient<TRequest> client, Func<IRequestClient<TRequest>, Task<Response<TResponse1, TResponse2>>> action,
        TimeSpan? timeout = null, TimeSpan? idleTimeout = null, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse1 : class
        where TResponse2 : class
    {
        return ExecuteAndWaitForIdleAsync(client, action, timeout, idleTimeout, TimeProvider.System, cancellationToken);
    }

    /// <summary>
    /// Executes a request and waits, using the supplied time source, until the complete request activity becomes idle before
    /// returning one of its responses.
    /// </summary>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse1">The first response message type.</typeparam>
    /// <typeparam name="TResponse2">The second response message type.</typeparam>
    /// <param name="client">The request client used by the operation.</param>
    /// <param name="action">The request operation to execute.</param>
    /// <param name="timeout">The maximum time spent observing activity.</param>
    /// <param name="idleTimeout">The required period without active spans.</param>
    /// <param name="timeProvider">The time source used for timeout measurement.</param>
    /// <param name="cancellationToken">The token checked before the action and used to cancel its subsequent activity wait.</param>
    /// <returns>The response produced by <paramref name="action" />.</returns>
    public static async Task<Response<TResponse1, TResponse2>> ExecuteAndWaitForIdleAsync<TRequest, TResponse1, TResponse2>(
        this IRequestClient<TRequest> client, Func<IRequestClient<TRequest>, Task<Response<TResponse1, TResponse2>>> action,
        TimeSpan? timeout, TimeSpan? idleTimeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse1 : class
        where TResponse2 : class
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(timeProvider);
        cancellationToken.ThrowIfCancellationRequested();

        var trackedActivity = new TrackedActivity(GetTestMethodName(), timeout, idleTimeout, timeProvider);
        bool bodyCompleted = false;

        try
        {
            Response<TResponse1, TResponse2> response = await action(client).ConfigureAwait(false);
            await trackedActivity.WaitForCompletionAsync(cancellationToken).ConfigureAwait(false);
            bodyCompleted = true;
            return response;
        }
        catch
        {
            trackedActivity.StopWaiting();
            throw;
        }
        finally
        {
            DisposeTracker(trackedActivity, bodyCompleted);
        }
    }

    /// <summary>
    /// Executes a request and waits until the complete request activity becomes idle before returning one of its responses.
    /// </summary>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse1">The first response message type.</typeparam>
    /// <typeparam name="TResponse2">The second response message type.</typeparam>
    /// <typeparam name="TResponse3">The third response message type.</typeparam>
    /// <param name="client">The request client used by the operation.</param>
    /// <param name="action">The request operation to execute.</param>
    /// <param name="timeout">The maximum time spent observing activity.</param>
    /// <param name="idleTimeout">The required period without active spans.</param>
    /// <param name="cancellationToken">The token checked before the action and used to cancel its subsequent activity wait.</param>
    /// <returns>The response produced by <paramref name="action" />.</returns>
    public static Task<Response<TResponse1, TResponse2, TResponse3>> ExecuteAndWaitForIdleAsync<TRequest, TResponse1, TResponse2, TResponse3>(
        this IRequestClient<TRequest> client, Func<IRequestClient<TRequest>, Task<Response<TResponse1, TResponse2, TResponse3>>> action,
        TimeSpan? timeout = null, TimeSpan? idleTimeout = null, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse1 : class
        where TResponse2 : class
        where TResponse3 : class
    {
        return ExecuteAndWaitForIdleAsync(client, action, timeout, idleTimeout, TimeProvider.System, cancellationToken);
    }

    /// <summary>
    /// Executes a request and waits, using the supplied time source, until the complete request activity becomes idle before
    /// returning one of its responses.
    /// </summary>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse1">The first response message type.</typeparam>
    /// <typeparam name="TResponse2">The second response message type.</typeparam>
    /// <typeparam name="TResponse3">The third response message type.</typeparam>
    /// <param name="client">The request client used by the operation.</param>
    /// <param name="action">The request operation to execute.</param>
    /// <param name="timeout">The maximum time spent observing activity.</param>
    /// <param name="idleTimeout">The required period without active spans.</param>
    /// <param name="timeProvider">The time source used for timeout measurement.</param>
    /// <param name="cancellationToken">The token checked before the action and used to cancel its subsequent activity wait.</param>
    /// <returns>The response produced by <paramref name="action" />.</returns>
    public static async Task<Response<TResponse1, TResponse2, TResponse3>> ExecuteAndWaitForIdleAsync<TRequest, TResponse1, TResponse2, TResponse3>(
        this IRequestClient<TRequest> client, Func<IRequestClient<TRequest>, Task<Response<TResponse1, TResponse2, TResponse3>>> action,
        TimeSpan? timeout, TimeSpan? idleTimeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse1 : class
        where TResponse2 : class
        where TResponse3 : class
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(timeProvider);
        cancellationToken.ThrowIfCancellationRequested();

        var trackedActivity = new TrackedActivity(GetTestMethodName(), timeout, idleTimeout, timeProvider);
        bool bodyCompleted = false;

        try
        {
            Response<TResponse1, TResponse2, TResponse3> response = await action(client).ConfigureAwait(false);
            await trackedActivity.WaitForCompletionAsync(cancellationToken).ConfigureAwait(false);
            bodyCompleted = true;
            return response;
        }
        catch
        {
            trackedActivity.StopWaiting();
            throw;
        }
        finally
        {
            DisposeTracker(trackedActivity, bodyCompleted);
        }
    }

    static void DisposeTracker(TrackedActivity tracker, bool bodyCompleted)
    {
        if (bodyCompleted)
        {
            tracker.Dispose();
            return;
        }

        try
        {
            tracker.Dispose();
        }
        catch (Exception)
        {
            // Cleanup cannot replace the action or wait failure already propagating.
        }
    }

    static string? GetTestMethodName()
    {
        var stackTrace = new StackTrace(2);
        for (var index = 0; index < stackTrace.FrameCount; index++)
        {
            var method = stackTrace.GetFrame(index)?.GetMethod();
            if (method == null)
                continue;

            if (method.GetCustomAttributes(false).Any(attribute =>
                {
                    string name = attribute.GetType().Name;
                    return name.Contains("Test", StringComparison.OrdinalIgnoreCase)
                        || name.Contains("Fact", StringComparison.OrdinalIgnoreCase);
                }))
                return method.Name;
        }

        return null;
    }
}
