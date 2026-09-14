using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Clients.Requests;

internal sealed partial class ClientRequestHandle<TRequest>
    where TRequest : class
{
    void Fail(Fault message)
    {
        Fail(new RequestFaultException(typeof(TRequest), message));
    }

    void Fail(Exception responseException, Exception? messageException = null)
    {
        lock (_handlerLock)
        {
            if (_faultedOrCanceled != 0 || _responseCompleted)
                return;

            _faultedOrCanceled = 1;
        }

        void HandleFail()
        {
            try
            {
                DisposeRegistration();

                DisposeTimer();

                _readyToSend.TrySetException(responseException);

                var wasSet = _sendContext.TrySetException(responseException);

                _message.TrySetException(messageException ?? responseException);
                _message.Task.IgnoreUnobservedExceptions();

                DisconnectHandlers(handle => handle.TrySetException(responseException));

                if (wasSet)
                    CancelRequestSend();
            }
            finally
            {
                _terminalCleanupCompleted.TrySetResult();
            }
        }

        Task.Factory.StartNew(HandleFail, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
    }

    void CancelAndDispose()
    {
        try
        {
            DisposeRegistration();

            DisposeTimer();

            CancelRequestSend();

            var cancellationToken = _cancellationToken.IsCancellationRequested ? _cancellationToken : _requestSendCancellationToken;

            CompleteCancellationSignals(cancellationToken);

            DisconnectHandlers(handle => handle.TrySetCanceled(cancellationToken));
        }
        finally
        {
            _terminalCleanupCompleted.TrySetResult();
        }
    }

    void CompleteCancellationSignals(CancellationToken cancellationToken)
    {
        _readyToSend.TrySetCanceled(cancellationToken);
        _sendContext.TrySetCanceled(cancellationToken);
        _message.TrySetCanceled(cancellationToken);

        HandlerConnectHandle[] responseHandlers;
        lock (_handlerLock)
            responseHandlers = _responseHandlers.Values.ToArray();

        foreach (HandlerConnectHandle handle in responseHandlers)
            TryCleanup(() => handle.TrySetCanceled(cancellationToken), "Completing a canceled request response handler faulted");
    }

    void TimeoutExpired(object? state)
    {
        var timeoutException = new RequestTimeoutException(RequestId);

        Fail(timeoutException);
    }

    void DisposeTimer()
    {
        ITimer? timer = Interlocked.Exchange(ref _timeoutTimer, null);
        DisposeTimerSafely(timer);
    }

    CancellationToken CancellationTokenForCanceledRequest()
    {
        if (_cancellationToken.IsCancellationRequested)
            return _cancellationToken;
        if (_requestSendCancellationToken.IsCancellationRequested)
            return _requestSendCancellationToken;

        return new CancellationToken(canceled: true);
    }

    void DisconnectHandlers(Action<HandlerConnectHandle> complete)
    {
        HandlerConnectHandle[] responseHandlers;
        ConnectHandle? faultHandler;
        lock (_handlerLock)
        {
            responseHandlers = _responseHandlers.Values.ToArray();
            _responseHandlers.Clear();
            faultHandler = _faultHandler;
            _faultHandler = null;
        }

        foreach (HandlerConnectHandle handle in responseHandlers)
        {
            TryCleanup(() => complete(handle), "Completing a request response handler faulted");
            TryCleanup(handle.Disconnect, "Disconnecting a request response handler faulted");
        }

        if (faultHandler is not null)
            TryCleanup(faultHandler.Disconnect, "Disconnecting the request fault handler faulted");
    }

    void CancelRequestSend()
    {
        TryCleanup(_cancellationTokenSource.Cancel, "Canceling the request send faulted");
    }

    async Task DisposeCancellationTokenSourceAfterTerminalCleanupAsync()
    {
        await _terminalCleanupCompleted.Task.ConfigureAwait(false);

        try
        {
            await _send.ConfigureAwait(false);
        }
        catch
        {
            // Send failures are propagated through the request completion tasks.
        }

        _cancellationTokenSource.Dispose();
    }

    void DisposeRegistration()
    {
        TryCleanup(_registration.Dispose, "Disposing the request cancellation registration faulted");
    }

    static void DisposeTimerSafely(ITimer? timer)
    {
        if (timer is null)
            return;

        TryCleanup(timer.Dispose, "Disposing the request timeout timer faulted");
    }

    static void TryCleanup(Action cleanup, string message)
    {
        try
        {
            cleanup();
        }
        catch (Exception exception)
        {
            try
            {
                LogContext.Warning?.Log(exception, message);
            }
            catch
            {
                // Diagnostic logging cannot change request completion or cleanup outcomes.
            }
        }
    }
}
