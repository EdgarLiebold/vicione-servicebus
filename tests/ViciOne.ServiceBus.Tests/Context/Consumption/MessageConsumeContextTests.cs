using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Context.Consumption;

public sealed class MessageConsumeContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONSUME-CONTEXT", "construction-requires-context-and-message")]
    public void Construction_RejectsMissingCollaborators()
    {
        ConsumeContext source = DispatchProxy.Create<ConsumeContext, TypeLookupConsumeContextProxy>();

        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => new MessageConsumeContext<ProbeMessage>(null!, new ProbeMessage())).ParamName);
        Assert.Equal(
            "message",
            Assert.Throws<ArgumentNullException>(() => new MessageConsumeContext<ProbeMessage>(source, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONSUME-CONTEXT", "projected-message-types-are-visible-without-source-support")]
    public void ProjectedMessage_IsVisibleThroughExactAndAssignableTypedLookups()
    {
        ConsumeContext source = DispatchProxy.Create<ConsumeContext, TypeLookupConsumeContextProxy>();
        var sourceProxy = (TypeLookupConsumeContextProxy)(object)source;
        var message = new ProbeMessage();
        var context = new MessageConsumeContext<ProbeMessage>(source, message);

        Assert.True(context.HasMessageType(typeof(ProbeMessage)));
        Assert.True(context.HasMessageType(typeof(IProbeMessage)));
        Assert.True(context.TryGetMessage(out ConsumeContext<ProbeMessage>? exact));
        Assert.Same(context, exact);
        Assert.True(context.TryGetMessage(out ConsumeContext<IProbeMessage>? assignable));
        Assert.Same(message, assignable.Message);
        Assert.Equal(2, sourceProxy.HasMessageTypeInvocationCount);
        Assert.Equal(2, sourceProxy.TryGetMessageInvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONSUME-CONTEXT", "unsupported-message-types-delegate-to-source")]
    public void UnsupportedMessageType_DelegatesToTheSourceContext()
    {
        ConsumeContext source = DispatchProxy.Create<ConsumeContext, TypeLookupConsumeContextProxy>();
        var sourceProxy = (TypeLookupConsumeContextProxy)(object)source;
        var context = new MessageConsumeContext<ProbeMessage>(source, new ProbeMessage());

        Assert.False(context.HasMessageType(typeof(UnrelatedMessage)));
        Assert.False(context.TryGetMessage(out ConsumeContext<UnrelatedMessage>? unrelated));
        Assert.Null(unrelated);
        Assert.False(context.HasPayloadType(typeof(UnrelatedMessage)));
        Assert.False(context.TryGetPayload(out UnrelatedMessage? unrelatedPayload));
        Assert.Null(unrelatedPayload);
        Assert.Equal(1, sourceProxy.HasMessageTypeInvocationCount);
        Assert.Equal(1, sourceProxy.TryGetMessageInvocationCount);
        Assert.Equal("messageType", Assert.Throws<ArgumentNullException>(() => context.HasMessageType(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONSUME-CONTEXT", "source-contract-remains-authoritative-when-already-available")]
    public void ExistingSourceContract_RemainsAuthoritativeForRepeatedMaterialization()
    {
        ConsumeContext source = DispatchProxy.Create<ConsumeContext, TypeLookupConsumeContextProxy>();
        ConsumeContext<ProbeMessage> sourceMessageContext =
            DispatchProxy.Create<ConsumeContext<ProbeMessage>, UnexpectedInvocationProxy>();
        var sourceProxy = (TypeLookupConsumeContextProxy)(object)source;
        sourceProxy.ReturnedContext = sourceMessageContext;
        sourceProxy.HasMessageTypeResult = true;
        var context = new MessageConsumeContext<ProbeMessage>(source, new ProbeMessage());

        Assert.True(context.HasMessageType(typeof(ProbeMessage)));
        Assert.True(context.TryGetMessage(out ConsumeContext<ProbeMessage>? selected));
        Assert.Same(sourceMessageContext, selected);
        Assert.Equal(1, sourceProxy.HasMessageTypeInvocationCount);
        Assert.Equal(1, sourceProxy.TryGetMessageInvocationCount);
    }

    [Theory]
    [InlineData(InitializedResponseShape.Values)]
    [InlineData(InitializedResponseShape.TypedPipe)]
    [InlineData(InitializedResponseShape.UntypedPipe)]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONSUME-CONTEXT", "initialized-response-is-owned-before-endpoint-resolution")]
    public async Task InitializedResponse_BecomesConsumeOwnedBeforeEndpointResolutionAsync(
        InitializedResponseShape shape)
    {
        var endpointResolution = new TaskCompletionSource<ISendEndpoint>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        ISendEndpoint endpoint = DispatchProxy.Create<IAdvancedSendEndpoint, PendingSendEndpointProxy>();
        var endpointProxy = (PendingSendEndpointProxy)(object)endpoint;
        ISendEndpointProvider endpointProvider =
            DispatchProxy.Create<ISendEndpointProvider, PendingSendEndpointProviderProxy>();
        ((PendingSendEndpointProviderProxy)(object)endpointProvider).EndpointResolution = endpointResolution.Task;
        ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, ResponseReceiveContextProxy>();
        ((ResponseReceiveContextProxy)(object)receiveContext).SendEndpointProvider = endpointProvider;
        ConsumeContext source = DispatchProxy.Create<ConsumeContext, ResponseConsumeContextProxy>();
        var sourceProxy = (ResponseConsumeContextProxy)(object)source;
        sourceProxy.ReceiveContext = receiveContext;
        var context = new MessageConsumeContext<ProbeMessage>(source, new ProbeMessage());

        object values = new { Value = "accepted" };
        Task responseTask = shape switch
        {
            InitializedResponseShape.Values => context.RespondAsync<ResponseMessage>(values),
            InitializedResponseShape.TypedPipe => context.RespondAsync<ResponseMessage>(
                values,
                Pipe.Empty<SendContext<ResponseMessage>>()),
            InitializedResponseShape.UntypedPipe => context.RespondAsync<ResponseMessage>(
                values,
                Pipe.Empty<SendContext>()),
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
        };

        Assert.Same(responseTask, Assert.Single(sourceProxy.ConsumeTasks));

        endpointResolution.TrySetResult(endpoint);
        Task firstCompletion = await Task.WhenAny(endpointProxy.SendStarted.Task, responseTask)
            .WaitAsync(TestContext.Current.CancellationToken);
        if (ReferenceEquals(firstCompletion, responseTask))
            await responseTask;
        Assert.Same(endpointProxy.SendStarted.Task, firstCompletion);
        await sourceProxy.SecondConsumeTaskRegistered.Task
            .WaitAsync(TestContext.Current.CancellationToken);

        Assert.Collection(
            sourceProxy.ConsumeTasks,
            task => Assert.Same(responseTask, task),
            task => Assert.Same(endpointProxy.SendCompletion.Task, task));
        Assert.NotSame(responseTask, endpointProxy.SendCompletion.Task);
        ResponseMessage sent = Assert.IsType<ResponseMessage>(endpointProxy.Message);
        Assert.Equal("accepted", sent.Value);

        endpointProxy.SendCompletion.TrySetResult();
        await responseTask;
    }

    [Theory]
    [InlineData(ForwardingShape.Outgoing)]
    [InlineData(ForwardingShape.NotifyConsumed)]
    [InlineData(ForwardingShape.NotifyFaulted)]
    [InlineData(ForwardingShape.PublishMessage)]
    [InlineData(ForwardingShape.PublishTypedPipe)]
    [InlineData(ForwardingShape.PublishUntypedPipe)]
    [InlineData(ForwardingShape.PublishRuntimeMessage)]
    [InlineData(ForwardingShape.PublishRuntimePipe)]
    [InlineData(ForwardingShape.PublishRuntimeType)]
    [InlineData(ForwardingShape.PublishRuntimeTypePipe)]
    [InlineData(ForwardingShape.PublishValues)]
    [InlineData(ForwardingShape.PublishValuesTypedPipe)]
    [InlineData(ForwardingShape.PublishValuesUntypedPipe)]
    [InlineData(ForwardingShape.ConnectPublishObserver)]
    [InlineData(ForwardingShape.ConnectSendObserver)]
    [InlineData(ForwardingShape.GetSendEndpoint)]
    [InlineData(ForwardingShape.RespondMessage)]
    [InlineData(ForwardingShape.RespondOptions)]
    [InlineData(ForwardingShape.RespondTypedPipe)]
    [InlineData(ForwardingShape.RespondUntypedPipe)]
    [InlineData(ForwardingShape.RespondRuntimeMessage)]
    [InlineData(ForwardingShape.RespondRuntimeType)]
    [InlineData(ForwardingShape.RespondRuntimePipe)]
    [InlineData(ForwardingShape.RespondRuntimeTypePipe)]
    [InlineData(ForwardingShape.DeferResponse)]
    [InlineData(ForwardingShape.NotifyConsumedContext)]
    [InlineData(ForwardingShape.NotifyFaultedContext)]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONSUME-CONTEXT", "every-forwarding-api-preserves-call-shape")]
    public void EveryForwardingApi_PreservesTheSourceCallShape(ForwardingShape shape)
    {
        ConsumeContext source = DispatchProxy.Create<ConsumeContext, ForwardingConsumeContextProxy>();
        var sourceProxy = (ForwardingConsumeContextProxy)(object)source;
        var consumedMessage = new ProbeMessage();
        var context = new MessageConsumeContext<ProbeMessage>(source, consumedMessage);
        var response = new ResponseMessage { Value = "forwarded" };
        object values = new { Value = "initialized" };
        var options = new SendOptions();
        IPipe<PublishContext<ResponseMessage>> typedPublishPipe = Pipe.Empty<PublishContext<ResponseMessage>>();
        IPipe<PublishContext> publishPipe = Pipe.Empty<PublishContext>();
        IPipe<SendContext<ResponseMessage>> typedSendPipe = Pipe.Empty<SendContext<ResponseMessage>>();
        IPipe<SendContext> sendPipe = Pipe.Empty<SendContext>();
        IPublishObserver publishObserver = DispatchProxy.Create<IPublishObserver, UnexpectedInvocationProxy>();
        ISendObserver sendObserver = DispatchProxy.Create<ISendObserver, UnexpectedInvocationProxy>();
        Uri address = new("loopback://localhost/message-context-forwarding");
        using var cancellation = new CancellationTokenSource();
        object? significantArgument = null;
        bool expectsCancellationToken = false;

        switch (shape)
        {
            case ForwardingShape.Outgoing:
                Assert.Same(sourceProxy.Outgoing, context.Outgoing);
                break;
            case ForwardingShape.NotifyConsumed:
                Assert.Same(sourceProxy.CompletionTask, context.NotifyConsumedAsync(
                    TimeSpan.FromSeconds(1),
                    "consumer",
                    cancellation.Token));
                significantArgument = context;
                expectsCancellationToken = true;
                break;
            case ForwardingShape.NotifyFaulted:
                var failure = new ExpectedForwardingFailure();
                Assert.Same(sourceProxy.CompletionTask, context.NotifyFaultedAsync(
                    TimeSpan.FromSeconds(2),
                    "consumer",
                    failure,
                    cancellation.Token));
                significantArgument = failure;
                expectsCancellationToken = true;
                break;
            case ForwardingShape.PublishMessage:
                Assert.Same(sourceProxy.CompletionTask, context.PublishAsync(response, cancellation.Token));
                significantArgument = response;
                expectsCancellationToken = true;
                break;
            case ForwardingShape.PublishTypedPipe:
                Assert.Same(sourceProxy.CompletionTask, context.PublishAsync(response, typedPublishPipe, cancellation.Token));
                significantArgument = typedPublishPipe;
                expectsCancellationToken = true;
                break;
            case ForwardingShape.PublishUntypedPipe:
                Assert.Same(sourceProxy.CompletionTask, context.PublishAsync(response, publishPipe, cancellation.Token));
                significantArgument = publishPipe;
                expectsCancellationToken = true;
                break;
            case ForwardingShape.PublishRuntimeMessage:
                Assert.Same(sourceProxy.CompletionTask, context.PublishAsync((object)response, cancellation.Token));
                significantArgument = response;
                expectsCancellationToken = true;
                break;
            case ForwardingShape.PublishRuntimePipe:
                Assert.Same(sourceProxy.CompletionTask, context.PublishAsync((object)response, publishPipe, cancellation.Token));
                significantArgument = publishPipe;
                expectsCancellationToken = true;
                break;
            case ForwardingShape.PublishRuntimeType:
                Assert.Same(sourceProxy.CompletionTask, context.PublishAsync((object)response, typeof(ResponseMessage), cancellation.Token));
                significantArgument = typeof(ResponseMessage);
                expectsCancellationToken = true;
                break;
            case ForwardingShape.PublishRuntimeTypePipe:
                Assert.Same(sourceProxy.CompletionTask, context.PublishAsync(
                    (object)response,
                    typeof(ResponseMessage),
                    publishPipe,
                    cancellation.Token));
                significantArgument = publishPipe;
                expectsCancellationToken = true;
                break;
            case ForwardingShape.PublishValues:
                Assert.Same(sourceProxy.CompletionTask, context.PublishAsync<ResponseMessage>(values, cancellation.Token));
                significantArgument = values;
                expectsCancellationToken = true;
                break;
            case ForwardingShape.PublishValuesTypedPipe:
                Assert.Same(sourceProxy.CompletionTask, context.PublishAsync<ResponseMessage>(
                    values,
                    typedPublishPipe,
                    cancellation.Token));
                significantArgument = typedPublishPipe;
                expectsCancellationToken = true;
                break;
            case ForwardingShape.PublishValuesUntypedPipe:
                Assert.Same(sourceProxy.CompletionTask, context.PublishAsync<ResponseMessage>(
                    values,
                    publishPipe,
                    cancellation.Token));
                significantArgument = publishPipe;
                expectsCancellationToken = true;
                break;
            case ForwardingShape.ConnectPublishObserver:
                Assert.Same(sourceProxy.Connection, context.ConnectPublishObserver(publishObserver));
                significantArgument = publishObserver;
                break;
            case ForwardingShape.ConnectSendObserver:
                Assert.Same(sourceProxy.Connection, context.ConnectSendObserver(sendObserver));
                significantArgument = sendObserver;
                break;
            case ForwardingShape.GetSendEndpoint:
                Assert.Same(sourceProxy.EndpointResolution, context.GetSendEndpointAsync(address, cancellation.Token));
                significantArgument = address;
                expectsCancellationToken = true;
                break;
            case ForwardingShape.RespondMessage:
                Assert.Same(sourceProxy.CompletionTask, context.RespondAsync(response));
                significantArgument = response;
                break;
            case ForwardingShape.RespondOptions:
                Assert.Same(sourceProxy.CompletionTask, context.RespondAsync(response, options));
                significantArgument = options;
                break;
            case ForwardingShape.RespondTypedPipe:
                Assert.Same(sourceProxy.CompletionTask, context.RespondAsync(response, typedSendPipe));
                significantArgument = typedSendPipe;
                break;
            case ForwardingShape.RespondUntypedPipe:
                Assert.Same(sourceProxy.CompletionTask, context.RespondAsync(response, sendPipe));
                significantArgument = sendPipe;
                break;
            case ForwardingShape.RespondRuntimeMessage:
                Assert.Same(sourceProxy.CompletionTask, context.RespondAsync((object)response));
                significantArgument = response;
                break;
            case ForwardingShape.RespondRuntimeType:
                Assert.Same(sourceProxy.CompletionTask, context.RespondAsync((object)response, typeof(ResponseMessage)));
                significantArgument = typeof(ResponseMessage);
                break;
            case ForwardingShape.RespondRuntimePipe:
                Assert.Same(sourceProxy.CompletionTask, context.RespondAsync((object)response, sendPipe));
                significantArgument = sendPipe;
                break;
            case ForwardingShape.RespondRuntimeTypePipe:
                Assert.Same(sourceProxy.CompletionTask, context.RespondAsync(
                    (object)response,
                    typeof(ResponseMessage),
                    sendPipe));
                significantArgument = sendPipe;
                break;
            case ForwardingShape.DeferResponse:
                context.DeferResponse(response);
                significantArgument = response;
                break;
            case ForwardingShape.NotifyConsumedContext:
                Assert.Same(sourceProxy.CompletionTask, context.NotifyConsumedAsync(
                    context,
                    TimeSpan.FromSeconds(3),
                    "consumer",
                    cancellation.Token));
                significantArgument = context;
                expectsCancellationToken = true;
                break;
            case ForwardingShape.NotifyFaultedContext:
                var contextFailure = new ExpectedForwardingFailure();
                Assert.Same(sourceProxy.CompletionTask, context.NotifyFaultedAsync(
                    context,
                    TimeSpan.FromSeconds(4),
                    "consumer",
                    contextFailure,
                    cancellation.Token));
                significantArgument = contextFailure;
                expectsCancellationToken = true;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
        }

        RecordedInvocation invocation = Assert.Single(sourceProxy.Invocations);
        Assert.Equal(ExpectedMethodName(shape), invocation.Method.Name);
        if (significantArgument is not null)
            Assert.Contains(invocation.Arguments, argument => ReferenceEquals(significantArgument, argument));
        if (expectsCancellationToken)
            Assert.Contains(invocation.Arguments, argument => argument is CancellationToken token && token == cancellation.Token);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONSUME-CONTEXT", "forwarding-apis-require-local-arguments")]
    public void ForwardingApis_RejectMissingLocallyOwnedArguments()
    {
        ConsumeContext source = DispatchProxy.Create<ConsumeContext, ForwardingConsumeContextProxy>();
        var context = new MessageConsumeContext<ProbeMessage>(source, new ProbeMessage());
        void ResolveMissingAddress() =>
            _ = context.GetSendEndpointAsync(null!, TestContext.Current.CancellationToken);
        void RespondWithMissingValues() =>
            _ = context.RespondAsync<ResponseMessage>((object)null!);
        void RespondWithMissingValuesAndTypedPipe() =>
            _ = context.RespondAsync<ResponseMessage>(
                (object)null!,
                Pipe.Empty<SendContext<ResponseMessage>>());
        void RespondWithMissingValuesAndUntypedPipe() =>
            _ = context.RespondAsync<ResponseMessage>((object)null!, Pipe.Empty<SendContext>());
        void RespondWithMissingTypedPipe() =>
            _ = context.RespondAsync(
                new { Value = "value" },
                (IPipe<SendContext<ResponseMessage>>)null!);
        void RespondWithMissingUntypedPipe() =>
            _ = context.RespondAsync<ResponseMessage>(
                new { Value = "value" },
                (IPipe<SendContext>)null!);

        Assert.True(context.TryGetPayload(out MessageConsumeContext<ProbeMessage>? selectedContext));
        Assert.Same(context, selectedContext);
        Assert.Same(
            context,
            context.GetOrAddPayload<MessageConsumeContext<ProbeMessage>>(
                () => throw new InvalidOperationException()));
        Assert.Same(
            context,
            context.AddOrUpdatePayload<MessageConsumeContext<ProbeMessage>>(
                () => throw new InvalidOperationException(),
                _ => throw new InvalidOperationException()));

        Assert.Equal(
            "observer",
            Assert.Throws<ArgumentNullException>(() => context.ConnectPublishObserver(null!)).ParamName);
        Assert.Equal(
            "observer",
            Assert.Throws<ArgumentNullException>(() => context.ConnectSendObserver(null!)).ParamName);
        Assert.Equal(
            "address",
            Assert.Throws<ArgumentNullException>(ResolveMissingAddress).ParamName);
        Assert.Equal("task", Assert.Throws<ArgumentNullException>(() => context.AddConsumeTask(null!)).ParamName);
        Assert.Equal(
            "values",
            Assert.Throws<ArgumentNullException>(RespondWithMissingValues).ParamName);
        Assert.Equal(
            "values",
            Assert.Throws<ArgumentNullException>(RespondWithMissingValuesAndTypedPipe).ParamName);
        Assert.Equal(
            "values",
            Assert.Throws<ArgumentNullException>(RespondWithMissingValuesAndUntypedPipe).ParamName);
        Assert.Equal(
            "sendPipe",
            Assert.Throws<ArgumentNullException>(RespondWithMissingTypedPipe).ParamName);
        Assert.Equal(
            "sendPipe",
            Assert.Throws<ArgumentNullException>(RespondWithMissingUntypedPipe).ParamName);
    }

    private static string ExpectedMethodName(ForwardingShape shape) => shape switch
    {
        ForwardingShape.Outgoing => "get_Outgoing",
        ForwardingShape.NotifyConsumed or ForwardingShape.NotifyConsumedContext =>
            nameof(ConsumeContext.NotifyConsumedAsync),
        ForwardingShape.NotifyFaulted or ForwardingShape.NotifyFaultedContext =>
            nameof(ConsumeContext.NotifyFaultedAsync),
        ForwardingShape.PublishMessage or ForwardingShape.PublishTypedPipe or ForwardingShape.PublishUntypedPipe
            or ForwardingShape.PublishRuntimeMessage or ForwardingShape.PublishRuntimePipe
            or ForwardingShape.PublishRuntimeType or ForwardingShape.PublishRuntimeTypePipe
            or ForwardingShape.PublishValues or ForwardingShape.PublishValuesTypedPipe
            or ForwardingShape.PublishValuesUntypedPipe =>
            nameof(IPublishEndpoint.PublishAsync),
        ForwardingShape.ConnectPublishObserver => nameof(IPublishObserverConnector.ConnectPublishObserver),
        ForwardingShape.ConnectSendObserver => nameof(ISendObserverConnector.ConnectSendObserver),
        ForwardingShape.GetSendEndpoint => nameof(ISendEndpointProvider.GetSendEndpointAsync),
        ForwardingShape.RespondMessage or ForwardingShape.RespondOptions or ForwardingShape.RespondTypedPipe
            or ForwardingShape.RespondUntypedPipe or ForwardingShape.RespondRuntimeMessage
            or ForwardingShape.RespondRuntimeType or ForwardingShape.RespondRuntimePipe
            or ForwardingShape.RespondRuntimeTypePipe => nameof(ConsumeContext.RespondAsync),
        ForwardingShape.DeferResponse => nameof(ConsumeContext.DeferResponse),
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
    };

    private interface IProbeMessage
    {
    }

    private sealed class ProbeMessage : IProbeMessage
    {
    }

    private sealed class UnrelatedMessage
    {
    }

    private sealed class ResponseMessage
    {
        public string Value { get; set; } = string.Empty;
    }

    public enum InitializedResponseShape
    {
        Values,
        TypedPipe,
        UntypedPipe,
    }

    public enum ForwardingShape
    {
        Outgoing,
        NotifyConsumed,
        NotifyFaulted,
        PublishMessage,
        PublishTypedPipe,
        PublishUntypedPipe,
        PublishRuntimeMessage,
        PublishRuntimePipe,
        PublishRuntimeType,
        PublishRuntimeTypePipe,
        PublishValues,
        PublishValuesTypedPipe,
        PublishValuesUntypedPipe,
        ConnectPublishObserver,
        ConnectSendObserver,
        GetSendEndpoint,
        RespondMessage,
        RespondOptions,
        RespondTypedPipe,
        RespondUntypedPipe,
        RespondRuntimeMessage,
        RespondRuntimeType,
        RespondRuntimePipe,
        RespondRuntimeTypePipe,
        DeferResponse,
        NotifyConsumedContext,
        NotifyFaultedContext,
    }

    private sealed record RecordedInvocation(MethodInfo Method, object?[] Arguments);

    private sealed class ExpectedForwardingFailure : Exception;

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The typed source context unexpectedly invoked {targetMethod?.Name}.");
    }

    private class TypeLookupConsumeContextProxy : DispatchProxy
    {
        public bool HasMessageTypeResult { get; set; }

        public int HasMessageTypeInvocationCount { get; private set; }

        public object? ReturnedContext { get; set; }

        public int TryGetMessageInvocationCount { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(ConsumeContext.HasMessageType))
            {
                HasMessageTypeInvocationCount++;
                return HasMessageTypeResult;
            }

            if (targetMethod?.Name == nameof(ConsumeContext.TryGetMessage))
            {
                TryGetMessageInvocationCount++;
                args![0] = ReturnedContext;
                return ReturnedContext is not null;
            }

            if (targetMethod?.Name == nameof(PipeContext.HasPayloadType))
                return false;

            if (targetMethod?.Name == nameof(PipeContext.TryGetPayload))
            {
                args![0] = null;
                return false;
            }

            throw new InvalidOperationException($"The typed message lookup unexpectedly invoked {targetMethod?.Name}.");
        }
    }

    private class ResponseConsumeContextProxy : DispatchProxy
    {
        public List<Task> ConsumeTasks { get; } = [];

        public ReceiveContext ReceiveContext { get; set; } = null!;

        public TaskCompletionSource SecondConsumeTaskRegistered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_CancellationToken" => TestContext.Current.CancellationToken,
                "get_ReceiveContext" => ReceiveContext,
                "get_RequestId" => Guid.Parse("76f02795-73a6-4cc2-9c8a-0478b88db7b2"),
                "get_ResponseAddress" => new Uri("loopback://localhost/response-lifetime"),
                "AddConsumeTask" => RecordConsumeTask(args),
                _ => throw new InvalidOperationException($"The response lifetime test unexpectedly invoked {targetMethod?.Name}."),
            };
        }

        private object? RecordConsumeTask(object?[]? args)
        {
            ConsumeTasks.Add(Assert.IsAssignableFrom<Task>(Assert.Single(args!)));
            if (ConsumeTasks.Count == 2)
                SecondConsumeTaskRegistered.TrySetResult();

            return null;
        }
    }

    private class ForwardingConsumeContextProxy : DispatchProxy
    {
        public Task CompletionTask { get; } = Task.CompletedTask;

        public ConnectHandle Connection { get; } = new EmptyConnectHandle();

        public IOutgoingMessages Outgoing { get; } =
            DispatchProxy.Create<IOutgoingMessages, UnexpectedInvocationProxy>();

        public ISendEndpoint Endpoint { get; } =
            DispatchProxy.Create<IAdvancedSendEndpoint, UnexpectedInvocationProxy>();

        public Task<ISendEndpoint> EndpointResolution { get; }

        public List<RecordedInvocation> Invocations { get; } = [];

        public ForwardingConsumeContextProxy()
        {
            EndpointResolution = Task.FromResult(Endpoint);
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            MethodInfo method = targetMethod
                ?? throw new InvalidOperationException("The forwarding source received no method metadata.");
            Invocations.Add(new RecordedInvocation(method, args?.ToArray() ?? []));

            return method.Name switch
            {
                "get_Outgoing" => Outgoing,
                nameof(ISendEndpointProvider.GetSendEndpointAsync) => EndpointResolution,
                nameof(IPublishObserverConnector.ConnectPublishObserver) => Connection,
                nameof(ISendObserverConnector.ConnectSendObserver) => Connection,
                nameof(ConsumeContext.DeferResponse) => null,
                _ when method.ReturnType == typeof(Task) => CompletionTask,
                _ => throw new InvalidOperationException(
                    $"The forwarding source unexpectedly invoked {method.Name}."),
            };
        }
    }

    private class ResponseReceiveContextProxy : DispatchProxy
    {
        public ISendEndpointProvider SendEndpointProvider { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_SendEndpointProvider"
                ? SendEndpointProvider
                : throw new InvalidOperationException($"The response receive context unexpectedly invoked {targetMethod?.Name}.");
    }

    private class PendingSendEndpointProviderProxy : DispatchProxy
    {
        public Task<ISendEndpoint> EndpointResolution { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "GetSendEndpointAsync"
                ? EndpointResolution
                : throw new InvalidOperationException($"The response endpoint provider unexpectedly invoked {targetMethod?.Name}.");
    }

    private class PendingSendEndpointProxy : DispatchProxy
    {
        public TaskCompletionSource SendCompletion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource SendStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public object? Message { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != "SendAsync")
                throw new InvalidOperationException($"The response endpoint unexpectedly invoked {targetMethod?.Name}.");

            Message = args![0];
            SendStarted.TrySetResult();
            return SendCompletion.Task;
        }
    }
}
