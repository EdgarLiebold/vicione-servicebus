using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Clients;

/// <summary>A connection to a request which handles a result, and completes the Task when it's received.</summary>
/// <typeparam name="TResponse">The response type.</typeparam>
internal sealed class ResponseHandlerConnectHandle<TResponse> :
    HandlerConnectHandle<TResponse>
    where TResponse : class
{
    readonly TaskCompletionSource<ConsumeContext<TResponse>> _completed;
    readonly ConnectHandle _handle;
    readonly Task _requestTask;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="handle">The handle.</param>
    /// <param name="completed">The completed.</param>
    /// <param name="requestTask">The request task.</param>
    public ResponseHandlerConnectHandle(ConnectHandle handle, TaskCompletionSource<ConsumeContext<TResponse>> completed, Task requestTask)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
        _completed = completed ?? throw new ArgumentNullException(nameof(completed));
        _requestTask = requestTask ?? throw new ArgumentNullException(nameof(requestTask));

        Task = GetTaskAsync();
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        _handle.Dispose();
    }

    /// <summary>Disconnects the current observer or endpoint.</summary>
    public void Disconnect()
    {
        _handle.Disconnect();
    }

    /// <summary>Attempts to set exception.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    public void TrySetException(Exception exception)
    {
        _completed.TrySetException(exception);
        _completed.Task.IgnoreUnobservedExceptions();
    }

    /// <summary>Attempts to set canceled.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public void TrySetCanceled(CancellationToken cancellationToken)
    {
        _completed.TrySetCanceled(cancellationToken);
        _completed.Task.IgnoreUnobservedExceptions();
    }

    /// <summary>Gets the task.</summary>
    public Task<Response<TResponse>> Task { get; }

    async Task<Response<TResponse>> GetTaskAsync()
    {
        await _requestTask.ConfigureAwait(false);

        ConsumeContext<TResponse> context = await _completed.Task.ConfigureAwait(false);

        return new MessageResponse<TResponse>(context);
    }
}
