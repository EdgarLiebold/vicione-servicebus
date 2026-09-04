using System.Reflection;

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
        IMessageScheduler? scheduler = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);

        ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, ReceiveContextProxy>();
        ((ReceiveContextProxy)(object)receiveContext).Configure(
            new Uri("loopback://localhost/in-memory-outbox-test"),
            cancellationToken);
        SerializerContext serializerContext = DispatchProxy.Create<SerializerContext, UnsupportedInvocationProxy>();
        TestConsumeContext<T> consumeContext = DispatchProxy.Create<TestConsumeContext<T>, ConsumeContextProxy>();
        ((ConsumeContextProxy)(object)consumeContext).Configure(
            message,
            receiveContext,
            serializerContext,
            cancellationToken,
            scheduler);
        return consumeContext;
    }

    private interface TestConsumeContext<out T> :
        ConsumeContext<T>,
        ConsumeContext
        where T : class;

    private class ConsumeContextProxy : DispatchProxy
    {
        private readonly Dictionary<Type, object> _payloads = [];
        private CancellationToken _cancellationToken;
        private object _message = null!;
        private ReceiveContext _receiveContext = null!;
        private SerializerContext _serializerContext = null!;

        public void Configure<T>(
            T message,
            ReceiveContext receiveContext,
            SerializerContext serializerContext,
            CancellationToken cancellationToken,
            IMessageScheduler? scheduler)
            where T : class
        {
            _message = message;
            _receiveContext = receiveContext;
            _serializerContext = serializerContext;
            _cancellationToken = cancellationToken;
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
                case "get_ReceiveContext":
                    return _receiveContext;
                case "get_SerializerContext":
                    return _serializerContext;
                case "get_CancellationToken":
                    return _cancellationToken;
                case "get_ConsumeCompleted":
                    return Task.CompletedTask;
                case "HasPayloadType":
                    return _payloads.ContainsKey((Type)args![0]!);
                case "TryGetPayload":
                    {
                        Type payloadType = targetMethod.GetGenericArguments()[0];
                        bool found = _payloads.TryGetValue(payloadType, out object? payload);
                        args![0] = payload;
                        return found;
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
        private readonly IPublishEndpointProvider _publishEndpointProvider =
            DispatchProxy.Create<IPublishEndpointProvider, UnsupportedInvocationProxy>();
        private readonly ISendEndpointProvider _sendEndpointProvider =
            DispatchProxy.Create<ISendEndpointProvider, UnsupportedInvocationProxy>();
        private CancellationToken _cancellationToken;
        private Uri _inputAddress = null!;

        public void Configure(Uri inputAddress, CancellationToken cancellationToken)
        {
            _inputAddress = inputAddress;
            _cancellationToken = cancellationToken;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_InputAddress" => _inputAddress,
            "get_CancellationToken" => _cancellationToken,
            "get_PublishEndpointProvider" => _publishEndpointProvider,
            "get_SendEndpointProvider" => _sendEndpointProvider,
            _ => throw new NotSupportedException(targetMethod?.Name),
        };
    }

    private class MessageSchedulerContextProxy : DispatchProxy
    {
        public required IMessageScheduler Scheduler { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_SchedulerFactory" => new MessageSchedulerFactory(_ => Scheduler),
            _ => throw new NotSupportedException(targetMethod?.Name),
        };
    }

    private class UnsupportedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }
}
