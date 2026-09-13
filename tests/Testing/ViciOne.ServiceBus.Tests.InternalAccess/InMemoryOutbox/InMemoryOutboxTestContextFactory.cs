using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
/// <summary>
/// Creates the minimal real consume-context boundary needed by in-memory-outbox tests. The factory
/// supplies transport plumbing only; product checkpoints, rollback decisions and assertions remain
/// owned by the source-mirrored executable tests.
/// </summary>
public static class InMemoryOutboxTestContextFactory
{
    public static ConsumeContext<T> Create<T>(
        T message,
        CancellationToken cancellationToken = default,
        IMessageScheduler? scheduler = null,
        OutgoingMessageRecorder? outgoingMessages = null,
        DateTimeOffset? sentTime = null,
        ulong? transportSequenceNumber = null,
        Guid? messageId = null,
        bool isDelivered = false,
        SerializerContext? serializerContext = null,
        Uri? responseAddress = null,
        Guid? requestId = null,
        IServiceProvider? serviceProvider = null,
        Uri? sourceAddress = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);

        ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, ReceiveContextProxy>();
        ((ReceiveContextProxy)(object)receiveContext).Configure(
            new Uri("loopback://localhost/in-memory-outbox-test"),
            cancellationToken,
            outgoingMessages,
            transportSequenceNumber,
            isDelivered);
        serializerContext ??= DispatchProxy.Create<SerializerContext, UnsupportedInvocationProxy>();
        TestConsumeContext<T> consumeContext = DispatchProxy.Create<TestConsumeContext<T>, ConsumeContextProxy>();
        ((ConsumeContextProxy)(object)consumeContext).Configure(
            message,
            receiveContext,
            serializerContext,
            cancellationToken,
            scheduler,
            sentTime ?? DateTimeOffset.UnixEpoch,
            messageId,
            responseAddress,
            requestId,
            serviceProvider,
            sourceAddress);
        return consumeContext;
    }

    private interface TestConsumeContext<out T> :
        ConsumeContext<T>,
        ConsumeContext
        where T : class;

    private class ConsumeContextProxy : DispatchProxy
    {
        private readonly List<Task> _consumeTasks = [];
        private readonly Dictionary<Type, object> _payloads = [];
        private CancellationToken _cancellationToken;
        private object _message = null!;
        private Guid _messageId;
        private ReceiveContext _receiveContext = null!;
        private Uri? _responseAddress;
        private Uri? _sourceAddress;
        private Guid? _requestId;
        private SerializerContext _serializerContext = null!;
        private DateTimeOffset _sentTime;

        public void Configure<T>(
            T message,
            ReceiveContext receiveContext,
            SerializerContext serializerContext,
            CancellationToken cancellationToken,
            IMessageScheduler? scheduler,
            DateTimeOffset sentTime,
            Guid? messageId,
            Uri? responseAddress,
            Guid? requestId,
            IServiceProvider? serviceProvider,
            Uri? sourceAddress)
            where T : class
        {
            _message = message;
            _messageId = messageId ?? NewId.NextGuid();
            _receiveContext = receiveContext;
            _serializerContext = serializerContext;
            _cancellationToken = cancellationToken;
            _sentTime = sentTime;
            _responseAddress = responseAddress;
            _requestId = requestId;
            _sourceAddress = sourceAddress;
            if (serviceProvider is not null)
                _payloads.Add(typeof(IServiceProvider), serviceProvider);

            if (scheduler is not null)
            {
                MessageSchedulerContext schedulerContext = DispatchProxy.Create<MessageSchedulerContext, MessageSchedulerContextProxy>();
                ((MessageSchedulerContextProxy)(object)schedulerContext).Scheduler = scheduler;
                _payloads.Add(typeof(MessageSchedulerContext), schedulerContext);
            }
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            switch (targetMethod.Name)
            {
                case "get_Message":
                    return _message;
                case "get_MessageId":
                    return _messageId;
                case "get_SentTime":
                    return _sentTime;
                case "get_ResponseAddress":
                    return _responseAddress;
                case "get_SourceAddress":
                    return _sourceAddress;
                case "get_RequestId":
                    return _requestId;
                case "get_ReceiveContext":
                    return _receiveContext;
                case "get_SerializerContext":
                    return _serializerContext;
                case "get_CancellationToken":
                    return _cancellationToken;
                case "get_Headers":
                    return EmptyHeaders.Instance;
                case "get_Host":
                    return HostMetadataCache.Host;
                case "get_ConsumeCompleted":
                    return Task.WhenAll(_consumeTasks);
                case "AddConsumeTask":
                    _consumeTasks.Add((Task)args![0]!);
                    return null;
                case "HasPayloadType":
                    return _payloads.ContainsKey((Type)args![0]!);
                case "TryGetPayload":
                    {
                        Type payloadType = targetMethod.GetGenericArguments()[0];
                        bool found = _payloads.TryGetValue(payloadType, out object? payload);
                        args![0] = payload;
                        return found;
                    }
                case "GetOrAddPayload":
                    {
                        Type payloadType = targetMethod.GetGenericArguments()[0];
                        if (_payloads.TryGetValue(payloadType, out object? payload))
                            return payload;

                        var add = (Delegate)args![0]!;
                        payload = add.DynamicInvoke()!;
                        _payloads.Add(payloadType, payload);
                        return payload;
                    }
                case "AddOrUpdatePayload":
                    {
                        Type payloadType = targetMethod.GetGenericArguments()[0];
                        var add = (Delegate)args![0]!;
                        var update = (Delegate)args[1]!;
                        object payload = _payloads.TryGetValue(payloadType, out object? existing)
                            ? update.DynamicInvoke(existing)!
                            : add.DynamicInvoke()!;
                        _payloads[payloadType] = payload;
                        return payload;
                    }
                default:
                    throw new NotSupportedException(targetMethod.Name);
            }
        }
    }

    private class ReceiveContextProxy : DispatchProxy
    {
        private IPublishEndpointProvider _publishEndpointProvider = null!;
        private ISendEndpointProvider _sendEndpointProvider = null!;
        private CancellationToken _cancellationToken;
        private Uri _inputAddress = null!;
        private bool _isDelivered;
        private ITransportSequenceNumber? _transportSequenceNumber;

        public void Configure(
            Uri inputAddress,
            CancellationToken cancellationToken,
            OutgoingMessageRecorder? outgoingMessages,
            ulong? transportSequenceNumber,
            bool isDelivered)
        {
            _inputAddress = inputAddress;
            _cancellationToken = cancellationToken;
            _isDelivered = isDelivered;
            _transportSequenceNumber = transportSequenceNumber.HasValue
                ? new TransportSequenceNumber(transportSequenceNumber.Value)
                : null;

            if (outgoingMessages is null)
            {
                _publishEndpointProvider = DispatchProxy.Create<IPublishEndpointProvider, UnsupportedInvocationProxy>();
                _sendEndpointProvider = DispatchProxy.Create<ISendEndpointProvider, UnsupportedInvocationProxy>();
            }
            else
            {
                var provider = new RecordingEndpointProvider(outgoingMessages);
                _publishEndpointProvider = provider;
                _sendEndpointProvider = provider;
            }
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            return targetMethod.Name switch
            {
                "get_InputAddress" => _inputAddress,
                "get_CancellationToken" => _cancellationToken,
                "get_IsDelivered" => _isDelivered,
                "get_PublishEndpointProvider" => _publishEndpointProvider,
                "get_SendEndpointProvider" => _sendEndpointProvider,
                "HasPayloadType" => _transportSequenceNumber != null
                    && (Type)args![0]! == typeof(ITransportSequenceNumber),
                "TryGetPayload" => TryGetPayload(targetMethod, args),
                _ => throw new NotSupportedException(targetMethod.Name),
            };
        }

        private bool TryGetPayload(MethodInfo targetMethod, object?[]? args)
        {
            bool found = _transportSequenceNumber != null
                && targetMethod.GetGenericArguments()[0] == typeof(ITransportSequenceNumber);
            args![0] = found ? _transportSequenceNumber : null;
            return found;
        }
    }

    private sealed record TransportSequenceNumber(ulong? SequenceNumber) : ITransportSequenceNumber;

    private class MessageSchedulerContextProxy : DispatchProxy
    {
        public required IMessageScheduler Scheduler { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_SchedulerFactory" => new MessageSchedulerFactory(_ => Scheduler),
            _ => throw new NotSupportedException(targetMethod?.Name),
        };
    }

    private sealed class RecordingEndpointProvider(OutgoingMessageRecorder recorder) :
        IPublishEndpointProvider,
        ISendEndpointProvider
    {
        private readonly ISendEndpoint _endpoint = new RecordingSendEndpoint(recorder);

        public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
            where T : class =>
            cancellationToken.IsCancellationRequested
                ? Task.FromCanceled<ISendEndpoint>(cancellationToken)
                : Task.FromResult(_endpoint);

        public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(address);
            return cancellationToken.IsCancellationRequested
                ? Task.FromCanceled<ISendEndpoint>(cancellationToken)
                : Task.FromResult(_endpoint);
        }

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) =>
            throw new NotSupportedException();

        public ConnectHandle ConnectSendObserver(ISendObserver observer) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingSendEndpoint(OutgoingMessageRecorder recorder) : IAdvancedSendEndpoint
    {
        public Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class
            => RecordAsync(message, cancellationToken);

        public Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(pipe);
            return RecordAsync(message, pipe, cancellationToken);
        }

        public Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(pipe);
            return RecordAsync(message, pipe, cancellationToken);
        }

        public Task SendAsync(object message, CancellationToken cancellationToken = default) =>
            RecordAsync(message, cancellationToken);

        public Task SendAsync(object message, Type messageType, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(messageType);
            return RecordAsync(message, cancellationToken);
        }

        public Task SendAsync(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(pipe);
            return RecordAsync(message, cancellationToken);
        }

        public Task SendAsync(
            object message,
            Type messageType,
            IPipe<SendContext> pipe,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(messageType);
            ArgumentNullException.ThrowIfNull(pipe);
            return RecordAsync(message, cancellationToken);
        }

        public Task SendAsync<T>(object values, CancellationToken cancellationToken = default)
            where T : class =>
            RecordAsync(values, cancellationToken);

        public Task SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(pipe);
            return RecordAsync(values, cancellationToken);
        }

        public Task SendAsync<T>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(pipe);
            return RecordAsync(values, cancellationToken);
        }

        public ConnectHandle ConnectSendObserver(ISendObserver observer) =>
            throw new NotSupportedException();

        private Task RecordAsync(object message, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(message);
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            recorder.Add(message);
            return Task.CompletedTask;
        }

        private async Task RecordAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(message);
            cancellationToken.ThrowIfCancellationRequested();
            var context = new MessageSendContext<T>(message, cancellationToken);

            await pipe.SendAsync(context).ConfigureAwait(false);

            recorder.Add(message, context);
        }

        private async Task RecordAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(message);
            cancellationToken.ThrowIfCancellationRequested();
            var context = new MessageSendContext<T>(message, cancellationToken);

            await pipe.SendAsync(context).ConfigureAwait(false);

            recorder.Add(message, context);
        }
    }

    private class UnsupportedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }
}

/// <summary>Records messages emitted while a state-machine test executes its outgoing activities.</summary>
public sealed class OutgoingMessageRecorder
{
    private readonly List<object> _messages = [];
    private readonly List<SendObservation> _sendObservations = [];
    private readonly Action<object>? _beforeAdd;

    public OutgoingMessageRecorder(Action<object>? beforeAdd = null)
    {
        _beforeAdd = beforeAdd;
    }

    /// <summary>Gets recorded messages in emission order.</summary>
    public IReadOnlyList<object> Messages => _messages;

    /// <summary>Gets metadata captured after typed send pipes have been applied.</summary>
    public IReadOnlyList<SendObservation> SendObservations => _sendObservations;

    internal void Add(object message)
    {
        _beforeAdd?.Invoke(message);
        _messages.Add(message);
    }

    internal void Add(object message, SendContext context)
    {
        Add(message);
        _sendObservations.Add(new SendObservation(
            message,
            context.RequestId,
            context.ResponseAddress,
            context.FaultAddress));
    }

    /// <summary>Captures request, response, and fault routing metadata applied by a send pipe.</summary>
    public sealed record SendObservation(
        object Message,
        Guid? RequestId,
        Uri? ResponseAddress,
        Uri? FaultAddress);
}
