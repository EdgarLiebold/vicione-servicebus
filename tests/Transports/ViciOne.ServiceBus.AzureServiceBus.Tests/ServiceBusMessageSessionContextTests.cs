using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusMessageSessionContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TRANSPORT-METADATA", "session-expiry-follows-sdk-renewals-in-utc-without-message-mutation")]
    public async Task LockedUntilUtc_FollowsSuccessfulSessionRenewalsAsync()
    {
        DateTimeOffset initial = new(2044, 5, 6, 7, 8, 9, TimeSpan.FromHours(2));
        DateTimeOffset messageExpiry = initial.AddMinutes(1);
        ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"), sessionId: "accepted-session", lockedUntil: messageExpiry);
        await using var receiver = new SessionReceiver(initial);
        using var processor = new CancellationTokenSource();
        var callback = new ProcessSessionMessageEventArgs(message, receiver, processor.Token);
        var context = new ServiceBusMessageSessionContext(callback, processor.Token);

        Assert.Equal(initial.ToUniversalTime(), context.LockedUntilUtc);
        Assert.Equal(TimeSpan.Zero, context.LockedUntilUtc.Offset);
        Assert.Equal("accepted-session", context.SessionId);
        for (var index = 1; index <= 2; index++)
        {
            await callback.RenewSessionLockAsync(processor.Token);
            Assert.Equal(initial.AddMinutes(5 * index).ToUniversalTime(), context.LockedUntilUtc);
            Assert.Equal(TimeSpan.Zero, context.LockedUntilUtc.Offset);
            Assert.Equal(messageExpiry, message.LockedUntil);
            Assert.Equal(processor.Token, receiver.RenewalToken);
        }
        Assert.Equal(2, receiver.Renewals);
        await context.RenewLockAsync(message, TestContext.Current.CancellationToken);
        Assert.Equal(2, receiver.Renewals);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SETTLEMENT-CANCELLATION", "session-noop-renewal-preserves-pre-canceled-caller-token")]
    public async Task RenewLock_PreCanceledCallerRetainsItsTokenAsync()
    {
        ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(sessionId: "accepted-session");
        await using var receiver = new SessionReceiver(DateTimeOffset.UnixEpoch);
        var context = new ServiceBusMessageSessionContext(new ProcessSessionMessageEventArgs(message, receiver, CancellationToken.None), CancellationToken.None);
        using var caller = new CancellationTokenSource();
        caller.Cancel();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.RenewLockAsync(message, caller.Token));

        Assert.Equal(caller.Token, exception.CancellationToken);
        Assert.Equal(0, receiver.Renewals);
    }

    sealed class SessionReceiver(DateTimeOffset expiry) : ServiceBusSessionReceiver
    {
        DateTimeOffset _expiry = expiry;
        public override string SessionId => "accepted-session";
        public override DateTimeOffset SessionLockedUntil => _expiry;
        public int Renewals { get; private set; }
        public CancellationToken RenewalToken { get; private set; }

        public override Task RenewSessionLockAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RenewalToken = cancellationToken;
            Renewals++;
            _expiry = _expiry.AddMinutes(5);
            return Task.CompletedTask;
        }

        public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
