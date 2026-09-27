using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests.AzureServiceBusTransport;

public sealed class ServiceBusReceiveLockContextTests
{
    private static readonly DateTimeOffset Now = new(2044, 5, 6, 7, 8, 9, TimeSpan.Zero);
    private static readonly Uri InputAddress = new("sb://settlement-test.servicebus.windows.net/input");
    private static TimeSpan Timeout => TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;

    [Theory]
    [InlineData(FailureKind.Consumer, true)]
    [InlineData(FailureKind.ExpiredLock, false)]
    [InlineData(FailureKind.ExpiredMessage, false)]
    [InlineData(FailureKind.LostLock, false)]
    [InlineData(FailureKind.LostSession, false)]
    [InlineData(FailureKind.Communication, false)]
    [InlineData(FailureKind.Busy, true)]
    [InlineData(FailureKind.Timeout, true)]
    [RequirementCoverage("REQ-VSB-ASB-RECEIVE-SETTLEMENT", "failure-reason-selects-abandon-with-original-identity")]
    public async Task FaultSettlement_AbandonsOnlyRecoverableFailuresWithOriginalIdentityAsync(FailureKind kind, bool abandon)
    {
        using var caller = new CancellationTokenSource();
        var provider = new RecordingMessageLock();
        ServiceBusReceivedMessage message = CreateMessage(kind);
        var context = new ServiceBusReceiveLockContext(InputAddress, provider, message, new FixedClock());

        Exception failure;
        if (kind == FailureKind.ExpiredLock)
        {
            failure = await Assert.ThrowsAsync<MessageLockExpiredException>(() => context.ValidateLockStatusAsync(caller.Token));
            Assert.Contains(message.MessageId, failure.Message, StringComparison.Ordinal);
        }
        else if (kind == FailureKind.ExpiredMessage)
        {
            failure = await Assert.ThrowsAsync<MessageTimeToLiveExpiredException>(() => context.ValidateLockStatusAsync(caller.Token));
            Assert.Contains(message.MessageId, failure.Message, StringComparison.Ordinal);
        }
        else
        {
            await context.ValidateLockStatusAsync(caller.Token);
            failure = kind switch
            {
                FailureKind.Consumer => new InvalidOperationException("consumer failed"),
                FailureKind.LostLock => new ServiceBusException("lock lost", ServiceBusFailureReason.MessageLockLost),
                FailureKind.LostSession => new ServiceBusException("session lost", ServiceBusFailureReason.SessionLockLost),
                FailureKind.Communication => new ServiceBusException("connection lost", ServiceBusFailureReason.ServiceCommunicationProblem),
                FailureKind.Busy => new ServiceBusException("broker busy", ServiceBusFailureReason.ServiceBusy),
                FailureKind.Timeout => new ServiceBusException("broker timeout", ServiceBusFailureReason.ServiceTimeout),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }

        await context.FaultedAsync(failure, caller.Token).WaitAsync(Timeout, TestContext.Current.CancellationToken);

        Assert.Equal(abandon ? 1 : 0, provider.AbandonCount);
        if (abandon)
        {
            Assert.Same(failure, provider.Failure);
            Assert.Equal(caller.Token, provider.Token);
        }
        else
        {
            Assert.Null(provider.Failure);
            Assert.Equal(CancellationToken.None, provider.Token);
        }
        Assert.Equal(0, provider.CompleteCount);
        Assert.Equal(0, provider.DeadLetterCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASB-RECEIVE-SETTLEMENT", "abandon-awaited-with-provider-failure-containment")]
    public async Task FaultSettlement_AwaitsAbandonAndContainsProviderFailureAsync(bool providerFails)
    {
        using var caller = new CancellationTokenSource();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var providerFailure = new InvalidOperationException("abandon rejected by provider");
        var provider = new RecordingMessageLock
        {
            Abandon = async () =>
            {
                entered.TrySetResult();
                await release.Task;
                if (providerFails)
                    throw providerFailure;
            }
        };
        var context = new ServiceBusReceiveLockContext(InputAddress, provider, CreateMessage(FailureKind.Consumer), new FixedClock());
        var consumerFailure = new InvalidOperationException("consumer failed before abandonment");
        Task settlement = context.FaultedAsync(consumerFailure, caller.Token);

        try
        {
            await entered.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Assert.False(settlement.IsCompleted);
            Assert.Same(consumerFailure, provider.Failure);
            Assert.Equal(caller.Token, provider.Token);
            Assert.Equal(1, provider.AbandonCount);
        }
        finally
        {
            release.TrySetResult();
            await settlement.WaitAsync(Timeout, CancellationToken.None);
        }

        Assert.Equal(TaskStatus.RanToCompletion, settlement.Status);
        Assert.Same(consumerFailure, provider.Failure);
        Assert.Equal(1, provider.AbandonCount);
        Assert.Equal(0, provider.CompleteCount);
        Assert.Equal(0, provider.DeadLetterCount);
    }

    private static ServiceBusReceivedMessage CreateMessage(FailureKind kind)
    {
        DateTimeOffset enqueuedAt = Now.AddHours(-2);
        DateTimeOffset expiresAt = kind == FailureKind.ExpiredMessage ? Now.AddTicks(-1) : Now.AddHours(1);
        return ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            messageId: "settlement-message",
            timeToLive: expiresAt - enqueuedAt,
            lockedUntil: kind == FailureKind.ExpiredLock ? Now : Now.AddMinutes(30),
            enqueuedTime: enqueuedAt);
    }

    public enum FailureKind
    {
        Consumer,
        ExpiredLock,
        ExpiredMessage,
        LostLock,
        LostSession,
        Communication,
        Busy,
        Timeout
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class RecordingMessageLock : MessageLockContext
    {
        public Func<Task> Abandon { get; init; } = () => Task.CompletedTask;
        public int AbandonCount { get; private set; }
        public int CompleteCount { get; private set; }
        public int DeadLetterCount { get; private set; }
        public Exception? Failure { get; private set; }
        public CancellationToken Token { get; private set; }

        public Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            CompleteCount++;
            return Task.CompletedTask;
        }

        public Task AbandonAsync(Exception exception, CancellationToken cancellationToken = default)
        {
            AbandonCount++;
            Failure = exception;
            Token = cancellationToken;
            return Abandon();
        }

        public Task DeadLetterAsync(CancellationToken cancellationToken = default)
        {
            DeadLetterCount++;
            return Task.CompletedTask;
        }

        public Task DeadLetterAsync(Exception exception, CancellationToken cancellationToken = default)
        {
            DeadLetterCount++;
            return Task.CompletedTask;
        }
    }
}
