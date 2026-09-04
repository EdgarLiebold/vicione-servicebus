using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.Outbox;

public sealed class BusOutboxNotificationTests
{
    private static readonly DateTimeOffset Now =
        new(2042, 3, 4, 5, 6, 7, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-OUTBOX-NOTIFICATION", "delivery-signal-bypasses-poll-delay")]
    public async Task Delivered_WakesTheWaiterWithoutAdvancingTheConfiguredClock()
    {
        var timeProvider = new FakeTimeProvider(Now);
        var notification = CreateNotification(timeProvider);
        Task wait = notification.WaitForDelivery(TestContext.Current.CancellationToken);

        notification.Delivered();
        await wait.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        Assert.Equal(Now, timeProvider.GetUtcNow());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-OUTBOX-NOTIFICATION", "delivery-signal-before-wait-is-retained")]
    public async Task DeliveredBeforeWait_IsConsumedWithoutAdvancingTheConfiguredClock()
    {
        var timeProvider = new FakeTimeProvider(Now);
        var notification = CreateNotification(timeProvider);

        notification.Delivered();
        await notification.WaitForDelivery(TestContext.Current.CancellationToken)
            .WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        Assert.Equal(Now, timeProvider.GetUtcNow());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-OUTBOX-NOTIFICATION", "poll-delay-uses-injected-clock")]
    public async Task PollDelay_CompletesOnlyAfterTheInjectedClockReachesTheDeadline()
    {
        var timeProvider = new FakeTimeProvider(Now);
        var notification = CreateNotification(timeProvider);
        Task wait = notification.WaitForDelivery(TestContext.Current.CancellationToken);

        timeProvider.Advance(TimeSpan.FromMinutes(10) - TimeSpan.FromTicks(1));
        Assert.False(wait.IsCompleted);
        timeProvider.Advance(TimeSpan.FromTicks(1));
        await wait.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        Assert.Equal(Now + TimeSpan.FromMinutes(10), timeProvider.GetUtcNow());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-OUTBOX-NOTIFICATION", "second-waiter-fails-closed")]
    public async Task ConcurrentWaiter_IsRejectedWithoutReplacingTheActiveSignal()
    {
        var notification = CreateNotification(new FakeTimeProvider(Now));
        Task firstWait = notification.WaitForDelivery(TestContext.Current.CancellationToken);

        InvalidOperationException rejected = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            notification.WaitForDelivery(TestContext.Current.CancellationToken));
        notification.Delivered();
        await firstWait.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        Assert.Equal("Only one outbox delivery agent may wait on NotificationScope.", rejected.Message);
    }

    private static BusOutboxNotification<NotificationScope> CreateNotification(TimeProvider timeProvider) => new(
        Options.Create(new OutboxDeliveryServiceOptions<NotificationScope> { QueryDelay = TimeSpan.FromMinutes(10) }),
        timeProvider);

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed class NotificationScope;
}
