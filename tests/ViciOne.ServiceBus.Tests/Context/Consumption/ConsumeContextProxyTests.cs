using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Context.Consumption;

public sealed class ConsumeContextProxyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-CONTEXT-PROXY", "complete-delivery-and-payload-projection")]
    public async Task Proxy_PreservesTheCompleteDeliveryAndPayloadContractAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"consume-proxy-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var assertionsCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        harness.AddHandler<FlowMessage>(context =>
        {
            var proxy = new ConsumeContextProxy<FlowMessage>(context);

            Assert.Same(context.Message, proxy.Message);
            Assert.Equal(context.CancellationToken, proxy.CancellationToken);
            Assert.Equal(context.MessageId, proxy.MessageId);
            Assert.Equal(context.RequestId, proxy.RequestId);
            Assert.Equal(context.CorrelationId, proxy.CorrelationId);
            Assert.Equal(context.ConversationId, proxy.ConversationId);
            Assert.Equal(context.InitiatorId, proxy.InitiatorId);
            Assert.Equal(context.ExpirationTime, proxy.ExpirationTime);
            Assert.Equal(context.SourceAddress, proxy.SourceAddress);
            Assert.Equal(context.DestinationAddress, proxy.DestinationAddress);
            Assert.Equal(context.ResponseAddress, proxy.ResponseAddress);
            Assert.Equal(context.FaultAddress, proxy.FaultAddress);
            Assert.Equal(context.SentTime, proxy.SentTime);
            Assert.Same(context.Headers, proxy.Headers);
            Assert.Same(context.Host, proxy.Host);
            Assert.Same(context.Advanced().ReceiveContext, proxy.ReceiveContext);
            Assert.Same(context.Advanced().SerializerContext, proxy.SerializerContext);
            Assert.Same(context.Advanced().ConsumeCompleted, proxy.ConsumeCompleted);
            Assert.Equal(context.Advanced().SupportedMessageTypes, proxy.SupportedMessageTypes);
            Assert.True(proxy.HasMessageType(typeof(FlowMessage)));
            Assert.True(proxy.TryGetMessage(out ConsumeContext<FlowMessage>? projected));
            Assert.Same(context.Message, projected.Message);
            Assert.NotSame(context, projected);
            Assert.False(proxy.TryGetMessage(out ConsumeContext<MissingFlowMessage>? missing));
            Assert.Null(missing);

            var sourcePayload = new Payload("source");
            Assert.Same(sourcePayload, context.GetOrAddPayload(() => sourcePayload));
            Assert.True(proxy.HasPayloadType(typeof(ConsumeContextProxy<FlowMessage>)));
            Assert.True(proxy.TryGetPayload(out ConsumeContextProxy<FlowMessage>? selectedProxy));
            Assert.Same(proxy, selectedProxy);
            Assert.Same(
                proxy,
                proxy.GetOrAddPayload<ConsumeContextProxy<FlowMessage>>(
                    () => throw new InvalidOperationException()));
            Assert.Same(
                proxy,
                proxy.AddOrUpdatePayload<ConsumeContextProxy<FlowMessage>>(
                    () => throw new InvalidOperationException(),
                    _ => throw new InvalidOperationException()));
            Assert.True(proxy.HasPayloadType(typeof(Payload)));
            Assert.True(proxy.TryGetPayload(out Payload? selectedPayload));
            Assert.Same(sourcePayload, selectedPayload);
            Assert.Same(sourcePayload, proxy.GetOrAddPayload(() => new Payload("unexpected")));
            Payload updated = proxy.AddOrUpdatePayload(
                () => new Payload("unexpected"),
                current => new Payload(current.Value + "-updated"));
            Assert.Equal(new Payload("source-updated"), updated);
            Assert.True(context.TryGetPayload(out Payload? sourceUpdated));
            Assert.Same(updated, sourceUpdated);

            proxy.AddConsumeTask(Task.CompletedTask);
            Assert.Equal("messageType", Assert.Throws<ArgumentNullException>(() => proxy.HasMessageType(null!)).ParamName);
            Assert.Equal("task", Assert.Throws<ArgumentNullException>(() => proxy.AddConsumeTask(null!)).ParamName);
            Assert.Equal("payloadType", Assert.Throws<ArgumentNullException>(() => proxy.HasPayloadType(null!)).ParamName);
            Assert.Equal(
                "payloadFactory",
                Assert.Throws<ArgumentNullException>(() => proxy.GetOrAddPayload<Payload>(null!)).ParamName);
            Assert.Equal(
                "addFactory",
                Assert.Throws<ArgumentNullException>(() => proxy.AddOrUpdatePayload<Payload>(null!, value => value)).ParamName);
            Assert.Equal(
                "updateFactory",
                Assert.Throws<ArgumentNullException>(() => proxy.AddOrUpdatePayload(() => sourcePayload, null!)).ParamName);

            assertionsCompleted.TrySetResult();
            return Task.CompletedTask;
        });

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.Bus.PublishAsync(new FlowMessage("value"), cancellationToken);
            await assertionsCompleted.Task.WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(InvalidTypedLookupResult.MissingContext)]
    [InlineData(InvalidTypedLookupResult.MissingMessage)]
    [RequirementCoverage("REQ-VSB-CONSUME-CONTEXT-PROXY", "successful-typed-lookup-requires-context")]
    public void SuccessfulTypedLookup_RejectsInvalidSourceResult(InvalidTypedLookupResult result)
    {
        IPublishEndpointProvider publishEndpointProvider =
            DispatchProxy.Create<IPublishEndpointProvider, UnexpectedInvocationProxy>();
        ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, MinimalReceiveContextProxy>();
        ((MinimalReceiveContextProxy)(object)receiveContext).PublishEndpointProvider = publishEndpointProvider;
        SerializerContext serializerContext = DispatchProxy.Create<SerializerContext, UnexpectedInvocationProxy>();
        AdvancedProbeConsumeContext source =
            DispatchProxy.Create<AdvancedProbeConsumeContext, InvalidTypedLookupContextProxy>();
        var sourceProxy = (InvalidTypedLookupContextProxy)(object)source;
        sourceProxy.ReceiveContext = receiveContext;
        sourceProxy.SerializerContext = serializerContext;
        sourceProxy.ReturnedContext = result switch
        {
            InvalidTypedLookupResult.MissingContext => null,
            InvalidTypedLookupResult.MissingMessage =>
                DispatchProxy.Create<ConsumeContext<OtherMessage>, NullMessageContextProxy>(),
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, null),
        };
        var context = new ConsumeContextProxy<ProbeMessage>(source);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => context.TryGetMessage<OtherMessage>(out _));

        Assert.Equal(
            result == InvalidTypedLookupResult.MissingContext
                ? "The source consume context reported a successful 'OtherMessage' lookup without returning a context."
                : "The source consume context returned a 'OtherMessage' context without a message.",
            exception.Message);
        Assert.Equal(1, sourceProxy.TryGetMessageInvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-CONTEXT-PROXY", "typed-notifications-preserve-all-arguments")]
    public void TypedNotifications_PreserveEverySourceArgument()
    {
        IPublishEndpointProvider publishEndpointProvider =
            DispatchProxy.Create<IPublishEndpointProvider, UnexpectedInvocationProxy>();
        ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, MinimalReceiveContextProxy>();
        ((MinimalReceiveContextProxy)(object)receiveContext).PublishEndpointProvider = publishEndpointProvider;
        SerializerContext serializerContext = DispatchProxy.Create<SerializerContext, UnexpectedInvocationProxy>();
        AdvancedProbeConsumeContext source =
            DispatchProxy.Create<AdvancedProbeConsumeContext, InvalidTypedLookupContextProxy>();
        var sourceProxy = (InvalidTypedLookupContextProxy)(object)source;
        sourceProxy.ReceiveContext = receiveContext;
        sourceProxy.SerializerContext = serializerContext;
        var context = new ConsumeContextProxy<ProbeMessage>(source);
        var failure = new ExpectedProxyFailure();
        TimeSpan consumedDuration = TimeSpan.FromMilliseconds(11);
        TimeSpan faultedDuration = TimeSpan.FromMilliseconds(13);
        using var cancellation = new CancellationTokenSource();

        Assert.Same(
            sourceProxy.NotificationTask,
            context.NotifyConsumedAsync(consumedDuration, "consumer", cancellation.Token));
        Assert.Same(
            sourceProxy.NotificationTask,
            context.NotifyFaultedAsync(faultedDuration, "consumer", failure, cancellation.Token));

        Assert.Collection(
            sourceProxy.NotificationInvocations,
            invocation =>
            {
                Assert.Equal(nameof(ConsumeContext.NotifyConsumedAsync), invocation.MethodName);
                Assert.Same(context, invocation.Arguments[0]);
                Assert.Equal(consumedDuration, invocation.Arguments[1]);
                Assert.Equal("consumer", invocation.Arguments[2]);
                Assert.Equal(cancellation.Token, invocation.Arguments[3]);
            },
            invocation =>
            {
                Assert.Equal(nameof(ConsumeContext.NotifyFaultedAsync), invocation.MethodName);
                Assert.Same(context, invocation.Arguments[0]);
                Assert.Equal(faultedDuration, invocation.Arguments[1]);
                Assert.Equal("consumer", invocation.Arguments[2]);
                Assert.Same(failure, invocation.Arguments[3]);
                Assert.Equal(cancellation.Token, invocation.Arguments[4]);
            });
    }

    public enum InvalidTypedLookupResult
    {
        MissingContext,
        MissingMessage,
    }

    private interface AdvancedProbeConsumeContext :
        ConsumeContext<ProbeMessage>,
        ConsumeContext;

    private sealed record ProbeMessage(string Value);

    private sealed record OtherMessage(string Value);

    private sealed record FlowMessage(string Value);

    private sealed record MissingFlowMessage(string Value);

    private sealed record Payload(string Value);

    private sealed record NotificationInvocation(string MethodName, object?[] Arguments);

    private sealed class ExpectedProxyFailure : Exception;

    private class InvalidTypedLookupContextProxy : DispatchProxy
    {
        public ReceiveContext ReceiveContext { get; set; } = null!;

        public SerializerContext SerializerContext { get; set; } = null!;

        public object? ReturnedContext { get; set; }

        public int TryGetMessageInvocationCount { get; private set; }

        public Task NotificationTask { get; } = Task.CompletedTask;

        public List<NotificationInvocation> NotificationInvocations { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_ReceiveContext" => ReceiveContext,
                "get_SerializerContext" => SerializerContext,
                nameof(ConsumeContext.TryGetMessage) => ReturnInvalidSuccess(args),
                nameof(ConsumeContext.NotifyConsumedAsync) or nameof(ConsumeContext.NotifyFaultedAsync) =>
                    RecordNotification(targetMethod.Name, args),
                _ => throw new InvalidOperationException(
                    $"The invalid typed lookup source unexpectedly invoked {targetMethod?.Name}."),
            };
        }

        private object ReturnInvalidSuccess(object?[]? args)
        {
            TryGetMessageInvocationCount++;
            args![0] = ReturnedContext;
            return true;
        }

        private object RecordNotification(string methodName, object?[]? args)
        {
            NotificationInvocations.Add(new NotificationInvocation(methodName, args?.ToArray() ?? []));
            return NotificationTask;
        }
    }

    private class NullMessageContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_Message"
                ? null
                : throw new InvalidOperationException(
                    $"The null-message context unexpectedly invoked {targetMethod?.Name}.");
    }

    private class MinimalReceiveContextProxy : DispatchProxy
    {
        public IPublishEndpointProvider PublishEndpointProvider { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_PublishEndpointProvider"
                ? PublishEndpointProvider
                : throw new InvalidOperationException(
                    $"The minimal receive context unexpectedly invoked {targetMethod?.Name}.");
    }

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The proxy unexpectedly invoked {targetMethod?.Name}.");
    }
}
