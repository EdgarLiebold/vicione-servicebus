using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Clients.Requests;

internal sealed partial class ClientRequestHandle<TRequest>
    where TRequest : class
{
    async Task SendRequestAsync()
    {
        try
        {
            var message = await _sendRequestCallback(RequestId, this, _requestSendCancellationToken).ConfigureAwait(false);

            _message.TrySetResult(message);
        }
        catch (RequestException exception)
        {
            Fail(exception);

            throw;
        }
        catch (OperationCanceledException exception)
        {
            if (_sendContext.Task.IsFaulted)
                await _sendContext.Task.ConfigureAwait(false);

            var requestException = new RequestCanceledException(RequestId, exception, exception.CancellationToken);

            Fail(requestException);

            throw requestException;
        }
        catch (Exception exception)
        {
            var requestException = new RequestException(
                $"An exception occurred while processing the {typeof(TRequest).Name} request",
                exception);

            Fail(requestException, exception);

            throw requestException;
        }
    }

    Task<Response<TResponse>> ResponseAsync<TResponse>(bool readyToSend)
        where TResponse : class
    {
        lock (_handlerLock)
        {
            if (_faultedOrCanceled != 0 || _cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<Response<TResponse>>(CancellationTokenForCanceledRequest());

            if (_readyToSend.Task.IsCompleted)
                throw new RequestException("Response handlers cannot be registered after the request is ready to send");

            if (typeof(TResponse) == typeof(Fault<TRequest>) || _responseHandlers.ContainsKey(typeof(TResponse)))
                throw new RequestException($"Only one handler of type {TypeCache<TResponse>.ShortName} can be registered");

            var completed = TaskCompletionSources.Create<ConsumeContext<TResponse>>();
            var pipeConfigurator = new PipeConfigurator<ConsumeContext<TResponse>>();

            Task MessageHandlerAsync(ConsumeContext<TResponse> context)
            {
                completed.TrySetResult(context);
                return Task.CompletedTask;
            }

            ConnectHandle connectHandle = _context.ConnectRequestHandler(RequestId, MessageHandlerAsync, pipeConfigurator);
            var handle = new ResponseHandlerConnectHandle<TResponse>(connectHandle, completed, _send);

            _responseHandlers.Add(typeof(TResponse), handle);
            _accept.Add(MessageUrn.ForTypeString<TResponse>());

            if (readyToSend)
                _readyToSend.TrySetResult(true);

            return handle.Task;
        }
    }

    void ConnectFaultHandler()
    {
        Task MessageHandlerAsync(ConsumeContext<Fault<TRequest>> context)
        {
            return HandleFaultAsync(context);
        }

        lock (_handlerLock)
        {
            if (_faultedOrCanceled != 0 || _cancellationToken.IsCancellationRequested)
                return;

            _faultHandler = _context.ConnectRequestHandler(
                RequestId,
                MessageHandlerAsync,
                new PipeConfigurator<ConsumeContext<Fault<TRequest>>>());
        }
    }

    Task HandleFaultAsync(ConsumeContext<Fault<TRequest>> context)
    {
        Fail(context.Message);

        return Task.CompletedTask;
    }
}
