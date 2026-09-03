using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.AzureServiceBusTransport;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests;

public sealed class ServiceBusTimeProviderTests
{
    private static readonly DateTimeOffset Now = new(2044, 5, 6, 7, 8, 9, TimeSpan.Zero);
    private static readonly Uri InputAddress = new("sb://clock-test.servicebus.windows.net/input");

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TIME-SOURCE", "relative-send-metadata-uses-context-clock")]
    public void RelativeSendMetadata_UsesTheContextClockForSetAndGet()
    {
        var clock = new MutableTimeProvider(Now);
        var context = new AzureServiceBusSendContext<ClockMessage>(new ClockMessage("payload"), CancellationToken.None);
        context.SetTimeProvider(clock);

        context.Delay = TimeSpan.FromMinutes(15);

        Assert.Equal(Now.UtcDateTime.AddMinutes(15), context.ScheduledEnqueueTimeUtc);
        Assert.Equal(TimeSpan.FromMinutes(15), context.Delay);

        clock.Advance(TimeSpan.FromMinutes(4));
        Assert.Equal(TimeSpan.FromMinutes(11), context.Delay);

        context.SetScheduledEnqueueTime(TimeSpan.FromMinutes(2));
        Assert.Equal(Now.UtcDateTime.AddMinutes(6), context.ScheduledEnqueueTimeUtc);

        context.Delay = TimeSpan.Zero;
        Assert.Null(context.ScheduledEnqueueTimeUtc);
        Assert.Equal(TimeSpan.Zero, context.Delay);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TIME-SOURCE", "lock-and-ttl-exact-clock-boundaries")]
    public async Task ReceiveLockValidation_UsesTheInjectedClockAtExactBoundaries()
    {
        var clock = new MutableTimeProvider(Now);
        var lockContext = new StubMessageLockContext();
        ServiceBusReceivedMessage lockExpired = CreateMessage(
            lockedUntil: Now,
            expiresAt: Now.AddHours(1));
        var expiredLock = new ServiceBusReceiveLockContext(InputAddress, lockContext, lockExpired, clock);

        MessageLockExpiredException lockException = await Assert.ThrowsAsync<MessageLockExpiredException>(expiredLock.ValidateLockStatus);
        Assert.Contains("clock-message", lockException.Message, StringComparison.Ordinal);

        ServiceBusReceivedMessage ttlBoundary = CreateMessage(
            lockedUntil: Now.AddHours(1),
            expiresAt: Now);
        var activeAtBoundary = new ServiceBusReceiveLockContext(InputAddress, lockContext, ttlBoundary, clock);

        await activeAtBoundary.ValidateLockStatus();

        clock.Advance(TimeSpan.FromTicks(1));
        MessageTimeToLiveExpiredException ttlException =
            await Assert.ThrowsAsync<MessageTimeToLiveExpiredException>(activeAtBoundary.ValidateLockStatus);
        Assert.Contains("clock-message", ttlException.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TIME-SOURCE", "receive-lock-rejects-null-clock")]
    public void ReceiveLockConstruction_RejectsANullClock()
    {
        ServiceBusReceivedMessage message = CreateMessage(Now.AddMinutes(1), Now.AddMinutes(2));

        var exception = Assert.Throws<ArgumentNullException>(
            () => new ServiceBusReceiveLockContext(InputAddress, new StubMessageLockContext(), message, null!));

        Assert.Equal("timeProvider", exception.ParamName);
    }

    private static ServiceBusReceivedMessage CreateMessage(DateTimeOffset lockedUntil, DateTimeOffset expiresAt)
    {
        DateTimeOffset enqueuedAt = Now.AddHours(-2);
        ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            messageId: "clock-message",
            timeToLive: expiresAt - enqueuedAt,
            lockedUntil: lockedUntil,
            enqueuedTime: enqueuedAt);

        Assert.Equal(lockedUntil, message.LockedUntil);
        Assert.Equal(expiresAt, message.ExpiresAt);
        return message;
    }

    private sealed record ClockMessage(string Value);

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan elapsed) => _utcNow += elapsed;
    }

    private sealed class StubMessageLockContext : MessageLockContext
    {
        public Task Complete() => Task.CompletedTask;

        public Task Abandon(Exception exception) => Task.CompletedTask;

        public Task DeadLetter() => Task.CompletedTask;

        public Task DeadLetter(Exception exception) => Task.CompletedTask;
    }
}
