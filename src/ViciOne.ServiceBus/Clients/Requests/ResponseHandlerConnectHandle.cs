using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Clients.Requests;

/// <summary>Owns a response-handler connection and combines its terminal outcome with request-send completion.</summary>
/// <typeparam name="TResponse">The response message contract.</typeparam>
internal sealed class ResponseHandlerConnectHandle<TResponse> :
    HandlerConnectHandle<TResponse>
    where TResponse : class
{
    readonly TaskCompletionSource<ConsumeContext<TResponse>> _completed;
    readonly ConnectHandle _handle;
    readonly Task _requestTask;

    /// <summary>Creates a response handle for one connected response pipeline.</summary>
    /// <param name="handle">The response-pipeline connection.</param>
    /// <param name="completed">The matching response context completion source.</param>
    /// <param name="requestTask">The task that sends the associated request.</param>
    public ResponseHandlerConnectHandle(ConnectHandle handle, TaskCompletionSource<ConsumeContext<TResponse>> completed, Task requestTask)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
        _completed = completed ?? throw new ArgumentNullException(nameof(completed));
        _requestTask = requestTask ?? throw new ArgumentNullException(nameof(requestTask));

        Task = GetTaskAsync();
    }

    /// <summary>Disposes the response-pipeline connection.</summary>
    public void Dispose()
    {
        _handle.Dispose();
    }

    /// <summary>Disconnects the response pipeline.</summary>
    public void Disconnect()
    {
        _handle.Disconnect();
    }

    /// <summary>Attempts to complete response waiting with a request failure.</summary>
    /// <param name="exception">The request failure.</param>
    public void TrySetException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _completed.TrySetException(exception);
        _completed.Task.IgnoreUnobservedExceptions();
    }

    /// <summary>Attempts to cancel response waiting with the originating token.</summary>
    /// <param name="cancellationToken">The token that canceled the request.</param>
    public void TrySetCanceled(CancellationToken cancellationToken)
    {
        _completed.TrySetCanceled(cancellationToken);
        _completed.Task.IgnoreUnobservedExceptions();
    }

    /// <summary>Gets the task that validates send completion and returns the matching response.</summary>
    public Task<Response<TResponse>> Task { get; }

    async Task<Response<TResponse>> GetTaskAsync()
    {
        if (!_completed.Task.IsCompleted && !_requestTask.IsCompleted)
            await System.Threading.Tasks.Task.WhenAny(_completed.Task, _requestTask).ConfigureAwait(false);

        if (_completed.Task is { IsCompleted: true, IsCompletedSuccessfully: false })
            await _completed.Task.ConfigureAwait(false);

        await _requestTask.ConfigureAwait(false);

        ConsumeContext<TResponse> context = await _completed.Task.ConfigureAwait(false);

        return new MessageResponse<TResponse>(context);
    }
}
