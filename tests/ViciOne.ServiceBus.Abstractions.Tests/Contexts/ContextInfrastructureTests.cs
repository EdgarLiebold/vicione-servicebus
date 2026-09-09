using System.Reflection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Payloads;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Contexts;

public sealed class ContextInfrastructureTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-DISPATCH", "closed-reference-contract-boundary")]
    public void RuntimeDispatchers_AreStaticAndRejectInvalidContractTypes()
    {
        ISendEndpoint sendEndpoint = CreateProxy<ISendEndpoint>(out _);
        IPublishEndpoint publishEndpoint = CreateProxy<IPublishEndpoint>(out _);
        ConsumeContext consumeContext = CreateProxy<ConsumeContext>(out _);
        var message = new TestMessage();
        CancellationToken testCancellation = TestContext.Current.CancellationToken;

        AssertStatic(typeof(SendEndpointConverterCache));
        AssertStatic(typeof(PublishEndpointConverterCache));
        AssertStatic(typeof(ResponseEndpointConverterCache));
        Assert.Equal(
            "messageType",
            Assert.Throws<ArgumentException>(() =>
            {
                _ = SendEndpointConverterCache.SendAsync(sendEndpoint, message, typeof(int), testCancellation);
            }).ParamName);
        Assert.Equal(
            "messageType",
            Assert.Throws<ArgumentException>(() =>
            {
                _ = PublishEndpointConverterCache.PublishAsync(publishEndpoint, message, typeof(List<>), testCancellation);
            }).ParamName);
        Assert.Equal(
            "messageType",
            Assert.Throws<ArgumentNullException>(() =>
            {
                _ = ResponseEndpointConverterCache.RespondAsync(consumeContext, message, null!);
            }).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-DISPATCH", "explicit-contract-and-cancellation")]
    public async Task RuntimeDispatchers_UseTheExplicitContractAndPreserveCancellationAsync()
    {
        ISendEndpoint sendEndpoint = CreateProxy<ISendEndpoint>(out RecordingProxy sendRecorder);
        IPublishEndpoint publishEndpoint = CreateProxy<IPublishEndpoint>(out RecordingProxy publishRecorder);
        ConsumeContext consumeContext = CreateProxy<ConsumeContext>(out RecordingProxy responseRecorder);
        var message = new DerivedMessage();
        using var cancellationSource = new CancellationTokenSource();

        await SendEndpointConverterCache.SendAsync(sendEndpoint, message, typeof(TestMessage), cancellationSource.Token);
        await PublishEndpointConverterCache.PublishAsync(publishEndpoint, message, typeof(TestMessage), cancellationSource.Token);
        await ResponseEndpointConverterCache.RespondAsync(consumeContext, message, typeof(TestMessage));

        Invocation send = Assert.Single(sendRecorder.Invocations);
        Assert.Equal(nameof(ISendEndpoint.SendAsync), send.Method.Name);
        Assert.Equal(typeof(TestMessage), Assert.Single(send.Method.GetGenericArguments()));
        Assert.Equal(cancellationSource.Token, Assert.IsType<CancellationToken>(send.Arguments[1]));

        Invocation publish = Assert.Single(publishRecorder.Invocations);
        Assert.Equal(nameof(IPublishEndpoint.PublishAsync), publish.Method.Name);
        Assert.Equal(typeof(TestMessage), Assert.Single(publish.Method.GetGenericArguments()));
        Assert.Equal(cancellationSource.Token, Assert.IsType<CancellationToken>(publish.Arguments[1]));

        Invocation response = Assert.Single(responseRecorder.Invocations);
        Assert.Equal(nameof(ConsumeContext.RespondAsync), response.Method.Name);
        Assert.Equal(typeof(TestMessage), Assert.Single(response.Method.GetGenericArguments()));
        Assert.Same(message, response.Arguments[0]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-DISPATCH", "required-inputs-and-runtime-assignability")]
    public void RuntimeDispatchers_RejectMissingAndIncompatibleInputsBeforeInvocation()
    {
        ISendEndpoint sendEndpoint = CreateProxy<ISendEndpoint>(out RecordingProxy sendRecorder);
        IPublishEndpoint publishEndpoint = CreateProxy<IPublishEndpoint>(out RecordingProxy publishRecorder);
        ConsumeContext consumeContext = CreateProxy<ConsumeContext>(out RecordingProxy responseRecorder);
        var message = new TestMessage();
        CancellationToken testCancellation = TestContext.Current.CancellationToken;

        Assert.Equal(
            "endpoint",
            Assert.Throws<ArgumentNullException>(() =>
            {
                _ = SendEndpointConverterCache.SendAsync(null!, message, typeof(TestMessage), testCancellation);
            }).ParamName);
        Assert.Equal(
            "message",
            Assert.Throws<ArgumentNullException>(() =>
            {
                _ = PublishEndpointConverterCache.PublishAsync(publishEndpoint, null!, typeof(TestMessage), testCancellation);
            }).ParamName);
        Assert.Equal(
            "consumeContext",
            Assert.Throws<ArgumentNullException>(() =>
            {
                _ = ResponseEndpointConverterCache.RespondAsync(null!, message, typeof(TestMessage));
            }).ParamName);
        Assert.Equal(
            "message",
            Assert.Throws<ArgumentException>(() =>
            {
                _ = SendEndpointConverterCache.SendAsync(sendEndpoint, new OtherMessage(), typeof(TestMessage), testCancellation);
            }).ParamName);
        Assert.Equal(
            "message",
            Assert.Throws<ArgumentException>(() =>
            {
                _ = PublishEndpointConverterCache.PublishAsync(publishEndpoint, new OtherMessage(), typeof(TestMessage), testCancellation);
            }).ParamName);
        Assert.Equal(
            "message",
            Assert.Throws<ArgumentException>(() =>
            {
                _ = ResponseEndpointConverterCache.RespondAsync(consumeContext, new OtherMessage(), typeof(TestMessage));
            }).ParamName);

        Assert.Empty(sendRecorder.Invocations);
        Assert.Empty(publishRecorder.Invocations);
        Assert.Empty(responseRecorder.Invocations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTEXT-PROXY", "required-context-and-message")]
    public void TypedContextProxies_RequireTheirContextAndMessage()
    {
        SendContext sendContext = CreateProxy<SendContext>(out _);
        PublishContext publishContext = CreateProxy<PublishContext>(out _);
        var message = new TestMessage();

        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => new SendContextProxy<TestMessage>(null!, message)).ParamName);
        Assert.Equal(
            "message",
            Assert.Throws<ArgumentNullException>(() => new SendContextProxy<TestMessage>(sendContext, null!)).ParamName);
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => new PublishContextProxy<TestMessage>(null!, message)).ParamName);
        Assert.Equal(
            "message",
            Assert.Throws<ArgumentNullException>(() => new PublishContextProxy<TestMessage>(publishContext, null!)).ParamName);

        var typedPublishContext = new PublishContextProxy<TestMessage>(publishContext, message);
        var otherMessage = new OtherMessage();
        SendContext<OtherMessage> converted = typedPublishContext.CreateProxy(otherMessage);

        Assert.Same(message, typedPublishContext.Message);
        Assert.Same(otherMessage, converted.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTEXT-SCOPE", "isolated-payload-boundaries")]
    public void SendContextScope_ValidatesAndIsolatesItsPayloads()
    {
        SendContext sendContext = CreateProxy<SendContext>(out _);
        var payload = new TestPayload("initial");
        var scope = new SendContextScope(sendContext, payload);

        Assert.True(scope.HasPayloadType(typeof(TestPayload)));
        Assert.True(scope.TryGetPayload(out TestPayload? resolved));
        Assert.Same(payload, resolved);
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => new SendContextScope(null!)).ParamName);
        Assert.Equal(
            "payloads",
            Assert.Throws<ArgumentNullException>(() => new SendContextScope(sendContext, (object[])null!)).ParamName);
        Assert.Equal(
            "payloads",
            Assert.Throws<ArgumentException>(() => new SendContextScope(sendContext, [null!])).ParamName);
        Assert.Equal(
            "payloadType",
            Assert.Throws<ArgumentNullException>(() => scope.HasPayloadType(null!)).ParamName);
        Assert.Equal(
            "payloadFactory",
            Assert.Throws<ArgumentNullException>(() => scope.GetOrAddPayload<TestPayload>(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MISSING-CONSUME-CONTEXT", "singleton-and-diagnostic-failure")]
    public async Task MissingConsumeContext_IsASingletonWithConsistentFailureAndCancellationAsync()
    {
        ConsumeContext context = MissingConsumeContext.Instance;
        var message = new TestMessage();

        Assert.Empty(typeof(MissingConsumeContext).GetConstructors());
        Assert.Same(context, MissingConsumeContext.Instance);
        Assert.Throws<ConsumeContextNotAvailableException>(() => _ = context.MessageId);
        Assert.Throws<ConsumeContextNotAvailableException>(() =>
        {
            _ = context.PublishAsync(message, TestContext.Current.CancellationToken);
        });

        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        Task canceledTask = context.PublishAsync(message, cancellationSource.Token);
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledTask);

        Assert.Equal(cancellationSource.Token, canceled.CancellationToken);
    }

    private static TContract CreateProxy<TContract>(out RecordingProxy recorder)
        where TContract : class
    {
        TContract contract = DispatchProxy.Create<TContract, RecordingProxy>();
        recorder = (RecordingProxy)(object)contract;
        return contract;
    }

    private static void AssertStatic(Type type)
    {
        Assert.True(type.IsAbstract);
        Assert.True(type.IsSealed);
        Assert.Empty(type.GetConstructors());
    }

    private sealed record Invocation(MethodInfo Method, object?[] Arguments);

    private class RecordingProxy : DispatchProxy
    {
        public List<Invocation> Invocations { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            object?[] arguments = args ?? [];
            Invocations.Add(new Invocation(targetMethod, arguments));

            if (targetMethod.ReturnType == typeof(Task))
                return Task.CompletedTask;

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private record TestMessage;

    private sealed record DerivedMessage : TestMessage;

    private sealed record OtherMessage;

    private sealed record TestPayload(string Value);
}
