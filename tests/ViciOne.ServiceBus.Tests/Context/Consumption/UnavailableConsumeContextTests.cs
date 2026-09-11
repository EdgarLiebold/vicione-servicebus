using System.Reflection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Context.Consumption;

public sealed class UnavailableConsumeContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MISSING-CONSUME-CONTEXT", "internal-singleton-identity")]
    public void Instance_IsTheSingleInternalUnavailableContext()
    {
        UnavailableConsumeContext context = Assert.IsType<UnavailableConsumeContext>(UnavailableConsumeContext.Instance);

        Assert.False(typeof(UnavailableConsumeContext).IsPublic);
        Assert.Empty(typeof(UnavailableConsumeContext).GetConstructors());
        Assert.Same(context, UnavailableConsumeContext.Instance);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MISSING-CONSUME-CONTEXT", "every-synchronous-member-has-one-domain-failure")]
    public void EverySynchronousMember_ThrowsTheDomainUnavailableException()
    {
        ConsumeContext context = UnavailableConsumeContext.Instance;
        var message = new TestMessage();
        var values = new { Value = "response" };
        IPipe<SendContext<TestMessage>> typedSendPipe = CreateProxy<IPipe<SendContext<TestMessage>>>();
        IPipe<SendContext> sendPipe = CreateProxy<IPipe<SendContext>>();
        IPublishObserver publishObserver = CreateProxy<IPublishObserver>();
        ISendObserver sendObserver = CreateProxy<ISendObserver>();
        Action[] operations =
        [
            () => _ = context.CancellationToken,
            () => _ = context.MessageId,
            () => _ = context.RequestId,
            () => _ = context.CorrelationId,
            () => _ = context.ConversationId,
            () => _ = context.InitiatorId,
            () => _ = context.ExpirationTime,
            () => _ = context.SourceAddress,
            () => _ = context.DestinationAddress,
            () => _ = context.ResponseAddress,
            () => _ = context.FaultAddress,
            () => _ = context.SentTime,
            () => _ = context.Headers,
            () => _ = context.Host,
            () => _ = context.ReceiveContext,
            () => _ = context.SerializerContext,
            () => _ = context.ConsumeCompleted,
            () => _ = context.Outgoing,
            () => _ = context.SupportedMessageTypes,
            () => _ = context.HasPayloadType(typeof(TestPayload)),
            () => context.TryGetPayload<TestPayload>(out _),
            () => _ = context.GetOrAddPayload(() => new TestPayload()),
            () => _ = context.AddOrUpdatePayload(() => new TestPayload(), current => current),
            () => _ = context.ConnectPublishObserver(publishObserver),
            () => _ = context.ConnectSendObserver(sendObserver),
            () => _ = context.HasMessageType(typeof(TestMessage)),
            () => context.TryGetMessage<TestMessage>(out _),
            () => context.AddConsumeTask(Task.CompletedTask),
            () => _ = context.RespondAsync(message),
            () => _ = context.RespondAsync(message, new SendOptions()),
            () => _ = context.RespondAsync(message, typedSendPipe),
            () => _ = context.RespondAsync(message, sendPipe),
            () => _ = context.RespondAsync((object)message),
            () => _ = context.RespondAsync((object)message, typeof(TestMessage)),
            () => _ = context.RespondAsync((object)message, sendPipe),
            () => _ = context.RespondAsync((object)message, typeof(TestMessage), sendPipe),
            () => _ = context.RespondAsync<TestMessage>(values),
            () => _ = context.RespondAsync<TestMessage>(values, typedSendPipe),
            () => _ = context.RespondAsync<TestMessage>(values, sendPipe),
            () => context.DeferResponse(message),
        ];

        Assert.All(operations, operation => Assert.Throws<ConsumeContextNotAvailableException>(operation));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MISSING-CONSUME-CONTEXT", "every-token-operation-fails-unavailable-when-active")]
    public void EveryTokenBearingOperation_ThrowsUnavailableWhenTheTokenIsActive()
    {
        CancellationToken token = TestContext.Current.CancellationToken;

        Assert.All(CreateTokenOperations(token), operation =>
            Assert.Throws<ConsumeContextNotAvailableException>(() =>
            {
                _ = operation();
            }));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MISSING-CONSUME-CONTEXT", "every-token-operation-preserves-pre-cancellation")]
    public async Task EveryTokenBearingOperation_PreservesPreCancellationAsync()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        foreach (Func<Task> operation in CreateTokenOperations(cancellationSource.Token))
        {
            Task canceledTask = operation();
            OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledTask);
            Assert.Equal(cancellationSource.Token, canceled.CancellationToken);
        }
    }

    private static Func<Task>[] CreateTokenOperations(CancellationToken token)
    {
        UnavailableConsumeContext context = Assert.IsType<UnavailableConsumeContext>(UnavailableConsumeContext.Instance);
        var message = new TestMessage();
        object values = new { Value = "publish" };
        IPipe<PublishContext<TestMessage>> typedPublishPipe = CreateProxy<IPipe<PublishContext<TestMessage>>>();
        IPipe<PublishContext> publishPipe = CreateProxy<IPipe<PublishContext>>();
        ConsumeContext<TestMessage> messageContext = CreateProxy<ConsumeContext<TestMessage>>();
        var failure = new InvalidOperationException("failed");

        return
        [
            () => context.PublishAsync(message, token),
            () => context.PublishAsync(message, typedPublishPipe, token),
            () => context.PublishAsync(message, publishPipe, token),
            () => context.PublishAsync((object)message, token),
            () => context.PublishAsync((object)message, publishPipe, token),
            () => context.PublishAsync((object)message, typeof(TestMessage), token),
            () => context.PublishAsync((object)message, typeof(TestMessage), publishPipe, token),
            () => context.PublishAsync<TestMessage>(values, token),
            () => context.PublishAsync<TestMessage>(values, typedPublishPipe, token),
            () => context.PublishAsync<TestMessage>(values, publishPipe, token),
            () => context.GetSendEndpointAsync(new Uri("loopback://unavailable/destination"), token),
            () => context.NotifyConsumedAsync(messageContext, TimeSpan.FromMilliseconds(3), "Consumer", token),
            () => context.NotifyFaultedAsync(messageContext, TimeSpan.FromMilliseconds(5), "Consumer", failure, token),
        ];
    }

    private static TContract CreateProxy<TContract>()
        where TContract : class => DispatchProxy.Create<TContract, UnusedProxy>();

    private class UnusedProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }

    private sealed record TestMessage;

    private sealed record TestPayload;
}
