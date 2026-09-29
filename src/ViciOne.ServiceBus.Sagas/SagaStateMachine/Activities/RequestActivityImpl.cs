using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Sends a state-machine request and schedules its configured timeout message.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public abstract class RequestActivityImpl<TInstance, TRequest, TResponse>
    where TInstance : class, ISagaStateMachineInstance
    where TRequest : class
    where TResponse : class
{
    readonly IRequest<TInstance, TRequest, TResponse> _request;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="request">The request.</param>
    protected RequestActivityImpl(IRequest<TInstance, TRequest, TResponse> request)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
    }

    /// <summary>Sends request.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="sendTuple">The send tuple.</param>
    /// <param name="serviceAddress">The service address.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected async Task SendRequestAsync(IBehaviorContext<TInstance> context, global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TRequest> sendTuple, Uri serviceAddress)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CancellationToken.ThrowIfCancellationRequested();

        var timeout = _request.Settings.Timeout;
        MessageSchedulerContext? schedulerContext = null;
        if (timeout > TimeSpan.Zero && !context.TryGetPayload(out schedulerContext))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Saga", "unknown", "A request timeout was specified but no message scheduler was specified or available", "Correct the named configuration before starting the host"));

        if (timeout > TimeSpan.Zero && schedulerContext is not global::ViciOne.ServiceBus.Advanced.IScheduleCancellationCapability
            { CancellationMode: global::ViciOne.ServiceBus.Advanced.ScheduleCancellationMode.CallerSpecifiedToken })
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Saga", "unknown", "A request timeout requires a message scheduler that can cancel using the caller-specified scheduling token", "Configure a cancellable scheduler before starting the host"));

        if (timeout > TimeSpan.Zero)
        {
            var admissionTime = context.GetTimeProvider().GetUtcNow().UtcDateTime;
            if (timeout > DateTime.MaxValue - admissionTime)
                throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Saga", "unknown", "The request timeout exceeds the supported date range", "Correct the named configuration before starting the host"));
        }

        var requestId = _request.GenerateRequestId(context.Saga);

        var pipe = new SendRequestPipe(_request, context.ReceiveContext.InputAddress, requestId, sendTuple.Pipe);

        var endpoint = serviceAddress != null
            ? await context.GetSendEndpointAsync(serviceAddress).ConfigureAwait(false)
            : await context.ReceiveContext.PublishEndpointProvider.GetPublishEndpointAsync<TRequest>(context, null);

        await endpoint.SendAsync(sendTuple.Message, pipe, context.CancellationToken).ConfigureAwait(false);

        _request.SetRequestId(context.Saga, requestId);

        if (timeout > TimeSpan.Zero)
        {
            var now = context.GetTimeProvider().GetUtcNow().UtcDateTime;
            var expirationTime = timeout > DateTime.MaxValue - now
                ? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
                : now + timeout;
            IRequestTimeoutExpired<TRequest> message =
                new TimeoutExpired<TRequest>(now, expirationTime, context.Saga.CorrelationId, pipe.RequestId, sendTuple.Message);

            IPipe<SendContext<IRequestTimeoutExpired<TRequest>>> schedulePipe =
                Pipe.Execute<SendContext<IRequestTimeoutExpired<TRequest>>>(sendContext => sendContext.ScheduledMessageId = requestId);
            await schedulerContext!.ScheduleSendAsync(expirationTime, message, schedulePipe, context.CancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public virtual void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("request");
        scope.Add("requestType", TypeCache<TRequest>.ShortName);
        scope.Add("responseType", TypeCache<TResponse>.ShortName);
        scope.Set(_request.Settings);
    }


    /// <summary>Handles the sending of a request to the endpoint specified.</summary>
    class SendRequestPipe :
        IPipe<SendContext<TRequest>>
    {
        readonly IPipe<SendContext<TRequest>> _pipe = null!;
        readonly IRequest<TInstance, TRequest, TResponse> _request;
        readonly Uri _responseAddress;

        public SendRequestPipe(IRequest<TInstance, TRequest, TResponse> request, Uri responseAddress, Guid requestId, IPipe<SendContext<TRequest>> pipe)
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
        IRequestTimeoutExpired<T>
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
