using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class ObserverMessageFilterFailureTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-MESSAGE-OBSERVER-FAILURE-OWNERSHIP", "on-next-and-independent-failure-reporting-causes")]
    public async Task OnNextFailure_PreservesEveryFailureAndStillCallsOnErrorAsync(
        bool notificationFails, bool onErrorFails)
    {
        var operationFailure = new InvalidOperationException("observer work failed");
        var notificationFailure = new ApplicationException("fault notification failed");
        var onErrorFailure = new NotSupportedException("observer error callback failed");
        var events = new List<string>();
        var context = new RecordingContext(
            InMemoryOutboxTestContextFactory.Create(new ObservedMessage(), TestContext.Current.CancellationToken),
            events,
            notificationFails ? notificationFailure : null);
        var observer = new RecordingObserver(context, operationFailure,
            onErrorFails ? onErrorFailure : null, events);
        IFilter<ConsumeContext<ObservedMessage>> filter = new ObserverMessageFilter<ObservedMessage>(observer);
        IPipe<ConsumeContext<ObservedMessage>> next = Pipe.Execute<ConsumeContext<ObservedMessage>>(_ => events.Add("next"));

        Exception actual = await Record.ExceptionAsync(() => filter.SendAsync(context, next))
            ?? throw new Xunit.Sdk.XunitException("The failed observer completed successfully.");

        if (!notificationFails && !onErrorFails)
            Assert.Same(operationFailure, actual);
        else
        {
            var expected = new List<Exception> { operationFailure };
            if (notificationFails)
                expected.Add(notificationFailure);
            if (onErrorFails)
                expected.Add(onErrorFailure);
            Assert.Equal(expected, Assert.IsType<AggregateException>(actual).InnerExceptions,
                ReferenceEqualityComparer.Instance);
        }

        Assert.Same(operationFailure, context.Fault);
        Assert.Same(operationFailure, observer.ReportedError);
        Assert.Equal(1, observer.OnNextCount);
        Assert.Equal(1, observer.OnErrorCount);
        Assert.Equal(["on-next", "faulted", "on-error"], events);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-OBSERVER-FAILURE-OWNERSHIP", "downstream-failure-remains-downstream")]
    public async Task DownstreamFailure_DoesNotFaultACompletedObserverAsync()
    {
        var downstreamFailure = new InvalidOperationException("downstream failed");
        var events = new List<string>();
        var context = new RecordingContext(
            InMemoryOutboxTestContextFactory.Create(new ObservedMessage(), TestContext.Current.CancellationToken), events, null);
        var observer = new RecordingObserver(context, downstreamFailure, null, events, throwOnNext: false);
        IFilter<ConsumeContext<ObservedMessage>> filter = new ObserverMessageFilter<ObservedMessage>(observer);
        IPipe<ConsumeContext<ObservedMessage>> next = Pipe.ExecuteAwaited<ConsumeContext<ObservedMessage>>(_ =>
        {
            events.Add("next");
            return Task.FromException(downstreamFailure);
        });

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => filter.SendAsync(context, next));

        Assert.Same(downstreamFailure, actual);
        Assert.Null(context.Fault);
        Assert.Null(observer.ReportedError);
        Assert.Equal(1, observer.OnNextCount);
        Assert.Equal(0, observer.OnErrorCount);
        Assert.Equal(["on-next", "consumed", "next"], events);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-OBSERVER-FAILURE-OWNERSHIP", "consume-notification-failure-belongs-to-observer-stage")]
    public async Task ConsumeNotificationFailure_ReportsTheOriginalAndStopsBeforeDownstreamAsync()
    {
        var notificationFailure = new InvalidOperationException("consumed notification failed");
        var events = new List<string>();
        var context = new RecordingContext(
            InMemoryOutboxTestContextFactory.Create(new ObservedMessage(), TestContext.Current.CancellationToken),
            events, null, notificationFailure);
        var observer = new RecordingObserver(context, notificationFailure, null, events, throwOnNext: false);
        IFilter<ConsumeContext<ObservedMessage>> filter = new ObserverMessageFilter<ObservedMessage>(observer);
        IPipe<ConsumeContext<ObservedMessage>> next = Pipe.Execute<ConsumeContext<ObservedMessage>>(_ => events.Add("next"));

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => filter.SendAsync(context, next));

        Assert.Same(notificationFailure, actual);
        Assert.Same(notificationFailure, context.Fault);
        Assert.Same(notificationFailure, observer.ReportedError);
        Assert.Equal(1, observer.OnNextCount);
        Assert.Equal(1, observer.OnErrorCount);
        Assert.Equal(["on-next", "consumed", "faulted", "on-error"], events);
    }

    public sealed record ObservedMessage;

    private sealed class RecordingContext(ConsumeContext<ObservedMessage> source,
        List<string> events, Exception? notificationFailure, Exception? consumedNotificationFailure = null)
        : ConsumeContextScope<ObservedMessage>(source)
    {
        public Exception? Fault { get; private set; }

        public override Task NotifyConsumedAsync<T>(ConsumeContext<T> messageContext, TimeSpan duration,
            string consumerType, CancellationToken cancellationToken = default)
        {
            events.Add("consumed");
            return consumedNotificationFailure is null
                ? Task.CompletedTask
                : Task.FromException(consumedNotificationFailure);
        }

        public override Task NotifyFaultedAsync<T>(ConsumeContext<T> messageContext, TimeSpan duration,
            string consumerType, Exception exception, CancellationToken cancellationToken = default)
        {
            Assert.Same(this, messageContext);
            Fault = exception;
            events.Add("faulted");
            return notificationFailure is null ? Task.CompletedTask : Task.FromException(notificationFailure);
        }
    }

    private sealed class RecordingObserver(ConsumeContext<ObservedMessage> expectedContext,
        Exception operationFailure, Exception? onErrorFailure, List<string> events, bool throwOnNext = true)
        : IObserver<ConsumeContext<ObservedMessage>>
    {
        public int OnNextCount { get; private set; }

        public int OnErrorCount { get; private set; }

        public Exception? ReportedError { get; private set; }

        public void OnCompleted() => throw new Xunit.Sdk.XunitException("OnCompleted must not run after observer failure.");

        public void OnError(Exception error)
        {
            OnErrorCount++;
            ReportedError = error;
            events.Add("on-error");
            if (onErrorFailure is not null)
                throw onErrorFailure;
        }

        public void OnNext(ConsumeContext<ObservedMessage> value)
        {
            Assert.Same(expectedContext, value);
            OnNextCount++;
            events.Add("on-next");
            if (throwOnNext)
                throw operationFailure;
        }
    }
}
