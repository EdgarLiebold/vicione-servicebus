using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Context.Consumption;

public sealed class DeserializerConsumeContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-DESERIALIZER-CONTEXT", "consume-completion-owns-pending-work")]
    public async Task ConsumeCompletion_AwaitsEveryAdmittedTaskAsync()
    {
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var context = CreateContext(TestContext.Current.CancellationToken);

        context.AddConsumeTask(Task.CompletedTask);
        context.AddConsumeTask(pending.Task);
        Task completion = context.ConsumeCompleted;

        Assert.False(completion.IsCompleted);
        pending.TrySetResult();
        await completion;
        Assert.True(completion.IsCompletedSuccessfully);
        Assert.Equal("task", Assert.Throws<ArgumentNullException>(() => context.AddConsumeTask(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DESERIALIZER-CONTEXT", "consume-completion-preserves-failure")]
    public async Task ConsumeCompletion_PreservesTheExactOwnedFailureAsync()
    {
        var failure = new ExpectedConsumeFailure();
        var context = CreateContext(TestContext.Current.CancellationToken);
        context.AddConsumeTask(Task.FromException(failure));

        ExpectedConsumeFailure exception = await Assert.ThrowsAsync<ExpectedConsumeFailure>(
            () => context.ConsumeCompleted);

        Assert.Same(failure, exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DESERIALIZER-CONTEXT", "consume-completion-preserves-cancellation")]
    public async Task ConsumeCompletion_PreservesTheReceiveCancellationTokenAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var context = CreateContext(cancellation.Token);
        context.AddConsumeTask(pending.Task);
        Task completion = context.ConsumeCompleted;

        cancellation.Cancel();
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => completion);

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        pending.TrySetResult();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DESERIALIZER-CONTEXT", "local-context-precedes-receive-payloads")]
    public void PayloadOperations_PreferTheContextAndOtherwiseUseTheReceiveContext()
    {
        var sourcePayload = new Payload("source");
        var context = CreateContext(TestContext.Current.CancellationToken, sourcePayload);
        var contextFactoryInvoked = false;

        Assert.True(context.HasPayloadType(typeof(TestDeserializerConsumeContext)));
        Assert.True(context.TryGetPayload(out TestDeserializerConsumeContext? selectedContext));
        Assert.Same(context, selectedContext);
        Assert.Same(context, context.GetOrAddPayload(() =>
        {
            contextFactoryInvoked = true;
            return context;
        }));
        Assert.Same(context, context.AddOrUpdatePayload<TestDeserializerConsumeContext>(
            () => throw new InvalidOperationException(),
            _ => throw new InvalidOperationException()));
        Assert.False(contextFactoryInvoked);

        Assert.True(context.HasPayloadType(typeof(Payload)));
        Assert.True(context.TryGetPayload(out Payload? selectedPayload));
        Assert.Same(sourcePayload, selectedPayload);
        Assert.Same(sourcePayload, context.GetOrAddPayload(() => new Payload("unexpected")));
        Payload updated = context.AddOrUpdatePayload(
            () => new Payload("unexpected"),
            current => new Payload(current.Value + "-updated"));
        Assert.Equal(new Payload("source-updated"), updated);
        Assert.False(context.HasPayloadType(typeof(UnrelatedPayload)));
        Assert.False(context.TryGetPayload(out UnrelatedPayload? unrelated));
        Assert.Null(unrelated);

        Assert.Equal("payloadType", Assert.Throws<ArgumentNullException>(() => context.HasPayloadType(null!)).ParamName);
        Assert.Equal(
            "payloadFactory",
            Assert.Throws<ArgumentNullException>(() => context.GetOrAddPayload<Payload>(null!)).ParamName);
        Assert.Equal(
            "addFactory",
            Assert.Throws<ArgumentNullException>(() => context.AddOrUpdatePayload<Payload>(null!, value => value)).ParamName);
        Assert.Equal(
            "updateFactory",
            Assert.Throws<ArgumentNullException>(() => context.AddOrUpdatePayload(() => sourcePayload, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DESERIALIZER-CONTEXT", "construction-requires-collaborators")]
    public void Construction_RejectsMissingCollaborators()
    {
        ReceiveContext receiveContext = CreateReceiveContext(TestContext.Current.CancellationToken, null);
        SerializerContext serializerContext = DispatchProxy.Create<SerializerContext, UnexpectedInvocationProxy>();

        Assert.Equal(
            "receiveContext",
            Assert.Throws<ArgumentNullException>(() => new TestDeserializerConsumeContext(null!, serializerContext)).ParamName);
        Assert.Equal(
            "serializerContext",
            Assert.Throws<ArgumentNullException>(() => new TestDeserializerConsumeContext(receiveContext, null!)).ParamName);
    }

    private static TestDeserializerConsumeContext CreateContext(
        CancellationToken cancellationToken,
        object? payload = null)
    {
        ReceiveContext receiveContext = CreateReceiveContext(cancellationToken, payload);
        SerializerContext serializerContext = DispatchProxy.Create<SerializerContext, UnexpectedInvocationProxy>();
        return new TestDeserializerConsumeContext(receiveContext, serializerContext);
    }

    private static ReceiveContext CreateReceiveContext(CancellationToken cancellationToken, object? payload)
    {
        ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, PayloadReceiveContextProxy>();
        var receiveProxy = (PayloadReceiveContextProxy)(object)receiveContext;
        receiveProxy.CancellationToken = cancellationToken;
        receiveProxy.Payload = payload;
        receiveProxy.PublishEndpointProvider =
            DispatchProxy.Create<IPublishEndpointProvider, UnexpectedInvocationProxy>();
        return receiveContext;
    }

    private sealed class TestDeserializerConsumeContext : DeserializerConsumeContext
    {
        public TestDeserializerConsumeContext(ReceiveContext receiveContext, SerializerContext serializerContext)
            : base(receiveContext, serializerContext)
        {
        }

        public override Guid? MessageId => null;
        public override Guid? RequestId => null;
        public override Guid? CorrelationId => null;
        public override Guid? ConversationId => null;
        public override Guid? InitiatorId => null;
        public override DateTimeOffset? ExpirationTime => null;
        public override Uri? SourceAddress => null;
        public override Uri? DestinationAddress => null;
        public override Uri? ResponseAddress => null;
        public override Uri? FaultAddress => null;
        public override DateTimeOffset? SentTime => null;
        public override Headers Headers => DispatchProxy.Create<Headers, UnexpectedInvocationProxy>();
        public override HostInfo Host => DispatchProxy.Create<HostInfo, UnexpectedInvocationProxy>();
        public override IEnumerable<string> SupportedMessageTypes => [];

        public override bool HasMessageType(Type messageType) => false;

        public override bool TryGetMessage<T>([NotNullWhen(true)] out ConsumeContext<T>? consumeContext)
            where T : class
        {
            consumeContext = null;
            return false;
        }
    }

    private class PayloadReceiveContextProxy : DispatchProxy
    {
        public CancellationToken CancellationToken { get; set; }

        public object? Payload { get; set; }

        public IPublishEndpointProvider PublishEndpointProvider { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_CancellationToken" => CancellationToken,
                "get_PublishEndpointProvider" => PublishEndpointProvider,
                nameof(PipeContext.HasPayloadType) => ((Type)args![0]!).IsInstanceOfType(Payload),
                nameof(PipeContext.TryGetPayload) => TryGetPayload(targetMethod, args),
                nameof(PipeContext.GetOrAddPayload) => GetOrAddPayload(args),
                nameof(PipeContext.AddOrUpdatePayload) => AddOrUpdatePayload(args),
                _ => throw new InvalidOperationException(
                    $"The payload receive context unexpectedly invoked {targetMethod?.Name}."),
            };
        }

        private object TryGetPayload(MethodInfo targetMethod, object?[]? args)
        {
            bool found = targetMethod.GetGenericArguments()[0].IsInstanceOfType(Payload);
            args![0] = found ? Payload : null;
            return found;
        }

        private object GetOrAddPayload(object?[]? args)
        {
            Payload ??= ((Delegate)args![0]!).DynamicInvoke();
            return Payload!;
        }

        private object AddOrUpdatePayload(object?[]? args)
        {
            Payload = Payload is null
                ? ((Delegate)args![0]!).DynamicInvoke()
                : ((Delegate)args![1]!).DynamicInvoke(Payload);
            return Payload!;
        }
    }

    private sealed record Payload(string Value);

    private sealed record UnrelatedPayload(string Value);

    private sealed class ExpectedConsumeFailure : Exception;

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The proxy unexpectedly invoked {targetMethod?.Name}.");
    }
}
