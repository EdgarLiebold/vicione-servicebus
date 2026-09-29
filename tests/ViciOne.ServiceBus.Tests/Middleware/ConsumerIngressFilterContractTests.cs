using System.Diagnostics;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Consumers;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.CircuitBreaker;
using ViciOne.ServiceBus.Logging.Diagnostics;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;
using DiagnosticActivityContext = System.Diagnostics.ActivityContext;

namespace ViciOne.ServiceBus.Tests.Middleware;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class ConsumerIngressFilterContractTests
{
    [Theory]
    [InlineData(FilterShape.Factory)]
    [InlineData(FilterShape.Handler)]
    [InlineData(FilterShape.Instance)]
    public async Task SuccessfulWork_AwaitsTheConsumeNotificationBeforeContinuingAsync(FilterShape shape)
    {
        var trace = new List<string>();
        var notificationEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseNotification = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var consumer = new TestConsumer();
        ConsumeContext<TestMessage> context = CreateContext(
            TestContext.Current.CancellationToken, trace, notificationEntered, releaseNotification.Task);
        IFilter<ConsumeContext<TestMessage>> filter = CreateFilter(shape, consumer, received =>
        {
            AssertDeliveryContext(context, received);
            if (shape == FilterShape.Handler)
                Assert.Same(context, received);
            trace.Add("work");
            return Task.CompletedTask;
        });
        IPipe<ConsumeContext<TestMessage>> next = Pipe.Execute<ConsumeContext<TestMessage>>(received =>
        {
            Assert.Same(context, received);
            trace.Add("next");
        });

        Task operation = filter.SendAsync(context, next);
        try
        {
            Task firstCompletion = await Task.WhenAny(notificationEntered.Task, operation)
                .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Same(notificationEntered.Task, firstCompletion);
            Assert.Equal(["work", "consumed"], trace);
            Assert.False(operation.IsCompleted);

            releaseNotification.SetResult();
            await operation.WaitAsync(TestContext.Current.CancellationToken);
        }
        finally
        {
            releaseNotification.TrySetResult();
        }

        Assert.Equal(["work", "consumed", "next"], trace);
    }

    [Theory]
    [InlineData(FilterShape.Factory)]
    [InlineData(FilterShape.Handler)]
    [InlineData(FilterShape.Instance)]
    public async Task BusinessFailure_PreservesTheExceptionAndDoesNotContinueAsync(FilterShape shape)
    {
        var trace = new List<string>();
        var expected = new ExpectedBusinessException();
        var consumer = new TestConsumer();
        ConsumeContext<TestMessage> context = CreateContext(TestContext.Current.CancellationToken, trace);
        IFilter<ConsumeContext<TestMessage>> filter = CreateFilter(shape, consumer, received =>
        {
            AssertDeliveryContext(context, received);
            if (shape == FilterShape.Handler)
                Assert.Same(context, received);
            trace.Add("work");
            return Task.FromException(expected);
        });
        IPipe<ConsumeContext<TestMessage>> next = Pipe.Execute<ConsumeContext<TestMessage>>(_ => trace.Add("next"));

        ExpectedBusinessException actual = await Assert.ThrowsAsync<ExpectedBusinessException>(() =>
            filter.SendAsync(context, next).WaitAsync(TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(["work", "faulted"], trace);
        Assert.Same(expected, ((RecordingScope)context).Fault);
    }

    [Theory]
    [InlineData(DiagnosticFault.ThrowBase)]
    [InlineData(DiagnosticFault.NullBase)]
    [InlineData(DiagnosticFault.ThrowStackTrace)]
    [RequirementCoverage("REQ-VSB-CONSUME-FAULT-NOTIFICATION", "unsafe-exception-diagnostics-cannot-suppress-fault-notification")]
    public async Task HandlerFault_UnsafeExceptionDiagnosticsStillReportTheOriginalFailureAsync(DiagnosticFault diagnosticFault)
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<DiagnosticActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);
        using var ambient = new Activity("orders receive").Start();
        var trace = new List<string>();
        var context = (RecordingScope)CreateContext(TestContext.Current.CancellationToken, trace);
        var failure = new UnsafeDiagnosticException(diagnosticFault);
        Activity? process = null;
        IFilter<ConsumeContext<TestMessage>> filter = CreateFilter(FilterShape.Handler, new TestConsumer(), _ =>
        {
            process = Activity.Current;
            trace.Add("work");
            return Task.FromException(failure);
        });

        Exception actual = await Record.ExceptionAsync(() => filter.SendAsync(context,
            Pipe.Execute<ConsumeContext<TestMessage>>(_ => trace.Add("next"))))
            ?? throw new Xunit.Sdk.XunitException("The failed handler completed successfully.");

        Assert.Same(failure, actual);
        Assert.Same(failure, context.Fault);
        Assert.Equal(["work", "faulted"], trace);
        Activity activity = Assert.IsType<Activity>(process);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        ActivityEvent exceptionEvent = Assert.Single(activity.Events);
        Assert.Equal("original failure", exceptionEvent.Tags.Single(tag =>
            tag.Key == ServiceBusTelemetry.Attributes.ExceptionMessage).Value);
        Assert.Equal(typeof(UnsafeDiagnosticException).FullName, exceptionEvent.Tags.Single(tag =>
            tag.Key == ServiceBusTelemetry.Attributes.ExceptionType).Value);
        if (diagnosticFault == DiagnosticFault.ThrowStackTrace)
            Assert.Equal("", exceptionEvent.Tags.Single(tag =>
                tag.Key == ServiceBusTelemetry.Attributes.ExceptionStackTrace).Value);
        Assert.True(activity.Duration > TimeSpan.Zero);
    }

    [Theory]
    [InlineData(FilterShape.Factory)]
    [InlineData(FilterShape.Handler)]
    [InlineData(FilterShape.Instance)]
    public async Task ConsumeNotificationFailure_DoesNotReplayWorkOrContinueAsync(FilterShape shape)
    {
        var trace = new List<string>();
        var notificationFailure = new ExpectedBusinessException();
        var context = (RecordingScope)CreateContext(
            TestContext.Current.CancellationToken, trace,
            notificationCompletion: Task.FromException(notificationFailure));
        IFilter<ConsumeContext<TestMessage>> filter = CreateFilter(shape, new TestConsumer(), received =>
        {
            AssertDeliveryContext(context, received);
            if (shape == FilterShape.Handler)
                Assert.Same(context, received);
            trace.Add("work");
            return Task.CompletedTask;
        });
        IPipe<ConsumeContext<TestMessage>> next = Pipe.Execute<ConsumeContext<TestMessage>>(_ => trace.Add("next"));

        ExpectedBusinessException actual = await Assert.ThrowsAsync<ExpectedBusinessException>(() =>
            filter.SendAsync(context, next).WaitAsync(TestContext.Current.CancellationToken));

        Assert.Same(notificationFailure, actual);
        Assert.Same(notificationFailure, context.Fault);
        Assert.Equal(["work", "consumed", "faulted"], trace);
    }

    [Theory]
    [InlineData(FilterShape.Factory)]
    [InlineData(FilterShape.Handler)]
    [InlineData(FilterShape.Instance)]
    public async Task DownstreamFailure_DoesNotReclassifyACompletedConsumerAsFaultedAsync(FilterShape shape)
    {
        var trace = new List<string>();
        var downstreamFailure = new ExpectedBusinessException();
        var context = (RecordingScope)CreateContext(TestContext.Current.CancellationToken, trace);
        IFilter<ConsumeContext<TestMessage>> filter = CreateFilter(shape, new TestConsumer(), received =>
        {
            AssertDeliveryContext(context, received);
            trace.Add("work");
            return Task.CompletedTask;
        });
        IPipe<ConsumeContext<TestMessage>> next = Pipe.ExecuteAwaited<ConsumeContext<TestMessage>>(received =>
        {
            Assert.Same(context, received);
            trace.Add("next");
            return Task.FromException(downstreamFailure);
        });

        ExpectedBusinessException actual = await Assert.ThrowsAsync<ExpectedBusinessException>(() =>
            filter.SendAsync(context, next).WaitAsync(TestContext.Current.CancellationToken));

        Assert.Same(downstreamFailure, actual);
        Assert.Null(context.Fault);
        Assert.Equal(["work", "consumed", "next"], trace);
    }

    [Theory]
    [InlineData(FilterShape.Factory, false)]
    [InlineData(FilterShape.Handler, false)]
    [InlineData(FilterShape.Instance, false)]
    [InlineData(FilterShape.Factory, true)]
    [InlineData(FilterShape.Handler, true)]
    [InlineData(FilterShape.Instance, true)]
    public async Task DownstreamCancellation_RemainsDownstreamRegardlessOfCallerTokenAsync(
        FilterShape shape, bool callerRequestedCancellation)
    {
        using var caller = new CancellationTokenSource();
        if (callerRequestedCancellation)
            caller.Cancel();

        var trace = new List<string>();
        var context = (RecordingScope)CreateContext(caller.Token, trace);
        var expected = new OperationCanceledException("downstream canceled", caller.Token);
        IFilter<ConsumeContext<TestMessage>> filter = CreateFilter(shape, new TestConsumer(), received =>
        {
            AssertDeliveryContext(context, received);
            trace.Add("work");
            return Task.CompletedTask;
        });
        IPipe<ConsumeContext<TestMessage>> next = Pipe.ExecuteAwaited<ConsumeContext<TestMessage>>(received =>
        {
            Assert.Same(context, received);
            trace.Add("next");
            return Task.FromException(expected);
        });

        OperationCanceledException actual = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            filter.SendAsync(context, next).WaitAsync(TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Null(context.Fault);
        Assert.Equal(["work", "consumed", "next"], trace);
    }

    [Theory]
    [InlineData(FilterShape.Factory)]
    [InlineData(FilterShape.Handler)]
    [InlineData(FilterShape.Instance)]
    public async Task ConsumerProcessActivity_EndsBeforeTheDownstreamStageAsync(FilterShape shape)
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<DiagnosticActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);
        using var ambient = new Activity("orders receive");
        ambient.Start();

        var trace = new List<string>();
        ConsumeContext<TestMessage> context = CreateContext(TestContext.Current.CancellationToken, trace);
        Activity? consumerActivity = null;
        Activity? downstreamActivity = null;
        IFilter<ConsumeContext<TestMessage>> filter = CreateFilter(shape, new TestConsumer(), received =>
        {
            AssertDeliveryContext(context, received);
            consumerActivity = Activity.Current;
            trace.Add("work");
            return Task.CompletedTask;
        });
        IPipe<ConsumeContext<TestMessage>> next = Pipe.Execute<ConsumeContext<TestMessage>>(_ =>
        {
            downstreamActivity = Activity.Current;
            trace.Add("next");
        });

        await filter.SendAsync(context, next).WaitAsync(TestContext.Current.CancellationToken);

        Activity process = Assert.IsType<Activity>(consumerActivity);
        Assert.NotSame(ambient, process);
        Assert.Equal(ActivityKind.Consumer, process.Kind);
        Assert.Same(ambient, downstreamActivity);
        Assert.Same(ambient, Activity.Current);
        Assert.Equal(["work", "consumed", "next"], trace);
    }

    [Theory]
    [InlineData(FilterShape.Factory, false)]
    [InlineData(FilterShape.Handler, false)]
    [InlineData(FilterShape.Instance, false)]
    [InlineData(FilterShape.Factory, true)]
    [InlineData(FilterShape.Handler, true)]
    [InlineData(FilterShape.Instance, true)]
    [RequirementCoverage("REQ-VSB-CONSUMER-CANCELLATION", "three-ingress-shapes-preserve-cancellation-cause-and-caller-ownership")]
    public async Task Cancellation_ReportsTheOriginalFailureAndPreservesCallerOwnershipAsync(
        FilterShape shape, bool callerRequestedCancellation)
    {
        using var cancellation = new CancellationTokenSource();
        if (callerRequestedCancellation)
            cancellation.Cancel();

        var trace = new List<string>();
        var expected = new OperationCanceledException("consumer dependency canceled", cancellation.Token);
        ConsumeContext<TestMessage> context = CreateContext(cancellation.Token, trace);
        IFilter<ConsumeContext<TestMessage>> filter = CreateFilter(shape, new TestConsumer(), received =>
        {
            AssertDeliveryContext(context, received);
            if (shape == FilterShape.Handler)
                Assert.Same(context, received);
            trace.Add("work");
            return Task.FromException(expected);
        });
        IPipe<ConsumeContext<TestMessage>> next = Pipe.Execute<ConsumeContext<TestMessage>>(_ => trace.Add("next"));

        Exception actual = await Record.ExceptionAsync(() =>
            filter.SendAsync(context, next).WaitAsync(TestContext.Current.CancellationToken))
            ?? throw new Xunit.Sdk.XunitException("The canceled consumer completed successfully.");

        if (callerRequestedCancellation)
            Assert.Same(expected, Assert.IsType<OperationCanceledException>(actual));
        else
            Assert.Same(expected, Assert.IsType<ConsumerCanceledException>(actual).InnerException);
        Assert.Equal(["work", "faulted"], trace);
        Assert.Same(expected, ((RecordingScope)context).Fault);
    }

    [Theory]
    [InlineData(FilterShape.Factory, false)]
    [InlineData(FilterShape.Handler, false)]
    [InlineData(FilterShape.Instance, false)]
    [InlineData(FilterShape.Factory, true)]
    [InlineData(FilterShape.Handler, true)]
    [InlineData(FilterShape.Instance, true)]
    [RequirementCoverage("REQ-VSB-CONSUMER-CANCELLATION", "aggregate-cause-retained-without-mixed-failure-reclassification")]
    public async Task AggregateConsumerCancellation_PreservesTheFullCauseWithoutHidingMixedFailuresAsync(
        FilterShape shape, bool mixedFailure)
    {
        using var dependency = new CancellationTokenSource();
        dependency.Cancel();
        var cancellation = new OperationCanceledException("dependency canceled", dependency.Token);
        var businessFailure = new ExpectedBusinessException();
        var aggregate = mixedFailure
            ? new AggregateException(cancellation, businessFailure)
            : new AggregateException(cancellation);
        var trace = new List<string>();
        var context = (RecordingScope)CreateContext(TestContext.Current.CancellationToken, trace);
        IFilter<ConsumeContext<TestMessage>> filter = CreateFilter(shape, new TestConsumer(), _ =>
        {
            trace.Add("work");
            return Task.FromException(aggregate);
        });
        IPipe<ConsumeContext<TestMessage>> next = Pipe.Execute<ConsumeContext<TestMessage>>(_ => trace.Add("next"));

        Exception actual = await Record.ExceptionAsync(() => filter.SendAsync(context, next))
            ?? throw new Xunit.Sdk.XunitException("The failed consumer completed successfully.");

        if (mixedFailure)
            Assert.Same(aggregate, Assert.IsType<AggregateException>(actual));
        else
            Assert.Same(aggregate, Assert.IsType<ConsumerCanceledException>(actual).InnerException);
        Assert.Same(aggregate, context.Fault);
        Assert.Equal(["work", "faulted"], trace);
    }

    [Theory]
    [InlineData(FilterShape.Factory)]
    [InlineData(FilterShape.Handler)]
    [InlineData(FilterShape.Instance)]
    [RequirementCoverage("REQ-VSB-CONSUMER-CANCELLATION", "nested-multiple-cancellations-remain-consumer-cancellation")]
    public async Task MultipleNestedCancellations_AreClassifiedWithoutLosingTheAggregateAsync(FilterShape shape)
    {
        using var dependency = new CancellationTokenSource();
        dependency.Cancel();
        var aggregate = new AggregateException(
            new OperationCanceledException("first", dependency.Token),
            new AggregateException(new OperationCanceledException("second", dependency.Token)));
        var trace = new List<string>();
        var context = (RecordingScope)CreateContext(TestContext.Current.CancellationToken, trace);
        IFilter<ConsumeContext<TestMessage>> filter = CreateFilter(shape, new TestConsumer(), _ =>
        {
            trace.Add("work");
            return Task.FromException(aggregate);
        });
        IPipe<ConsumeContext<TestMessage>> next = Pipe.Execute<ConsumeContext<TestMessage>>(_ => trace.Add("next"));

        ConsumerCanceledException actual = await Assert.ThrowsAsync<ConsumerCanceledException>(() =>
            filter.SendAsync(context, next));

        Assert.Same(aggregate, actual.InnerException);
        Assert.Same(aggregate, context.Fault);
        Assert.Equal(["work", "faulted"], trace);
    }

    [Theory]
    [InlineData(FilterShape.Factory)]
    [InlineData(FilterShape.Handler)]
    [InlineData(FilterShape.Instance)]
    [RequirementCoverage("REQ-VSB-CONSUMER-CANCELLATION", "unsafe-base-lookup-preserves-structural-cancellation")]
    public async Task UnsafeBaseLookup_StillClassifiesWrappedCancellationAsync(FilterShape shape)
    {
        var wrapper = new UnsafeBaseException(new OperationCanceledException("dependency canceled"));
        var trace = new List<string>();
        var context = (RecordingScope)CreateContext(TestContext.Current.CancellationToken, trace);
        IFilter<ConsumeContext<TestMessage>> filter = CreateFilter(shape, new TestConsumer(), _ =>
        {
            trace.Add("work");
            return Task.FromException(wrapper);
        });

        ConsumerCanceledException actual = await Assert.ThrowsAsync<ConsumerCanceledException>(() =>
            filter.SendAsync(context, Pipe.Execute<ConsumeContext<TestMessage>>(_ => trace.Add("next"))));

        Assert.Same(wrapper, actual.InnerException);
        Assert.Same(wrapper, context.Fault);
        Assert.Equal(["work", "faulted"], trace);
    }

    [Theory]
    [InlineData(FilterShape.Factory)]
    [InlineData(FilterShape.Handler)]
    [InlineData(FilterShape.Instance)]
    [RequirementCoverage("REQ-VSB-CONSUMER-CANCELLATION", "safe-custom-base-cancellation-remains-classified")]
    public async Task CustomBaseCancellation_RemainsClassifiedWithoutAnInnerExceptionAsync(FilterShape shape)
    {
        var failure = new ProjectedBaseException(new OperationCanceledException("dependency canceled"));
        var trace = new List<string>();
        var context = (RecordingScope)CreateContext(TestContext.Current.CancellationToken, trace);
        IFilter<ConsumeContext<TestMessage>> filter = CreateFilter(shape, new TestConsumer(), _ =>
        {
            trace.Add("work");
            return Task.FromException(failure);
        });

        ConsumerCanceledException actual = await Assert.ThrowsAsync<ConsumerCanceledException>(() =>
            filter.SendAsync(context, Pipe.Execute<ConsumeContext<TestMessage>>(_ => trace.Add("next"))));

        Assert.Same(failure, actual.InnerException);
        Assert.Same(failure, context.Fault);
        Assert.Equal(["work", "faulted"], trace);
    }

    [Theory]
    [InlineData(FilterShape.Factory, false, false)]
    [InlineData(FilterShape.Handler, false, false)]
    [InlineData(FilterShape.Instance, false, false)]
    [InlineData(FilterShape.Factory, true, false)]
    [InlineData(FilterShape.Handler, true, false)]
    [InlineData(FilterShape.Instance, true, false)]
    [InlineData(FilterShape.Factory, true, true)]
    [InlineData(FilterShape.Handler, true, true)]
    [InlineData(FilterShape.Instance, true, true)]
    [RequirementCoverage("REQ-VSB-CONSUMER-CANCELLATION", "wrapper-around-aggregate-preserves-pure-or-mixed-classification")]
    public async Task WrapperAroundAggregate_PreservesPureOrMixedCancellationClassificationAsync(
        FilterShape shape, bool mixedFailure, bool businessFirst)
    {
        var aggregate = mixedFailure
            ? businessFirst
                ? new AggregateException(new ExpectedBusinessException(), new OperationCanceledException("canceled"))
                : new AggregateException(new OperationCanceledException("canceled"), new ExpectedBusinessException())
            : new AggregateException(new OperationCanceledException("first"), new OperationCanceledException("second"));
        var wrapper = new InvalidOperationException("wrapper", aggregate);
        var trace = new List<string>();
        var context = (RecordingScope)CreateContext(TestContext.Current.CancellationToken, trace);
        IFilter<ConsumeContext<TestMessage>> filter = CreateFilter(shape, new TestConsumer(), _ =>
        {
            trace.Add("work");
            return Task.FromException(wrapper);
        });

        Exception actual = await Record.ExceptionAsync(() =>
            filter.SendAsync(context, Pipe.Empty<ConsumeContext<TestMessage>>()))
            ?? throw new Xunit.Sdk.XunitException("The failed consumer completed successfully.");

        if (mixedFailure)
            Assert.Same(wrapper, Assert.IsType<InvalidOperationException>(actual));
        else
            Assert.Same(wrapper, Assert.IsType<ConsumerCanceledException>(actual).InnerException);
        Assert.Same(wrapper, context.Fault);
        Assert.Equal(["work", "faulted"], trace);
    }

    [Theory]
    [InlineData(FilterShape.Factory)]
    [InlineData(FilterShape.Handler)]
    [InlineData(FilterShape.Instance)]
    [RequirementCoverage("REQ-VSB-CONSUMER-CANCELLATION", "preclassified-consumer-cancellation-keeps-identity")]
    public async Task PreclassifiedConsumerCancellation_IsNotWrappedAgainAsync(FilterShape shape)
    {
        var expected = new ConsumerCanceledException("already classified",
            new OperationCanceledException("dependency canceled"));
        var trace = new List<string>();
        var context = (RecordingScope)CreateContext(TestContext.Current.CancellationToken, trace);
        IFilter<ConsumeContext<TestMessage>> filter = CreateFilter(shape, new TestConsumer(), _ =>
        {
            trace.Add("work");
            return Task.FromException(expected);
        });

        ConsumerCanceledException actual = await Assert.ThrowsAsync<ConsumerCanceledException>(() =>
            filter.SendAsync(context, Pipe.Empty<ConsumeContext<TestMessage>>()));

        Assert.Same(expected, actual);
        Assert.Same(expected, context.Fault);
        Assert.Equal(["work", "faulted"], trace);
    }

    [Theory]
    [InlineData(FilterShape.Factory)]
    [InlineData(FilterShape.Handler)]
    [InlineData(FilterShape.Instance)]
    [RequirementCoverage("REQ-VSB-CONSUMER-CANCELLATION", "aggregate-around-preclassified-cancellation-keeps-identity")]
    public async Task AggregateAroundPreclassifiedCancellation_IsNotWrappedAgainAsync(FilterShape shape)
    {
        var aggregate = new AggregateException(new ConsumerCanceledException("already classified",
            new OperationCanceledException("dependency canceled")));
        var trace = new List<string>();
        var context = (RecordingScope)CreateContext(TestContext.Current.CancellationToken, trace);
        IFilter<ConsumeContext<TestMessage>> filter = CreateFilter(shape, new TestConsumer(), _ =>
        {
            trace.Add("work");
            return Task.FromException(aggregate);
        });

        AggregateException actual = await Assert.ThrowsAsync<AggregateException>(() =>
            filter.SendAsync(context, Pipe.Empty<ConsumeContext<TestMessage>>()));

        Assert.Same(aggregate, actual);
        Assert.Same(aggregate, context.Fault);
        Assert.Equal(["work", "faulted"], trace);
    }

    [Theory]
    [InlineData(FilterShape.Factory, false)]
    [InlineData(FilterShape.Handler, false)]
    [InlineData(FilterShape.Instance, false)]
    [InlineData(FilterShape.Factory, true)]
    [InlineData(FilterShape.Handler, true)]
    [InlineData(FilterShape.Instance, true)]
    [RequirementCoverage("REQ-VSB-CONSUMER-CANCELLATION", "notification-failure-preserves-operation-and-observer-causes")]
    public async Task FaultNotificationFailure_PreservesBothOperationAndObserverCausesAsync(
        FilterShape shape, bool businessFailure)
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<DiagnosticActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);
        using var ambient = new Activity("orders receive");
        ambient.Start();

        Exception original = businessFailure
            ? new ExpectedBusinessException()
            : new OperationCanceledException("dependency canceled");
        var observerFailure = new InvalidOperationException("fault observer failed");
        var trace = new List<string>();
        var context = (RecordingScope)CreateContext(TestContext.Current.CancellationToken, trace,
            faultNotificationFailure: observerFailure);
        Activity? consumerActivity = null;
        IFilter<ConsumeContext<TestMessage>> filter = CreateFilter(shape, new TestConsumer(), _ =>
        {
            consumerActivity = Activity.Current;
            trace.Add("work");
            return Task.FromException(original);
        });
        IPipe<ConsumeContext<TestMessage>> next = Pipe.Execute<ConsumeContext<TestMessage>>(_ => trace.Add("next"));

        AggregateException actual = await Assert.ThrowsAsync<AggregateException>(() => filter.SendAsync(context, next));

        Assert.Collection(actual.InnerExceptions,
            operation =>
            {
                if (businessFailure)
                    Assert.Same(original, operation);
                else
                    Assert.Same(original, Assert.IsType<ConsumerCanceledException>(operation).InnerException);
            },
            notification => Assert.Same(observerFailure, notification));
        Assert.Same(original, context.Fault);
        Assert.Equal(["work", "faulted"], trace);
        Activity recorded = Assert.IsType<Activity>(consumerActivity);
        Assert.Equal(ActivityStatusCode.Error, recorded.Status);
        Assert.Contains(recorded.Events, activityEvent => activityEvent.Name == ServiceBusTelemetry.Events.Exception);
        if (shape == FilterShape.Handler && businessFailure)
        {
            IProbeResult probe = filter.GetProbeResult(TestContext.Current.CancellationToken);
            IReadOnlyDictionary<string, object> scope = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(
                Assert.Contains("filters", probe.Results));
            Assert.Equal(1L, Assert.Contains("faulted", scope));
        }
    }

    [Fact]
    public async Task RetryAndCircuitBreaker_CountDeliveriesWithoutReplayingAnOpenConsumerAsync()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero));
        var trace = new List<string>();
        var failure = new ExpectedBusinessException();
        var attempt = 0;
        var attemptsByDelivery = new Dictionary<ConsumeContext<TestMessage>, int>(ReferenceEqualityComparer.Instance);
        IFilter<ConsumeContext<TestMessage>> handler = CreateFilter(FilterShape.Handler, new TestConsumer(), received =>
        {
            attemptsByDelivery[received] = attemptsByDelivery.GetValueOrDefault(received) + 1;
            trace.Add("work");
            attempt++;
            return attempt <= 4 ? Task.FromException(failure) : Task.CompletedTask;
        });
        IPipe<ConsumeContext<TestMessage>> pipe = Pipe.New<ConsumeContext<TestMessage>>(configuration =>
        {
            configuration.UseCircuitBreaker(options => options
                .SetMinimumThroughput(2)
                .SetFailureRatio(1)
                .SetBreakDuration(TimeSpan.FromSeconds(1))
                .SetTimeProvider(time));
            configuration.UseRetry(retry => retry.Immediate(1));
            configuration.UseFilter(handler);
        });

        RecordingScope First() => (RecordingScope)CreateContext(TestContext.Current.CancellationToken, trace);
        var first = First();
        var second = First();
        var rejected = First();

        Assert.Same(failure, await Assert.ThrowsAsync<ExpectedBusinessException>(() => pipe.SendAsync(first)));
        Assert.Same(failure, await Assert.ThrowsAsync<ExpectedBusinessException>(() => pipe.SendAsync(second)));
        int attemptsAtOpen = attempt;
        CircuitBreakerOpenException open = await Assert.ThrowsAsync<CircuitBreakerOpenException>(() =>
            pipe.SendAsync(rejected));

        Assert.Equal(4, attemptsAtOpen);
        Assert.Equal(attemptsAtOpen, attempt);
        Assert.Equal(2, attemptsByDelivery[first]);
        Assert.Equal(2, attemptsByDelivery[second]);
        Assert.False(attemptsByDelivery.ContainsKey(rejected));
        Assert.Equal(["work", "faulted", "work", "faulted", "work", "faulted", "work", "faulted"], trace);
        Assert.Same(failure, first.Fault);
        Assert.Same(failure, second.Fault);
        Assert.Null(rejected.Fault);
        Assert.True(open.RetryAfter > TimeSpan.Zero);

        time.Advance(TimeSpan.FromSeconds(1));
        var recovered = First();
        await pipe.SendAsync(recovered);

        Assert.Equal(5, attempt);
        Assert.Equal(1, attemptsByDelivery[recovered]);
        Assert.Equal("consumed", trace[^1]);
        Assert.Null(recovered.Fault);
        await pipe.SendAsync(First());
        Assert.Equal(6, attempt);
    }

    private static IFilter<ConsumeContext<TestMessage>> CreateFilter(
        FilterShape shape, TestConsumer consumer, Func<ConsumeContext<TestMessage>, Task> work)
    {
        IPipe<ConsumerConsumeContext<TestConsumer, TestMessage>> consumerPipe =
            Pipe.ExecuteAwaited<ConsumerConsumeContext<TestConsumer, TestMessage>>(scope =>
            {
                Assert.Same(consumer, scope.Consumer);
                return work(scope);
            });

        return shape switch
        {
            FilterShape.Factory => new ConsumerMessageFilter<TestConsumer, TestMessage>(
                new InstanceConsumerFactory<TestConsumer>(consumer), consumerPipe),
            FilterShape.Handler => new HandlerMessageFilter<TestMessage>(received => work(received)),
            FilterShape.Instance => new InstanceMessageFilter<TestConsumer, TestMessage>(consumer, consumerPipe),
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };
    }

    private static void AssertDeliveryContext(ConsumeContext<TestMessage> expected,
        ConsumeContext<TestMessage> received)
    {
        Assert.Same(expected.Message, received.Message);
        Assert.Same(expected.Advanced().ReceiveContext, received.Advanced().ReceiveContext);
        Assert.Equal(expected.CancellationToken, received.CancellationToken);
    }

    private static ConsumeContext<TestMessage> CreateContext(
        CancellationToken cancellationToken,
        List<string> trace,
        TaskCompletionSource? notificationEntered = null,
        Task? notificationCompletion = null,
        Exception? faultNotificationFailure = null)
    {
        return new RecordingScope(
            InMemoryOutboxTestContextFactory.Create(new TestMessage(), cancellationToken),
            trace, notificationEntered, notificationCompletion, faultNotificationFailure);
    }

    public enum FilterShape
    {
        Factory,
        Handler,
        Instance,
    }

    public enum DiagnosticFault
    {
        ThrowBase,
        NullBase,
        ThrowStackTrace,
    }

    private sealed class TestConsumer;

    public sealed record TestMessage;

    private sealed class ExpectedBusinessException : Exception;

    private sealed class UnsafeBaseException(Exception inner) : Exception("wrapper", inner)
    {
        public override Exception GetBaseException() => throw new InvalidOperationException("base lookup failed");
    }

    private sealed class ProjectedBaseException(Exception baseException) : Exception("projected cancellation")
    {
        public override Exception GetBaseException() => baseException;
    }

    private sealed class UnsafeDiagnosticException(DiagnosticFault fault) : Exception("original failure")
    {
        public override Exception GetBaseException() => fault == DiagnosticFault.ThrowBase
            ? throw new InvalidOperationException("base lookup failed")
            : fault == DiagnosticFault.NullBase ? null! : this;

        public override string? StackTrace => fault == DiagnosticFault.ThrowStackTrace
            ? throw new InvalidOperationException("stack trace lookup failed")
            : base.StackTrace;
    }

    private sealed class RecordingScope(
        ConsumeContext<TestMessage> context,
        List<string> trace,
        TaskCompletionSource? notificationEntered,
        Task? notificationCompletion,
        Exception? faultNotificationFailure) : ConsumeContextScope<TestMessage>(context)
    {
        public Exception? Fault { get; private set; }

        public override IEnumerable<string> SupportedMessageTypes =>
            [MessageTypeCache<TestMessage>.DiagnosticAddress];

        public override Task NotifyConsumedAsync<T>(ConsumeContext<T> messageContext, TimeSpan duration,
            string consumerType, CancellationToken cancellationToken = default)
        {
            Assert.Same(this, messageContext);
            Assert.True(duration >= TimeSpan.Zero);
            Assert.False(string.IsNullOrWhiteSpace(consumerType));
            trace.Add("consumed");
            notificationEntered?.TrySetResult();
            return notificationCompletion ?? Task.CompletedTask;
        }

        public override Task NotifyFaultedAsync<T>(ConsumeContext<T> messageContext, TimeSpan duration,
            string consumerType, Exception exception, CancellationToken cancellationToken = default)
        {
            Assert.Same(this, messageContext);
            Assert.True(duration >= TimeSpan.Zero);
            Assert.False(string.IsNullOrWhiteSpace(consumerType));
            Fault = exception;
            trace.Add("faulted");
            return faultNotificationFailure is null ? Task.CompletedTask : Task.FromException(faultNotificationFailure);
        }
    }
}
