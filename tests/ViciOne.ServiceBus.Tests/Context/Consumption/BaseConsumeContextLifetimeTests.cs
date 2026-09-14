using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Context.Consumption;

public sealed class BaseConsumeContextLifetimeTests
{
    [Theory]
    [InlineData(ResponseShape.Message)]
    [InlineData(ResponseShape.MessageOptions)]
    [InlineData(ResponseShape.MessageTypedPipe)]
    [InlineData(ResponseShape.MessageUntypedPipe)]
    [InlineData(ResponseShape.RuntimeMessage)]
    [InlineData(ResponseShape.RuntimeMessageType)]
    [InlineData(ResponseShape.RuntimeMessagePipe)]
    [InlineData(ResponseShape.RuntimeMessageTypePipe)]
    [InlineData(ResponseShape.Values)]
    [InlineData(ResponseShape.ValuesTypedPipe)]
    [InlineData(ResponseShape.ValuesUntypedPipe)]
    [InlineData(ResponseShape.DeferredMessage)]
    [RequirementCoverage("REQ-VSB-CONSUME-RESPONSE-LIFETIME", "every-response-shape-owns-each-required-task-once")]
    public async Task EveryResponseShape_RegistersEachRequiredTaskExactlyOnceAsync(ResponseShape shape)
    {
        IAdvancedSendEndpoint endpoint = DispatchProxy.Create<IAdvancedSendEndpoint, PendingSendEndpointProxy>();
        var endpointProxy = (PendingSendEndpointProxy)(object)endpoint;
        var context = new RecordingConsumeContext(endpoint);
        var response = new ResponseMessage { Value = "accepted" };
        object values = new { Value = "accepted" };

        Task responseTask = shape switch
        {
            ResponseShape.Message => context.RespondAsync(response),
            ResponseShape.MessageOptions => context.RespondAsync(response, new SendOptions()),
            ResponseShape.MessageTypedPipe => context.RespondAsync(
                response,
                Pipe.Empty<SendContext<ResponseMessage>>()),
            ResponseShape.MessageUntypedPipe => context.RespondAsync(response, Pipe.Empty<SendContext>()),
            ResponseShape.RuntimeMessage => context.RespondAsync((object)response),
            ResponseShape.RuntimeMessageType => context.RespondAsync((object)response, typeof(ResponseMessage)),
            ResponseShape.RuntimeMessagePipe => context.RespondAsync((object)response, Pipe.Empty<SendContext>()),
            ResponseShape.RuntimeMessageTypePipe => context.RespondAsync(
                (object)response,
                typeof(ResponseMessage),
                Pipe.Empty<SendContext>()),
            ResponseShape.Values => context.RespondAsync<ResponseMessage>(values),
            ResponseShape.ValuesTypedPipe => context.RespondAsync<ResponseMessage>(
                values,
                Pipe.Empty<SendContext<ResponseMessage>>()),
            ResponseShape.ValuesUntypedPipe => context.RespondAsync<ResponseMessage>(
                values,
                Pipe.Empty<SendContext>()),
            ResponseShape.DeferredMessage => DeferResponseAsync(context, response),
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
        };

        if (shape is ResponseShape.Values or ResponseShape.ValuesTypedPipe or ResponseShape.ValuesUntypedPipe)
        {
            Assert.Same(responseTask, Assert.Single(context.ConsumeTasks));
            Assert.NotSame(endpointProxy.SendCompletion.Task, responseTask);
        }
        else
        {
            Assert.Equal(2, context.ConsumeTasks.Count);
            Task ownedSend = Assert.Single(
                context.ConsumeTasks,
                task => ReferenceEquals(task, endpointProxy.SendCompletion.Task));
            Task ownedResponse = Assert.Single(context.ConsumeTasks, task => ReferenceEquals(task, responseTask));
            Assert.Same(endpointProxy.SendCompletion.Task, ownedSend);
            Assert.Same(responseTask, ownedResponse);
            Assert.NotSame(endpointProxy.SendCompletion.Task, responseTask);
        }
        ResponseMessage sent = Assert.IsType<ResponseMessage>(endpointProxy.Message);
        Assert.Equal("accepted", sent.Value);

        endpointProxy.SendCompletion.TrySetResult();
        await responseTask;
    }

    [Theory]
    [InlineData(InvalidEndpointResult.MissingTask)]
    [InlineData(InvalidEndpointResult.MissingEndpoint)]
    [RequirementCoverage("REQ-VSB-CONSUME-ENDPOINT-BOUNDARY", "invalid-send-provider-result")]
    public async Task SendEndpointResolution_RejectsInvalidProviderResultAsync(InvalidEndpointResult result)
    {
        ISendEndpointProvider sendEndpointProvider =
            DispatchProxy.Create<ISendEndpointProvider, InvalidSendEndpointProviderProxy>();
        ((InvalidSendEndpointProviderProxy)(object)sendEndpointProvider).Result = result;
        ReceiveContext receiveContext = CreateReceiveContext(sendEndpointProvider);
        var context = new RecordingConsumeContext(receiveContext);
        Uri address = new("loopback://localhost/invalid-send-endpoint");

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.GetSendEndpointAsync(address, TestContext.Current.CancellationToken));

        Assert.Equal(
            result == InvalidEndpointResult.MissingTask
                ? $"The receive context send endpoint provider returned no resolution task for '{address}'."
                : $"The receive context send endpoint provider resolved no send endpoint for '{address}'.",
            exception.Message);
    }

    [Theory]
    [InlineData(FaultNotificationShape.Failure, true)]
    [InlineData(FaultNotificationShape.MatchingCancellation, false)]
    [InlineData(FaultNotificationShape.ForeignCancellation, true)]
    [InlineData(FaultNotificationShape.CanceledDelivery, false)]
    [RequirementCoverage("REQ-VSB-CONSUME-FAULT-NOTIFICATION", "generation-and-notification-branches")]
    public async Task FaultNotification_GeneratesOnlyActionableFaultsAndAlwaysNotifiesAsync(
        FaultNotificationShape shape,
        bool expectsGeneratedFault)
    {
        using var deliveryCancellation = new CancellationTokenSource();
        using var foreignCancellation = new CancellationTokenSource();
        using var notificationCancellation = new CancellationTokenSource();
        if (shape == FaultNotificationShape.CanceledDelivery)
            deliveryCancellation.Cancel();

        ISendEndpointProvider sendEndpointProvider =
            DispatchProxy.Create<ISendEndpointProvider, UnusedProxy>();
        ReceiveContext receiveContext = CreateReceiveContext(sendEndpointProvider);
        var receiveProxy = (ResponseReceiveContextProxy)(object)receiveContext;
        receiveProxy.CancellationToken = deliveryCancellation.Token;
        var context = new RecordingConsumeContext(receiveContext);
        ConsumeContext<FaultMessage> messageContext =
            DispatchProxy.Create<ConsumeContext<FaultMessage>, FaultMessageContextProxy>();
        ((FaultMessageContextProxy)(object)messageContext).CancellationToken = deliveryCancellation.Token;
        Exception failure = shape switch
        {
            FaultNotificationShape.MatchingCancellation =>
                new OperationCanceledException(deliveryCancellation.Token),
            FaultNotificationShape.ForeignCancellation =>
                new OperationCanceledException(foreignCancellation.Token),
            _ => new ExpectedConsumeFailure(),
        };
        TimeSpan duration = TimeSpan.FromMilliseconds(17);

        await context.NotifyFaultedAsync(
            messageContext,
            duration,
            "faulting-consumer",
            failure,
            notificationCancellation.Token);

        if (expectsGeneratedFault)
        {
            GeneratedFault generated = Assert.Single(context.GeneratedFaults);
            Assert.Same(messageContext, generated.Context);
            Assert.Same(failure, generated.Exception);
        }
        else
            Assert.Empty(context.GeneratedFaults);

        FaultNotification notification = Assert.Single(receiveProxy.FaultNotifications);
        Assert.Same(messageContext, notification.Context);
        Assert.Equal(duration, notification.Duration);
        Assert.Equal("faulting-consumer", notification.ConsumerType);
        Assert.Same(failure, notification.Exception);
        Assert.Equal(notificationCancellation.Token, notification.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-ENDPOINT-BOUNDARY", "send-observer-registration")]
    public void SendObserverRegistration_PreservesObserverAndConnectionIdentity()
    {
        ISendEndpointProvider sendEndpointProvider =
            DispatchProxy.Create<ISendEndpointProvider, ObserverSendEndpointProviderProxy>();
        var providerProxy = (ObserverSendEndpointProviderProxy)(object)sendEndpointProvider;
        ReceiveContext receiveContext = CreateReceiveContext(sendEndpointProvider);
        var context = new RecordingConsumeContext(receiveContext);
        ISendObserver observer = DispatchProxy.Create<ISendObserver, UnusedProxy>();

        Assert.Same(providerProxy.Connection, context.ConnectSendObserver(observer));
        Assert.Same(observer, Assert.Single(providerProxy.Observers));
        Assert.Equal(
            "observer",
            Assert.Throws<ArgumentNullException>(() => context.ConnectSendObserver(null!)).ParamName);
        Assert.Single(providerProxy.Observers);
    }

    private static Task DeferResponseAsync(RecordingConsumeContext context, ResponseMessage response)
    {
        context.DeferResponse(response);
        return context.ConsumeTasks[^1];
    }

    public enum ResponseShape
    {
        Message,
        MessageOptions,
        MessageTypedPipe,
        MessageUntypedPipe,
        RuntimeMessage,
        RuntimeMessageType,
        RuntimeMessagePipe,
        RuntimeMessageTypePipe,
        Values,
        ValuesTypedPipe,
        ValuesUntypedPipe,
        DeferredMessage,
    }

    public enum InvalidEndpointResult
    {
        MissingTask,
        MissingEndpoint,
    }

    public enum FaultNotificationShape
    {
        Failure,
        MatchingCancellation,
        ForeignCancellation,
        CanceledDelivery,
    }

    private sealed record FaultMessage(string Value);

    private sealed class ExpectedConsumeFailure : Exception;

    private sealed record GeneratedFault(object Context, Exception Exception);

    private sealed record FaultNotification(
        object Context,
        TimeSpan Duration,
        string ConsumerType,
        Exception Exception,
        CancellationToken CancellationToken);

    private sealed class ResponseMessage
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class RecordingConsumeContext : BaseConsumeContext
    {
        private static readonly Uri ResponseDestination = new("loopback://localhost/base-response-lifetime");

        public RecordingConsumeContext(ISendEndpoint endpoint)
            : base(CreateReceiveContext(endpoint), DispatchProxy.Create<SerializerContext, UnusedProxy>())
        {
        }

        public RecordingConsumeContext(ReceiveContext receiveContext)
            : base(receiveContext, DispatchProxy.Create<SerializerContext, UnusedProxy>())
        {
        }

        public List<Task> ConsumeTasks { get; } = [];

        public List<GeneratedFault> GeneratedFaults { get; } = [];

        public override Task ConsumeCompleted => Task.WhenAll(ConsumeTasks);

        public override Guid? MessageId => Guid.Parse("e94de726-1bbe-4ff4-98fc-346e8d191e05");

        public override Guid? RequestId => Guid.Parse("832bb9bb-20f2-425f-a524-c71fc3c507dd");

        public override Guid? CorrelationId => null;

        public override Guid? ConversationId => null;

        public override Guid? InitiatorId => null;

        public override DateTimeOffset? ExpirationTime => null;

        public override Uri? SourceAddress => null;

        public override Uri? DestinationAddress => null;

        public override Uri? ResponseAddress => ResponseDestination;

        public override Uri? FaultAddress => null;

        public override DateTimeOffset? SentTime => null;

        public override Headers Headers => DispatchProxy.Create<Headers, UnusedProxy>();

        public override HostInfo Host => DispatchProxy.Create<HostInfo, UnusedProxy>();

        public override IEnumerable<string> SupportedMessageTypes => [];

        public override bool HasMessageType(Type messageType) => false;

        public override bool TryGetMessage<T>([NotNullWhen(true)] out ConsumeContext<T>? consumeContext)
            where T : class
        {
            consumeContext = null;
            return false;
        }

        public override bool HasPayloadType(Type payloadType) => payloadType.IsInstanceOfType(this);

        public override bool TryGetPayload<T>([NotNullWhen(true)] out T? payload)
            where T : class
        {
            payload = this as T;
            return payload is not null;
        }

        public override T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
            where T : class => payloadFactory();

        public override T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
            where T : class => addFactory();

        public override void AddConsumeTask(Task task)
        {
            ConsumeTasks.Add(task);
        }

        protected override Task GenerateFaultAsync<T>(ConsumeContext<T> context, Exception exception)
        {
            GeneratedFaults.Add(new GeneratedFault(context, exception));
            return Task.CompletedTask;
        }

        private static ReceiveContext CreateReceiveContext(ISendEndpoint endpoint)
        {
            ISendEndpointProvider sendEndpointProvider =
                DispatchProxy.Create<ISendEndpointProvider, ImmediateSendEndpointProviderProxy>();
            ((ImmediateSendEndpointProviderProxy)(object)sendEndpointProvider).Endpoint = endpoint;
            IPublishEndpointProvider publishEndpointProvider =
                DispatchProxy.Create<IPublishEndpointProvider, UnusedProxy>();
            ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, ResponseReceiveContextProxy>();
            var receiveContextProxy = (ResponseReceiveContextProxy)(object)receiveContext;
            receiveContextProxy.SendEndpointProvider = sendEndpointProvider;
            receiveContextProxy.PublishEndpointProvider = publishEndpointProvider;
            return receiveContext;
        }
    }

    private class PendingSendEndpointProxy : DispatchProxy
    {
        public TaskCompletionSource SendCompletion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public object? Message { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != "SendAsync")
                throw new InvalidOperationException($"The pending response endpoint unexpectedly invoked {targetMethod?.Name}.");

            Message = args![0];
            return SendCompletion.Task;
        }
    }

    private class ImmediateSendEndpointProviderProxy : DispatchProxy
    {
        public ISendEndpoint Endpoint { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "GetSendEndpointAsync"
                ? Task.FromResult(Endpoint)
                : throw new InvalidOperationException($"The response endpoint provider unexpectedly invoked {targetMethod?.Name}.");
    }

    private class InvalidSendEndpointProviderProxy : DispatchProxy
    {
        public InvalidEndpointResult Result { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != "GetSendEndpointAsync")
            {
                throw new InvalidOperationException(
                    $"The invalid send endpoint provider unexpectedly invoked {targetMethod?.Name}.");
            }

            return Result == InvalidEndpointResult.MissingTask
                ? null
                : Task.FromResult<ISendEndpoint>(null!);
        }
    }

    private class ObserverSendEndpointProviderProxy : DispatchProxy
    {
        public ConnectHandle Connection { get; } = new EmptyConnectHandle();

        public List<ISendObserver> Observers { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != nameof(ISendObserverConnector.ConnectSendObserver))
            {
                throw new InvalidOperationException(
                    $"The observer send endpoint provider unexpectedly invoked {targetMethod?.Name}.");
            }

            Observers.Add((ISendObserver)args![0]!);
            return Connection;
        }
    }

    private class ResponseReceiveContextProxy : DispatchProxy
    {
        public CancellationToken CancellationToken { get; set; } = TestContext.Current.CancellationToken;

        public List<FaultNotification> FaultNotifications { get; } = [];

        public IPublishEndpointProvider PublishEndpointProvider { get; set; } = null!;

        public ISendEndpointProvider SendEndpointProvider { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_CancellationToken" => CancellationToken,
            "get_PublishEndpointProvider" => PublishEndpointProvider,
            "get_SendEndpointProvider" => SendEndpointProvider,
            nameof(ReceiveContext.NotifyFaultedAsync) => RecordFaultNotification(args),
            _ => throw new InvalidOperationException($"The response receive context unexpectedly invoked {targetMethod?.Name}."),
        };

        private object RecordFaultNotification(object?[]? args)
        {
            FaultNotifications.Add(new FaultNotification(
                args![0]!,
                (TimeSpan)args[1]!,
                (string)args[2]!,
                (Exception)args[3]!,
                (CancellationToken)args[4]!));
            return Task.CompletedTask;
        }
    }

    private class FaultMessageContextProxy : DispatchProxy
    {
        public CancellationToken CancellationToken { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_CancellationToken"
                ? CancellationToken
                : throw new InvalidOperationException(
                    $"The fault message context unexpectedly invoked {targetMethod?.Name}.");
    }

    private static ReceiveContext CreateReceiveContext(ISendEndpointProvider sendEndpointProvider)
    {
        ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, ResponseReceiveContextProxy>();
        var receiveContextProxy = (ResponseReceiveContextProxy)(object)receiveContext;
        receiveContextProxy.SendEndpointProvider = sendEndpointProvider;
        receiveContextProxy.PublishEndpointProvider =
            DispatchProxy.Create<IPublishEndpointProvider, UnusedProxy>();
        return receiveContext;
    }

    private class UnusedProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The response lifetime test unexpectedly invoked {targetMethod?.Name}.");
    }
}
