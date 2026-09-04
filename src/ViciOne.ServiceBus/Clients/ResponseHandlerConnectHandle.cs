using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Clients;

/// <summary>
/// A connection to a request which handles a result, and completes the Task when it's received
/// </summary>
/// <typeparam name="TResponse"></typeparam>
public class ResponseHandlerConnectHandle<TResponse> :
    HandlerConnectHandle<TResponse>
    where TResponse : class
{
    readonly TaskCompletionSource<ConsumeContext<TResponse>> _completed;
    readonly ConnectHandle _handle;
    readonly Task _requestTask;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handle">The handle value.</param>
    /// <param name="completed">The completed value.</param>
    /// <param name="requestTask">The request task value.</param>
    public ResponseHandlerConnectHandle(ConnectHandle handle, TaskCompletionSource<ConsumeContext<TResponse>> completed, Task requestTask)
    {
        _handle = handle;
        _completed = completed;
        _requestTask = requestTask;

        Task = GetTaskAsync();
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    public void Dispose()
    {
        _handle.Dispose();
    }

    /// <summary>
    /// Performs the disconnect operation.
    /// </summary>
    public void Disconnect()
    {
        _handle.Disconnect();
    }

    /// <summary>
    /// Performs the try set exception operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    public void TrySetException(Exception exception)
    {
        _completed.TrySetException(exception);
        _completed.Task.IgnoreUnobservedExceptions();
    }

    /// <summary>
    /// Performs the try set canceled operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public void TrySetCanceled(CancellationToken cancellationToken)
    {
        _completed.TrySetCanceled(cancellationToken);
        _completed.Task.IgnoreUnobservedExceptions();
    }

    /// <summary>
    /// Gets the task value.
    /// </summary>
    public Task<Response<TResponse>> Task { get; }

    async Task<Response<TResponse>> GetTaskAsync()
    {
        await _requestTask.ConfigureAwait(false);

        ConsumeContext<TResponse> context = await _completed.Task.ConfigureAwait(false);

        return new MessageResponse<TResponse>(context);
    }
}
