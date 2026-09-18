using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Context;

public sealed class SagaConsumeContextProxyDeepContractTests
{
    [Theory]
    [InlineData(MissingCollaborator.MessageContext, "context")]
    [InlineData(MissingCollaborator.SagaContext, "sagaContext")]
    [RequirementCoverage("REQ-VSB-SAGA-CONSUME-CONTEXT-PROXY", "construction-requires-both-context-owners")]
    public void Construction_RejectsEitherMissingContextOwner(MissingCollaborator missing, string parameterName)
    {
        AdvancedMessageContext messageContext = CreateMessageContext(
            out _,
            cancellationToken: TestContext.Current.CancellationToken);
        SagaConsumeContext<TestSaga, TestMessage> sagaContext = CreateSagaContext(out _);

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new SagaConsumeContextProxy<TestSaga, TestMessage>(
                missing == MissingCollaborator.MessageContext ? null! : messageContext,
                missing == MissingCollaborator.SagaContext ? null! : sagaContext));

        Assert.Equal(parameterName, exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONSUME-CONTEXT-PROXY", "message-saga-correlation-token-and-payload-ownership")]
    public void Proxy_PreservesTheExactMessageSagaDeliveryAndPayloadOwners()
    {
        using var deliveryCancellation = new CancellationTokenSource();
        var message = new TestMessage("message-owner");
        var messagePayload = new TestPayload("message-payload");
        Guid messageCorrelationId = Guid.NewGuid();
        AdvancedMessageContext messageContext = CreateMessageContext(
            out MessageContextProxy messageSource,
            message,
            deliveryCancellation.Token,
            messageCorrelationId,
            messagePayload);
        var saga = new TestSaga(Guid.NewGuid());
        SagaConsumeContext<TestSaga, TestMessage> sagaContext = CreateSagaContext(out _, saga);
        var proxy = new SagaConsumeContextProxy<TestSaga, TestMessage>(messageContext, sagaContext);

        Assert.Same(message, proxy.Message);
        Assert.Same(saga, proxy.Saga);
        Assert.Equal(saga.CorrelationId, proxy.CorrelationId);
        Assert.NotEqual(messageCorrelationId, proxy.CorrelationId);
        Assert.Equal(deliveryCancellation.Token, proxy.CancellationToken);

        Assert.True(proxy.TryGetPayload(out TestPayload? selectedPayload));
        Assert.Same(messagePayload, selectedPayload);
        Assert.True(proxy.TryGetPayload(out SagaConsumeContext<TestSaga, TestMessage>? selectedContext));
        Assert.Same(proxy, selectedContext);
        Assert.Equal(1, messageSource.PayloadLookupCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONSUME-CONTEXT-PROXY", "completion-forwards-token-task-and-live-state")]
    public void Completion_ForwardsTheExactTokenTaskAndLiveState()
    {
        AdvancedMessageContext messageContext = CreateMessageContext(
            out _,
            cancellationToken: TestContext.Current.CancellationToken);
        SagaConsumeContext<TestSaga, TestMessage> sagaContext = CreateSagaContext(out SagaContextProxy sagaSource);
        var proxy = new SagaConsumeContextProxy<TestSaga, TestMessage>(messageContext, sagaContext);
        using var cancellation = new CancellationTokenSource();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        sagaSource.CompletionTask = completion.Task;

        Assert.False(proxy.IsCompleted);

        Task returned = proxy.SetCompletedAsync(cancellation.Token);

        Assert.Same(completion.Task, returned);
        Assert.Equal(cancellation.Token, sagaSource.CompletionToken);
        Assert.Equal(1, sagaSource.CompletionInvocationCount);

        sagaSource.IsCompleted = true;
        Assert.True(proxy.IsCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONSUME-CONTEXT-PROXY", "completion-preserves-failure-identity")]
    public async Task Completion_PreservesTheExactFailureAsync()
    {
        AdvancedMessageContext messageContext = CreateMessageContext(
            out _,
            cancellationToken: TestContext.Current.CancellationToken);
        SagaConsumeContext<TestSaga, TestMessage> sagaContext = CreateSagaContext(out SagaContextProxy sagaSource);
        var proxy = new SagaConsumeContextProxy<TestSaga, TestMessage>(messageContext, sagaContext);
        var failure = new ExpectedCompletionFailure();
        Task completion = Task.FromException(failure);
        sagaSource.CompletionTask = completion;

        Task returned = proxy.SetCompletedAsync(TestContext.Current.CancellationToken);

        Assert.Same(completion, returned);
        ExpectedCompletionFailure observed = await Assert.ThrowsAsync<ExpectedCompletionFailure>(() => returned);
        Assert.Same(failure, observed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONSUME-CONTEXT-PROXY", "completion-preserves-cancellation-token")]
    public async Task Completion_PreservesTheExactCancellationAsync()
    {
        AdvancedMessageContext messageContext = CreateMessageContext(
            out _,
            cancellationToken: TestContext.Current.CancellationToken);
        SagaConsumeContext<TestSaga, TestMessage> sagaContext = CreateSagaContext(out SagaContextProxy sagaSource);
        var proxy = new SagaConsumeContextProxy<TestSaga, TestMessage>(messageContext, sagaContext);
        using var requestedCancellation = new CancellationTokenSource();
        using var resultCancellation = new CancellationTokenSource();
        resultCancellation.Cancel();
        Task completion = Task.FromCanceled(resultCancellation.Token);
        sagaSource.CompletionTask = completion;

        Task returned = proxy.SetCompletedAsync(requestedCancellation.Token);

        Assert.Same(completion, returned);
        Assert.Equal(requestedCancellation.Token, sagaSource.CompletionToken);
        OperationCanceledException observed = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => returned);
        Assert.Equal(resultCancellation.Token, observed.CancellationToken);
    }

    private static AdvancedMessageContext CreateMessageContext(
        out MessageContextProxy source,
        TestMessage? message = null,
        CancellationToken cancellationToken = default,
        Guid? correlationId = null,
        TestPayload? payload = null)
    {
        AdvancedMessageContext context = DispatchProxy.Create<AdvancedMessageContext, MessageContextProxy>();
        source = (MessageContextProxy)(object)context;
        source.Message = message ?? new TestMessage("message");
        source.CancellationToken = cancellationToken;
        source.CorrelationId = correlationId;
        source.Payload = payload;
        source.ReceiveContext = CreateReceiveContext();
        source.SerializerContext = DispatchProxy.Create<SerializerContext, UnexpectedInvocationProxy>();
        return context;
    }

    private static SagaConsumeContext<TestSaga, TestMessage> CreateSagaContext(
        out SagaContextProxy source,
        TestSaga? saga = null)
    {
        SagaConsumeContext<TestSaga, TestMessage> context =
            DispatchProxy.Create<SagaConsumeContext<TestSaga, TestMessage>, SagaContextProxy>();
        source = (SagaContextProxy)(object)context;
        source.Saga = saga ?? new TestSaga(Guid.NewGuid());
        return context;
    }

    private static ReceiveContext CreateReceiveContext()
    {
        ReceiveContext context = DispatchProxy.Create<ReceiveContext, ReceiveContextProxy>();
        var source = (ReceiveContextProxy)(object)context;
        source.PublishEndpointProvider = DispatchProxy.Create<IPublishEndpointProvider, UnexpectedInvocationProxy>();
        return context;
    }

    public enum MissingCollaborator
    {
        MessageContext,
        SagaContext,
    }

    private interface AdvancedMessageContext :
        ConsumeContext<TestMessage>,
        ConsumeContext;

    private sealed record TestMessage(string Value);

    private sealed record TestPayload(string Value);

    private sealed class TestSaga(Guid correlationId) : ISaga
    {
        public Guid CorrelationId { get; set; } = correlationId;
    }

    private sealed class ExpectedCompletionFailure : Exception;

    private class MessageContextProxy : DispatchProxy
    {
        public TestMessage Message { get; set; } = null!;

        public CancellationToken CancellationToken { get; set; }

        public Guid? CorrelationId { get; set; }

        public TestPayload? Payload { get; set; }

        public ReceiveContext ReceiveContext { get; set; } = null!;

        public SerializerContext SerializerContext { get; set; } = null!;

        public int PayloadLookupCount { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_Message" => Message,
                "get_CancellationToken" => CancellationToken,
                "get_CorrelationId" => CorrelationId,
                "get_ReceiveContext" => ReceiveContext,
                "get_SerializerContext" => SerializerContext,
                nameof(PipeContext.TryGetPayload) => TryGetPayload(args),
                _ => throw new InvalidOperationException(
                    $"The message context unexpectedly invoked {targetMethod?.Name}."),
            };
        }

        private object TryGetPayload(object?[]? args)
        {
            PayloadLookupCount++;
            Type payloadType = args![0]?.GetType() ?? typeof(TestPayload);
            bool found = Payload is not null && payloadType.IsInstanceOfType(Payload);
            args[0] = found ? Payload : null;
            return found;
        }
    }

    private class SagaContextProxy : DispatchProxy
    {
        public TestSaga Saga { get; set; } = null!;

        public bool IsCompleted { get; set; }

        public Task CompletionTask { get; set; } = Task.CompletedTask;

        public CancellationToken CompletionToken { get; private set; }

        public int CompletionInvocationCount { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_Saga" => Saga,
                "get_IsCompleted" => IsCompleted,
                nameof(SagaConsumeContext<TestSaga>.SetCompletedAsync) => Complete(args),
                _ => throw new InvalidOperationException(
                    $"The saga context unexpectedly invoked {targetMethod?.Name}."),
            };
        }

        private object Complete(object?[]? args)
        {
            CompletionInvocationCount++;
            CompletionToken = Assert.IsType<CancellationToken>(args![0]);
            return CompletionTask;
        }
    }

    private class ReceiveContextProxy : DispatchProxy
    {
        public IPublishEndpointProvider PublishEndpointProvider { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_PublishEndpointProvider"
                ? PublishEndpointProvider
                : throw new InvalidOperationException(
                    $"The receive context unexpectedly invoked {targetMethod?.Name}.");
    }

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The proxy unexpectedly invoked {targetMethod?.Name}.");
    }
}
