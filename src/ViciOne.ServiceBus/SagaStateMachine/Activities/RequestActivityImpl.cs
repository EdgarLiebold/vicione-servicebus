using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a request activity impl implementation.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
/// <typeparam name="TRequest">The t request type.</typeparam>
/// <typeparam name="TResponse">The t response type.</typeparam>
public abstract class RequestActivityImpl<TInstance, TRequest, TResponse>
    where TInstance : class, SagaStateMachineInstance
    where TRequest : class
    where TResponse : class
{
    readonly Request<TInstance, TRequest, TResponse> _request;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="request">The request value.</param>
    protected RequestActivityImpl(Request<TInstance, TRequest, TResponse> request)
    {
        _request = request;
    }

    /// <summary>
    /// Sends request.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="sendTuple">The send tuple value.</param>
    /// <param name="serviceAddress">The service address value.</param>
    /// <returns>The result of the operation.</returns>
    protected async Task SendRequestAsync(BehaviorContext<TInstance> context, global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TRequest> sendTuple, Uri serviceAddress)
    {
        var requestId = _request.GenerateRequestId(context.Saga);

        var pipe = new SendRequestPipe(_request, context.ReceiveContext.InputAddress, requestId, sendTuple.Pipe);

        var endpoint = serviceAddress != null
            ? await context.GetSendEndpointAsync(serviceAddress).ConfigureAwait(false)
            : await context.ReceiveContext.PublishEndpointProvider.GetPublishEndpointAsync<TRequest>(context, null);

        await endpoint.SendAsync(sendTuple.Message, pipe, context.CancellationToken).ConfigureAwait(false);

        _request.SetRequestId(context.Saga, requestId);

        if (_request.Settings.Timeout > TimeSpan.Zero)
        {
            var now = context.GetTimeProvider().GetUtcNow().UtcDateTime;
            var expirationTime = now + _request.Settings.Timeout;

            RequestTimeoutExpired<TRequest> message =
                new TimeoutExpired<TRequest>(now, expirationTime, context.Saga.CorrelationId, pipe.RequestId, sendTuple.Message);

            if (context.TryGetPayload(out MessageSchedulerContext? schedulerContext))
                await schedulerContext.ScheduleSendAsync(expirationTime, message, context.CancellationToken).ConfigureAwait(false);
            else
                throw new ConfigurationException("A request timeout was specified but no message scheduler was specified or available");
        }
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public virtual void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("request");
        scope.Add("requestType", TypeCache<TRequest>.ShortName);
        scope.Add("responseType", TypeCache<TResponse>.ShortName);
        scope.Set(_request.Settings);
    }


    /// <summary>
    /// Handles the sending of a request to the endpoint specified
    /// </summary>
    class SendRequestPipe :
        IPipe<SendContext<TRequest>>
    {
        readonly IPipe<SendContext<TRequest>> _pipe = null!;
        readonly Request<TInstance, TRequest, TResponse> _request;
        readonly Uri _responseAddress;

        public SendRequestPipe(Request<TInstance, TRequest, TResponse> request, Uri responseAddress, Guid requestId, IPipe<SendContext<TRequest>> pipe)
        {
            _request = request;
            _responseAddress = responseAddress;

            RequestId = requestId;

            if (pipe.IsNotEmpty())
                _pipe = pipe;
        }

        public Guid RequestId { get; }

        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(SendContext<TRequest> context)
        {
            context.RequestId = RequestId;
            context.ResponseAddress = _responseAddress;

            _request.SetSendContextHeaders(context);

            return _pipe != null
                ? _pipe.SendAsync(context)
                : Task.CompletedTask;
        }
    }


    class TimeoutExpired<T> :
        RequestTimeoutExpired<T>
        where T : class
    {
        public TimeoutExpired(DateTimeOffset timestamp, DateTimeOffset expirationTime, Guid correlationId, Guid requestId, T message)
        {
            Timestamp = timestamp;
            ExpirationTime = expirationTime;
            CorrelationId = correlationId;
            RequestId = requestId;
            Message = message;
        }

        public DateTimeOffset Timestamp { get; }

        public DateTimeOffset ExpirationTime { get; }

        public Guid CorrelationId { get; }

        public Guid RequestId { get; }

        public T Message { get; }
    }
}
