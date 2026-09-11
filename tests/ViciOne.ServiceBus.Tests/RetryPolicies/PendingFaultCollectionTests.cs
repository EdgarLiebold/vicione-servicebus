using System.Reflection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.RetryPolicies;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.RetryPolicies;

public sealed class PendingFaultCollectionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PENDING-FAULTS", "required-inputs-and-duration")]
    public void Add_RejectsInvalidFaultData()
    {
        var faults = new PendingFaultCollection();
        ConsumeContext<TestMessage> messageContext = CreateMessageContext<TestMessage>();
        var exception = new InvalidOperationException("consumer failed");

        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => faults.Add<TestMessage>(null!, TimeSpan.Zero, "Consumer", exception)).ParamName);
        Assert.Equal(
            "elapsed",
            Assert.Throws<ArgumentOutOfRangeException>(() => faults.Add(messageContext, TimeSpan.FromTicks(-1), "Consumer", exception)).ParamName);
        Assert.Equal(
            "consumerType",
            Assert.Throws<ArgumentNullException>(() => faults.Add(messageContext, TimeSpan.Zero, null!, exception)).ParamName);
        Assert.Equal(
            "consumerType",
            Assert.Throws<ArgumentException>(() => faults.Add(messageContext, TimeSpan.Zero, string.Empty, exception)).ParamName);
        Assert.Equal(
            "consumerType",
            Assert.Throws<ArgumentException>(() => faults.Add(messageContext, TimeSpan.Zero, " ", exception)).ParamName);
        Assert.Equal(
            "exception",
            Assert.Throws<ArgumentNullException>(() => faults.Add(messageContext, TimeSpan.Zero, "Consumer", null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PENDING-FAULTS", "missing-owner-fails-before-collection-is-sealed")]
    public async Task MissingNotificationOwner_IsRejectedWithoutSealingTheCollectionAsync()
    {
        var faults = new PendingFaultCollection();
        ConsumeContext<TestMessage> messageContext = CreateMessageContext<TestMessage>();
        faults.Add(messageContext, TimeSpan.Zero, "Consumer", new InvalidOperationException("failed"));

        ArgumentNullException missing = await Assert.ThrowsAsync<ArgumentNullException>(
            () => faults.NotifyAsync(null!, TestContext.Current.CancellationToken));

        Assert.Equal("consumeContext", missing.ParamName);
        var (ownerContext, observer) = CreateOwnerContext();
        await faults.NotifyAsync(ownerContext, TestContext.Current.CancellationToken);
        Assert.Single(observer.Notifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PENDING-FAULTS", "complete-notification-data-and-token")]
    public async Task NotifyAsync_ForwardsEveryFaultAndTheCallerTokenAsync()
    {
        var faults = new PendingFaultCollection();
        ConsumeContext<TestMessage> firstContext = CreateMessageContext<TestMessage>();
        ConsumeContext<OtherMessage> secondContext = CreateMessageContext<OtherMessage>();
        var firstException = new InvalidOperationException("first");
        var secondException = new ApplicationException("second");
        faults.Add(firstContext, TimeSpan.FromMilliseconds(12), "FirstConsumer", firstException);
        faults.Add(secondContext, TimeSpan.FromMilliseconds(34), "SecondConsumer", secondException);
        var (ownerContext, observer) = CreateOwnerContext();
        using var cancellationSource = new CancellationTokenSource();

        await faults.NotifyAsync(ownerContext, cancellationSource.Token);

        Assert.Collection(
            observer.Notifications.OrderBy(notification => notification.ConsumerType),
            notification =>
            {
                Assert.Same(firstContext, notification.MessageContext);
                Assert.Equal(TimeSpan.FromMilliseconds(12), notification.Elapsed);
                Assert.Equal("FirstConsumer", notification.ConsumerType);
                Assert.Same(firstException, notification.Exception);
                Assert.Equal(cancellationSource.Token, notification.CancellationToken);
            },
            notification =>
            {
                Assert.Same(secondContext, notification.MessageContext);
                Assert.Equal(TimeSpan.FromMilliseconds(34), notification.Elapsed);
                Assert.Equal("SecondConsumer", notification.ConsumerType);
                Assert.Same(secondException, notification.Exception);
                Assert.Equal(cancellationSource.Token, notification.CancellationToken);
            });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PENDING-FAULTS", "single-notification-owner")]
    public async Task NotificationStart_AtomicallySealsTheCollectionAsync()
    {
        var faults = new PendingFaultCollection();
        ConsumeContext<TestMessage> messageContext = CreateMessageContext<TestMessage>();
        faults.Add(messageContext, TimeSpan.Zero, "Consumer", new InvalidOperationException("failed"));
        var (ownerContext, observer) = CreateOwnerContext();
        var notificationRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        observer.NotificationTask = notificationRelease.Task;

        Task notification = faults.NotifyAsync(ownerContext, TestContext.Current.CancellationToken);

        Assert.Throws<InvalidOperationException>(
            () => faults.Add(messageContext, TimeSpan.Zero, "LateConsumer", new InvalidOperationException("late")));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => faults.NotifyAsync(ownerContext, TestContext.Current.CancellationToken));

        notificationRelease.SetResult();
        await notification;
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PENDING-FAULTS", "pre-cancellation-preserves-collection")]
    public async Task PreCanceledNotification_DoesNotSealOrDispatchTheCollectionAsync()
    {
        var faults = new PendingFaultCollection();
        ConsumeContext<TestMessage> messageContext = CreateMessageContext<TestMessage>();
        faults.Add(messageContext, TimeSpan.Zero, "Consumer", new InvalidOperationException("failed"));
        var (ownerContext, observer) = CreateOwnerContext();
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => faults.NotifyAsync(ownerContext, cancellationSource.Token));

        Assert.Equal(cancellationSource.Token, canceled.CancellationToken);
        Assert.Empty(observer.Notifications);

        await faults.NotifyAsync(ownerContext, TestContext.Current.CancellationToken);
        Assert.Single(observer.Notifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PENDING-FAULTS", "synchronous-failure-does-not-skip-later-notifications")]
    public async Task SynchronousObserverFailure_DoesNotPreventLaterFaultNotificationAsync()
    {
        var faults = new PendingFaultCollection();
        ConsumeContext<TestMessage> firstContext = CreateMessageContext<TestMessage>();
        ConsumeContext<OtherMessage> secondContext = CreateMessageContext<OtherMessage>();
        faults.Add(firstContext, TimeSpan.FromMilliseconds(1), "FirstConsumer", new InvalidOperationException("first"));
        faults.Add(secondContext, TimeSpan.FromMilliseconds(2), "SecondConsumer", new InvalidOperationException("second"));
        var (ownerContext, observer) = CreateOwnerContext();
        observer.SynchronouslyFaultingConsumer = "FirstConsumer";

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => faults.NotifyAsync(ownerContext, TestContext.Current.CancellationToken));

        Assert.Equal("synchronous observer failure", failure.Message);
        Assert.Equal(["FirstConsumer", "SecondConsumer"], observer.Notifications.Select(notification => notification.ConsumerType));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PENDING-FAULTS", "missing-notification-task-does-not-skip-later-notifications")]
    public async Task MissingObserverTask_IsDiagnosedAfterEveryFaultIsAttemptedAsync()
    {
        var faults = new PendingFaultCollection();
        ConsumeContext<TestMessage> firstContext = CreateMessageContext<TestMessage>();
        ConsumeContext<OtherMessage> secondContext = CreateMessageContext<OtherMessage>();
        faults.Add(firstContext, TimeSpan.FromMilliseconds(1), "FirstConsumer", new InvalidOperationException("first"));
        faults.Add(secondContext, TimeSpan.FromMilliseconds(2), "SecondConsumer", new InvalidOperationException("second"));
        var (ownerContext, observer) = CreateOwnerContext();
        observer.NullTaskConsumer = "FirstConsumer";

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => faults.NotifyAsync(ownerContext, TestContext.Current.CancellationToken));

        Assert.Equal("A consumer fault notification returned no task.", failure.Message);
        Assert.Equal(["FirstConsumer", "SecondConsumer"], observer.Notifications.Select(notification => notification.ConsumerType));
    }

    private static ConsumeContext<TMessage> CreateMessageContext<TMessage>()
        where TMessage : class => DispatchProxy.Create<ConsumeContext<TMessage>, UnusedContextProxy>();

    private static (ConsumeContext Context, FaultObserverProxy Proxy) CreateOwnerContext()
    {
        ConsumeContext context = DispatchProxy.Create<ConsumeContext, FaultObserverProxy>();
        return (context, (FaultObserverProxy)(object)context);
    }

    private sealed record FaultNotification(
        object MessageContext,
        TimeSpan Elapsed,
        string ConsumerType,
        Exception Exception,
        CancellationToken CancellationToken);

    private class FaultObserverProxy : DispatchProxy
    {
        public List<FaultNotification> Notifications { get; } = [];

        public Task NotificationTask { get; set; } = Task.CompletedTask;

        public string? NullTaskConsumer { get; set; }

        public string? SynchronouslyFaultingConsumer { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            ArgumentNullException.ThrowIfNull(args);

            if (targetMethod.Name != nameof(ConsumeContext.NotifyFaultedAsync))
                throw new NotSupportedException(targetMethod.Name);

            Notifications.Add(
                new FaultNotification(
                    args[0]!,
                    (TimeSpan)args[1]!,
                    (string)args[2]!,
                    (Exception)args[3]!,
                    (CancellationToken)args[4]!));

            string consumerType = (string)args[2]!;
            if (consumerType == SynchronouslyFaultingConsumer)
                throw new InvalidOperationException("synchronous observer failure");
            if (consumerType == NullTaskConsumer)
                return null;

            return NotificationTask;
        }
    }

    private class UnusedContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }

    private sealed record TestMessage;

    private sealed record OtherMessage;
}
