using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using ViciOne.ServiceBus.Tests.Monitoring;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class SagaInitiationMessageFilterDeepContractTests
{
    private const string CallerActivitySource = "ViciOne.ServiceBus.Tests.SagaInitiationMessageFilterCaller";

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "initiation-filter-public-api-shape")]
    public void PublicApi_RemainsNonSealedAndRetainsParameterNamesAndConstraints()
    {
        AssertFilterApi(
            typeof(InitiatedBySagaMessageFilter<TestSaga, TestMessage>),
            typeof(InitiatedBySagaMessageFilter<,>),
            typeof(IInitiatedBy<>));
        AssertFilterApi(
            typeof(InitiatedByOrOrchestratesSagaMessageFilter<TestSaga, TestMessage>),
            typeof(InitiatedByOrOrchestratesSagaMessageFilter<,>),
            typeof(IInitiatedByOrOrchestrates<>));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "initiation-filter-required-input-precedence")]
    public async Task SendAsync_RejectsRequiredInputsBeforeCancellationOrUserCodeAsync(bool combinedRole)
    {
        using var cancellation = new CancellationTokenSource();
        var saga = new TestSaga(Guid.NewGuid(), _ => Task.CompletedTask);
        TrackingSagaContext context = CreateContext(saga, cancellation.Token);
        var next = new RecordingPipe(_ => Task.CompletedTask);
        ISagaMessageFilter<TestSaga, TestMessage> filter = CreateFilter(combinedRole);

        ArgumentNullException missingContext = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            filter.SendAsync(null!, next));
        ArgumentNullException bothMissing = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            filter.SendAsync(null!, null!));
        ArgumentNullException missingNext = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            filter.SendAsync(context, null!));

        cancellation.Cancel();
        ArgumentNullException canceledMissingNext = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            filter.SendAsync(context, null!));

        Assert.Equal("context", missingContext.ParamName);
        Assert.Equal("context", bothMissing.ParamName);
        Assert.Equal("next", missingNext.ParamName);
        Assert.Equal("next", canceledMissingNext.ParamName);
        Assert.Equal(0, saga.ConsumeCount);
        Assert.Equal(0, next.SendCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "initiation-filter-probe-null-and-compatible-metadata")]
    public void Probes_RejectNullAndRetainCompatibleMetadata()
    {
        AssertProbe(
            new InitiatedBySagaMessageFilter<TestSaga, TestMessage>(),
            "initiatedBy");
        AssertProbe(
            new InitiatedByOrOrchestratesSagaMessageFilter<TestSaga, TestMessage>(),
            "initiatedByOrOrchestrates");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "initiation-filter-pre-cancellation-before-user-code")]
    public async Task PreCancellation_StopsBeforeSagaAndContinuationAsync(bool combinedRole)
    {
        ILogContext? previousLogContext = LogContext.Current;
        await using ServiceProvider provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        using var observations = new MetricObservationSession(provider.GetRequiredService<IMeterFactory>());
        using var cancellation = new CancellationTokenSource();

        try
        {
            LogContext.Current = null;
            LogContext.ConfigureCurrentLogContextIfNull(provider);

            cancellation.Cancel();
            var saga = new TestSaga(Guid.NewGuid(), _ => Task.CompletedTask);
            TrackingSagaContext context = CreateContext(saga, cancellation.Token);
            var next = new RecordingPipe(_ => Task.CompletedTask);

            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => CreateFilter(combinedRole).SendAsync(context, next));

            Assert.Equal(cancellation.Token, actual.CancellationToken);
            Assert.Equal(0, saga.ConsumeCount);
            Assert.Equal(0, next.SendCount);
            Assert.Empty(observations.Measurements);
        }
        finally
        {
            LogContext.Current = previousLogContext;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "initiation-filter-post-saga-cancellation-gate")]
    public async Task CancellationAfterSagaCompletion_StopsBeforeContinuationAsync(bool combinedRole)
    {
        using var cancellation = new CancellationTokenSource();
        var sagaEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSaga = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var events = new List<string>();
        var saga = new TestSaga(
            Guid.NewGuid(),
            async _ =>
            {
                events.Add("saga-started");
                sagaEntered.TrySetResult();
                await releaseSaga.Task;
                events.Add("saga-completed");
                cancellation.Cancel();
            });
        TrackingSagaContext context = CreateContext(saga, cancellation.Token);
        var next = new RecordingPipe(
            _ =>
            {
                events.Add("next");
                return Task.CompletedTask;
            });

        Task operation = CreateFilter(combinedRole).SendAsync(context, next);
        try
        {
            await sagaEntered.Task.WaitAsync(TestContext.Current.CancellationToken);
            Assert.False(operation.IsCompleted);
            Assert.Equal(["saga-started"], events);

            releaseSaga.TrySetResult();

            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => operation.WaitAsync(TestContext.Current.CancellationToken));

            Assert.Equal(cancellation.Token, actual.CancellationToken);
            Assert.Equal(["saga-started", "saga-completed"], events);
            Assert.Equal(1, saga.ConsumeCount);
            Assert.Equal(0, next.SendCount);
        }
        finally
        {
            releaseSaga.TrySetResult();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "initiation-filter-concurrent-exact-order-and-correlation")]
    public async Task ConcurrentDispatch_IsolatedAndExactlyOnceInSagaThenContinuationOrderAsync(bool combinedRole)
    {
        const int DispatchCount = 16;
        var releaseSagas = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var fixtures = Enumerable.Range(0, DispatchCount)
            .Select(_ => CreateDispatchFixture(releaseSagas.Task))
            .ToArray();
        ISagaMessageFilter<TestSaga, TestMessage> filter = CreateFilter(combinedRole);

        Task[] operations = fixtures
            .Select(fixture => filter.SendAsync(fixture.Context, fixture.Next))
            .ToArray();

        try
        {
            Assert.All(fixtures, fixture =>
            {
                Assert.Equal(1, fixture.Saga.ConsumeCount);
                Assert.Equal(0, fixture.Next.SendCount);
                Assert.Same(fixture.Context, fixture.Saga.Context);
            });

            releaseSagas.SetResult();
            await Task.WhenAll(operations).WaitAsync(TestContext.Current.CancellationToken);
        }
        finally
        {
            releaseSagas.TrySetResult();
        }

        Assert.All(fixtures, fixture =>
        {
            Assert.Equal(["saga", "next"], fixture.Events);
            Assert.Equal(1, fixture.Saga.ConsumeCount);
            Assert.Equal(1, fixture.Next.SendCount);
            Assert.Same(fixture.Context, fixture.Next.Context);
            Assert.Equal(fixture.Saga.CorrelationId, fixture.Context.CorrelationId);
            Assert.Equal(fixture.Saga.CorrelationId, fixture.Context.Message.CorrelationId);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "initiation-filter-null-saga-task-owned-failure")]
    public async Task NullSagaTask_FailsAtTheSagaBoundaryWithoutContinuingAsync(bool combinedRole)
    {
        var saga = new TestSaga(Guid.NewGuid(), _ => null);
        TrackingSagaContext context = CreateContext(saga, TestContext.Current.CancellationToken);
        var next = new RecordingPipe(_ => Task.CompletedTask);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateFilter(combinedRole).SendAsync(context, next));

        Assert.Equal("The saga returned a null task from ConsumeAsync.", actual.Message);
        Assert.Equal(1, saga.ConsumeCount);
        Assert.Equal(0, next.SendCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "initiation-filter-null-continuation-task-owned-failure")]
    public async Task NullContinuationTask_FailsAfterExactlyOneSagaInvocationAsync(bool combinedRole)
    {
        var saga = new TestSaga(Guid.NewGuid(), _ => Task.CompletedTask);
        TrackingSagaContext context = CreateContext(saga, TestContext.Current.CancellationToken);
        var next = new RecordingPipe(_ => null);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateFilter(combinedRole).SendAsync(context, next));

        Assert.Equal("The saga-message continuation returned a null task from SendAsync.", actual.Message);
        Assert.Equal(1, saga.ConsumeCount);
        Assert.Equal(1, next.SendCount);
        Assert.Same(context, next.Context);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "initiation-filter-original-failure-identity-by-stage")]
    public async Task StageFailure_PreservesOriginalIdentityAndStopsAtTheCausalStageAsync(
        bool combinedRole,
        bool continuationFails)
    {
        var failure = new ExpectedFailure(continuationFails ? "continuation failed" : "saga failed");
        var saga = new TestSaga(
            Guid.NewGuid(),
            _ => continuationFails ? Task.CompletedTask : Task.FromException(failure));
        TrackingSagaContext context = CreateContext(saga, TestContext.Current.CancellationToken);
        var next = new RecordingPipe(
            _ => continuationFails ? Task.FromException(failure) : Task.CompletedTask);

        ExpectedFailure actual = await Assert.ThrowsAsync<ExpectedFailure>(
            () => CreateFilter(combinedRole).SendAsync(context, next));

        Assert.Same(failure, actual);
        Assert.Equal(1, saga.ConsumeCount);
        Assert.Equal(continuationFails ? 1 : 0, next.SendCount);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "initiation-filter-synchronous-failure-identity")]
    public async Task SynchronousStageFailure_PreservesOriginalIdentityAndExactOrderingAsync(
        bool combinedRole,
        bool continuationFails)
    {
        var events = new List<string>();
        var failure = new ExpectedFailure(
            continuationFails ? "continuation failed synchronously" : "saga failed synchronously");
        var saga = new TestSaga(
            Guid.NewGuid(),
            _ =>
            {
                events.Add("saga");
                return continuationFails ? Task.CompletedTask : throw failure;
            });
        TrackingSagaContext context = CreateContext(saga, TestContext.Current.CancellationToken);
        var next = new RecordingPipe(
            _ =>
            {
                events.Add("next");
                return continuationFails ? throw failure : Task.CompletedTask;
            });

        ExpectedFailure actual = await Assert.ThrowsAsync<ExpectedFailure>(() =>
            CreateFilter(combinedRole).SendAsync(context, next));

        Assert.Same(failure, actual);
        Assert.Equal(continuationFails ? new[] { "saga", "next" } : new[] { "saga" }, events);
        Assert.Equal(1, saga.ConsumeCount);
        Assert.Equal(continuationFails ? 1 : 0, next.SendCount);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "initiation-filter-canceled-collaborator-task-token")]
    public async Task CanceledStageTask_PreservesItsUnrelatedTokenWhileDeliveryRemainsActiveAsync(
        bool combinedRole,
        bool continuationCancels)
    {
        using var collaboratorCancellation = new CancellationTokenSource();
        collaboratorCancellation.Cancel();
        var saga = new TestSaga(
            Guid.NewGuid(),
            _ => continuationCancels
                ? Task.CompletedTask
                : Task.FromCanceled(collaboratorCancellation.Token));
        TrackingSagaContext context = CreateContext(saga, CancellationToken.None);
        var next = new RecordingPipe(
            _ => continuationCancels
                ? Task.FromCanceled(collaboratorCancellation.Token)
                : Task.CompletedTask);

        Task operation = CreateFilter(combinedRole).SendAsync(context, next);
        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);

        Assert.Equal(collaboratorCancellation.Token, actual.CancellationToken);
        Assert.True(operation.IsCanceled);
        Assert.False(context.CancellationToken.IsCancellationRequested);
        Assert.Equal(1, saga.ConsumeCount);
        Assert.Equal(continuationCancels ? 1 : 0, next.SendCount);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "initiation-filter-operation-cancellation-identity-and-token")]
    public async Task OperationCancellation_PreservesExactExceptionIdentityForDeliveryAndUnrelatedTokensAsync(
        bool combinedRole,
        bool continuationCancels,
        bool deliveryCancels)
    {
        ILogContext? previousLogContext = LogContext.Current;
        await using ServiceProvider provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        using var observations = new MetricObservationSession(provider.GetRequiredService<IMeterFactory>());
        using var deliveryCancellation = new CancellationTokenSource();
        using var collaboratorCancellation = new CancellationTokenSource();

        try
        {
            LogContext.Current = null;
            LogContext.ConfigureCurrentLogContextIfNull(provider);

            collaboratorCancellation.Cancel();
            CancellationToken expectedToken = deliveryCancels
                ? deliveryCancellation.Token
                : collaboratorCancellation.Token;
            var failure = new OperationCanceledException("causal cancellation", expectedToken);

            Task CancelStageAsync()
            {
                if (deliveryCancels)
                    deliveryCancellation.Cancel();

                return Task.FromException(failure);
            }

            var saga = new TestSaga(
                Guid.NewGuid(),
                _ => continuationCancels ? Task.CompletedTask : CancelStageAsync());
            TrackingSagaContext context = CreateContext(saga, deliveryCancellation.Token);
            var next = new RecordingPipe(
                _ => continuationCancels ? CancelStageAsync() : Task.CompletedTask);

            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                CreateFilter(combinedRole).SendAsync(context, next));

            Assert.Same(failure, actual);
            Assert.Equal(expectedToken, actual.CancellationToken);
            Assert.Equal(deliveryCancels, context.CancellationToken.IsCancellationRequested);
            Assert.Equal(1, saga.ConsumeCount);
            Assert.Equal(continuationCancels ? 1 : 0, next.SendCount);

            MetricMeasurement[] metrics = observations.Measurements.ToArray();
            MetricMeasurement[] activeOperations = metrics
                .Where(measurement => measurement.Name == ServiceBusTelemetry.Metrics.ActiveOperations)
                .ToArray();
            Assert.Equal([1d, -1d], activeOperations.Select(measurement => measurement.Value));
            Assert.All(activeOperations, AssertSagaMetricTags);

            MetricMeasurement completion = Assert.Single(metrics, measurement =>
                measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration);
            AssertSagaMetricTags(completion);
            if (deliveryCancels)
            {
                Assert.DoesNotContain(completion.Tags, tag =>
                    tag.Key == ServiceBusTelemetry.Attributes.ErrorType);
            }
            else
            {
                Assert.Equal(
                    typeof(OperationCanceledException).FullName,
                    Assert.Single(
                        completion.Tags,
                        tag => tag.Key == ServiceBusTelemetry.Attributes.ErrorType).Value);
            }
        }
        finally
        {
            LogContext.Current = previousLogContext;
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "initiation-filter-metrics-start-failure-and-completion")]
    public async Task Metrics_StartRecordStageFailureAndCompleteForBothFiltersAsync(
        bool combinedRole,
        bool continuationFails)
    {
        ILogContext? previous = LogContext.Current;
        await using ServiceProvider provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        IMeterFactory meterFactory = provider.GetRequiredService<IMeterFactory>();
        using var observations = new MetricObservationSession(meterFactory);
        var releaseSaga = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            LogContext.Current = null;
            LogContext.ConfigureCurrentLogContextIfNull(provider);

            var sagaEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var successfulSaga = new TestSaga(
                Guid.NewGuid(),
                _ =>
                {
                    sagaEntered.TrySetResult();
                    return releaseSaga.Task;
                });
            TrackingSagaContext successfulContext = CreateContext(
                successfulSaga,
                TestContext.Current.CancellationToken);
            var successfulNext = new RecordingPipe(_ => Task.CompletedTask);

            Task successfulOperation = CreateFilter(combinedRole).SendAsync(successfulContext, successfulNext);
            try
            {
                await sagaEntered.Task.WaitAsync(TestContext.Current.CancellationToken);

                MetricMeasurement started = Assert.Single(observations.Measurements, measurement =>
                    measurement.Name == ServiceBusTelemetry.Metrics.ActiveOperations
                    && measurement.Value == 1);
                Assert.Equal("saga", started.Tag(ServiceBusTelemetry.Attributes.OperationName));
                Assert.Equal("process", started.Tag(ServiceBusTelemetry.Attributes.OperationType));
                Assert.Equal("saga", started.Tag(ServiceBusTelemetry.Attributes.ProcessorKind));
                Assert.DoesNotContain(observations.Measurements, measurement =>
                    measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration);
            }
            finally
            {
                releaseSaga.TrySetResult();
            }

            await successfulOperation.WaitAsync(TestContext.Current.CancellationToken);

            MetricMeasurement successfulCompletion = Assert.Single(observations.Measurements, measurement =>
                measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration);
            Assert.DoesNotContain(successfulCompletion.Tags, tag =>
                tag.Key == ServiceBusTelemetry.Attributes.ErrorType);
            Assert.Equal(
                [1d, -1d],
                observations.Measurements
                    .Where(measurement => measurement.Name == ServiceBusTelemetry.Metrics.ActiveOperations)
                    .Select(measurement => measurement.Value));
            Assert.Equal(1, successfulSaga.ConsumeCount);
            Assert.Equal(1, successfulNext.SendCount);

            var failure = new ExpectedFailure(continuationFails ? "continuation metrics failure" : "saga metrics failure");
            var failingSaga = new TestSaga(
                Guid.NewGuid(),
                _ => continuationFails ? Task.CompletedTask : Task.FromException(failure));
            TrackingSagaContext failingContext = CreateContext(failingSaga, TestContext.Current.CancellationToken);
            var failingNext = new RecordingPipe(
                _ => continuationFails ? Task.FromException(failure) : Task.CompletedTask);

            ExpectedFailure actual = await Assert.ThrowsAsync<ExpectedFailure>(() =>
                CreateFilter(combinedRole).SendAsync(failingContext, failingNext));

            Assert.Same(failure, actual);
            MetricMeasurement faultedCompletion = Assert.Single(observations.Measurements, measurement =>
                measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration
                && measurement.Tags.Any(tag => tag.Key == ServiceBusTelemetry.Attributes.ErrorType));
            Assert.Equal(
                typeof(ExpectedFailure).FullName,
                faultedCompletion.Tag(ServiceBusTelemetry.Attributes.ErrorType));
            Assert.Equal(
                [1d, -1d, 1d, -1d],
                observations.Measurements
                    .Where(measurement => measurement.Name == ServiceBusTelemetry.Metrics.ActiveOperations)
                    .Select(measurement => measurement.Value));
            Assert.Equal(2, observations.Measurements.Count(measurement =>
                measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration));
            Assert.Equal(1, failingSaga.ConsumeCount);
            Assert.Equal(continuationFails ? 1 : 0, failingNext.SendCount);
        }
        finally
        {
            releaseSaga.TrySetResult();
            LogContext.Current = previous;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "initiation-filter-activity-success")]
    public async Task SuccessfulDispatch_EmitsOneCorrelatedSagaActivityAsync(bool combinedRole)
    {
        var saga = new TestSaga(Guid.NewGuid(), _ => Task.CompletedTask);
        TrackingSagaContext context = CreateContext(saga, TestContext.Current.CancellationToken);
        var next = new RecordingPipe(_ => Task.CompletedTask);
        using var activities = new ActivityCapture();

        await CreateFilter(combinedRole).SendAsync(context, next);

        Activity process = activities.SingleProcess(saga.CorrelationId);
        Assert.Equal(activities.Caller.TraceId, process.TraceId);
        Assert.Equal(activities.Caller.SpanId, process.ParentSpanId);
        Assert.Equal(ActivityKind.Consumer, process.Kind);
        Assert.Equal(ActivityStatusCode.Ok, process.Status);
        Assert.Equal("process", process.GetTagItem(ServiceBusTelemetry.Attributes.OperationType));
        Assert.Equal(saga.CorrelationId.ToString("D"), process.GetTagItem(ServiceBusTelemetry.Attributes.SagaId));
        Assert.Equal(TypeCache<TestSaga>.ShortName, process.GetTagItem(ServiceBusTelemetry.Attributes.ProcessorName));
        Assert.Equal(MessageTypeCache<TestMessage>.DiagnosticAddress,
            process.GetTagItem(ServiceBusTelemetry.Attributes.MessageContract));
        Assert.DoesNotContain(process.Events, activityEvent => activityEvent.Name == ServiceBusTelemetry.Events.Exception);
        Assert.Equal(1, saga.ConsumeCount);
        Assert.Equal(1, next.SendCount);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "initiation-filter-activity-failure-and-causal-identity")]
    public async Task FaultedDispatch_RecordsOneExceptionActivityAndPreservesApplicationFailureAsync(
        bool combinedRole,
        bool continuationFails)
    {
        var failure = new ExpectedFailure(continuationFails ? "active continuation failure" : "active saga failure");
        var saga = new TestSaga(
            Guid.NewGuid(),
            _ => continuationFails ? Task.CompletedTask : throw failure);
        TrackingSagaContext context = CreateContext(saga, TestContext.Current.CancellationToken);
        var next = new RecordingPipe(
            _ => continuationFails ? throw failure : Task.CompletedTask);
        using var activities = new ActivityCapture();

        ExpectedFailure actual = await Assert.ThrowsAsync<ExpectedFailure>(() =>
            CreateFilter(combinedRole).SendAsync(context, next));

        Assert.Same(failure, actual);
        Activity process = activities.SingleProcess(saga.CorrelationId);
        Assert.Equal(ActivityStatusCode.Error, process.Status);
        Assert.Equal(failure.Message, process.StatusDescription);
        ActivityEvent exceptionEvent = Assert.Single(
            process.Events,
            activityEvent => activityEvent.Name == ServiceBusTelemetry.Events.Exception);
        Assert.Equal(failure.Message, AssertEventTag(exceptionEvent, ServiceBusTelemetry.Attributes.ExceptionMessage));
        Assert.Equal(TypeCache<ExpectedFailure>.ShortName,
            AssertEventTag(exceptionEvent, ServiceBusTelemetry.Attributes.ExceptionType));
        Assert.NotEmpty(Assert.IsType<string>(
            AssertEventTag(exceptionEvent, ServiceBusTelemetry.Attributes.ExceptionStackTrace)));
        Assert.Equal(1, saga.ConsumeCount);
        Assert.Equal(continuationFails ? 1 : 0, next.SendCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "initiation-filter-activity-delivery-cancellation")]
    public async Task DeliveryCancellation_StopsActivityWithoutMisreportingAnApplicationFailureAsync(bool combinedRole)
    {
        using var cancellation = new CancellationTokenSource();
        var saga = new TestSaga(
            Guid.NewGuid(),
            _ =>
            {
                cancellation.Cancel();
                return Task.CompletedTask;
            });
        TrackingSagaContext context = CreateContext(saga, cancellation.Token);
        var next = new RecordingPipe(_ => Task.CompletedTask);
        using var activities = new ActivityCapture();

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateFilter(combinedRole).SendAsync(context, next));

        Assert.Equal(cancellation.Token, actual.CancellationToken);
        Activity process = activities.SingleProcess(saga.CorrelationId);
        Assert.Equal(ActivityStatusCode.Ok, process.Status);
        Assert.DoesNotContain(process.Events, activityEvent => activityEvent.Name == ServiceBusTelemetry.Events.Exception);
        Assert.Equal(1, saga.ConsumeCount);
        Assert.Equal(0, next.SendCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "initiation-filter-activity-unrelated-cancellation")]
    public async Task UnrelatedCancellation_WithActiveDelivery_RecordsFailureAndPreservesIdentityAsync(bool combinedRole)
    {
        using var collaboratorCancellation = new CancellationTokenSource();
        collaboratorCancellation.Cancel();
        var failure = new OperationCanceledException("dependency canceled", collaboratorCancellation.Token);
        var saga = new TestSaga(Guid.NewGuid(), _ => Task.FromException(failure));
        TrackingSagaContext context = CreateContext(saga, CancellationToken.None);
        var next = new RecordingPipe(_ => Task.CompletedTask);
        using var activities = new ActivityCapture();

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateFilter(combinedRole).SendAsync(context, next));

        Assert.Same(failure, actual);
        Assert.Equal(collaboratorCancellation.Token, actual.CancellationToken);
        Assert.False(context.CancellationToken.IsCancellationRequested);
        Activity process = activities.SingleProcess(saga.CorrelationId);
        Assert.Equal(ActivityStatusCode.Error, process.Status);
        ActivityEvent exceptionEvent = Assert.Single(
            process.Events,
            activityEvent => activityEvent.Name == ServiceBusTelemetry.Events.Exception);
        Assert.Equal(failure.Message, AssertEventTag(exceptionEvent, ServiceBusTelemetry.Attributes.ExceptionMessage));
        Assert.Equal(TypeCache<OperationCanceledException>.ShortName,
            AssertEventTag(exceptionEvent, ServiceBusTelemetry.Attributes.ExceptionType));
        Assert.Equal(1, saga.ConsumeCount);
        Assert.Equal(0, next.SendCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "initiation-filter-activity-pre-cancellation")]
    public async Task PreCanceledDispatch_DoesNotCreateASagaActivityAsync(bool combinedRole)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var saga = new TestSaga(Guid.NewGuid(), _ => Task.CompletedTask);
        TrackingSagaContext context = CreateContext(saga, cancellation.Token);
        var next = new RecordingPipe(_ => Task.CompletedTask);
        using var activities = new ActivityCapture();

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateFilter(combinedRole).SendAsync(context, next));

        Assert.Equal(cancellation.Token, actual.CancellationToken);
        Assert.DoesNotContain(activities.Stopped, activity => activities.IsProcess(activity, saga.CorrelationId));
        Assert.Equal(0, saga.ConsumeCount);
        Assert.Equal(0, next.SendCount);
    }

    private static ISagaMessageFilter<TestSaga, TestMessage> CreateFilter(bool combinedRole) =>
        combinedRole
            ? new InitiatedByOrOrchestratesSagaMessageFilter<TestSaga, TestMessage>()
            : new InitiatedBySagaMessageFilter<TestSaga, TestMessage>();

    private static void AssertFilterApi(Type closedType, Type openType, Type sagaRoleDefinition)
    {
        Assert.True(closedType.IsPublic);
        Assert.False(closedType.IsAbstract);
        Assert.False(closedType.IsSealed);
        Assert.Equal(openType, closedType.GetGenericTypeDefinition());
        Assert.Contains(typeof(ISagaMessageFilter<TestSaga, TestMessage>), closedType.GetInterfaces());

        ConstructorInfo constructor = Assert.Single(closedType.GetConstructors(BindingFlags.Instance | BindingFlags.Public));
        Assert.Empty(constructor.GetParameters());

        MethodInfo send = Assert.Single(closedType.GetMethods(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly));
        Assert.Equal("SendAsync", send.Name);
        Assert.Equal(typeof(Task), send.ReturnType);
        ParameterInfo[] sendParameters = send.GetParameters();
        Assert.Equal(2, sendParameters.Length);
        Assert.Equal(typeof(SagaConsumeContext<TestSaga, TestMessage>), sendParameters[0].ParameterType);
        Assert.Equal("context", sendParameters[0].Name);
        Assert.Equal(typeof(IPipe<SagaConsumeContext<TestSaga, TestMessage>>), sendParameters[1].ParameterType);
        Assert.Equal("next", sendParameters[1].Name);

        Type[] genericParameters = openType.GetGenericArguments();
        Assert.Equal(2, genericParameters.Length);
        Assert.Equal(["TSaga", "TMessage"], genericParameters.Select(parameter => parameter.Name));
        Type sagaParameter = genericParameters[0];
        Type messageParameter = genericParameters[1];
        Assert.Equal(
            GenericParameterAttributes.ReferenceTypeConstraint,
            sagaParameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Equal(
            GenericParameterAttributes.ReferenceTypeConstraint,
            messageParameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);

        Type[] sagaConstraints = sagaParameter.GetGenericParameterConstraints();
        Assert.Equal(2, sagaConstraints.Length);
        Assert.Contains(typeof(ISaga), sagaConstraints);
        Type sagaRole = Assert.Single(
            sagaConstraints,
            constraint => constraint.IsGenericType
                && constraint.GetGenericTypeDefinition() == sagaRoleDefinition);
        Assert.Same(messageParameter, Assert.Single(sagaRole.GetGenericArguments()));

        Type messageConstraint = Assert.Single(messageParameter.GetGenericParameterConstraints());
        Assert.Equal(typeof(ICorrelatedBy<Guid>), messageConstraint);
    }

    private static object? AssertEventTag(ActivityEvent activityEvent, string key) =>
        Assert.Single(activityEvent.Tags, tag => tag.Key == key).Value;

    private static void AssertSagaMetricTags(MetricMeasurement measurement)
    {
        Assert.Equal("saga", measurement.Tag(ServiceBusTelemetry.Attributes.OperationName));
        Assert.Equal("process", measurement.Tag(ServiceBusTelemetry.Attributes.OperationType));
        Assert.Equal("saga", measurement.Tag(ServiceBusTelemetry.Attributes.ProcessorKind));
    }

    private static TrackingSagaContext CreateContext(TestSaga saga, CancellationToken cancellationToken)
    {
        ConsumeContext<TestMessage> messageContext = InMemoryOutboxTestContextFactory.Create(
            new TestMessage(saga.CorrelationId),
            cancellationToken);
        return new TrackingSagaContext(messageContext, saga);
    }

    private static DispatchFixture CreateDispatchFixture(Task sagaCompletion)
    {
        var events = new List<string>();
        var saga = new TestSaga(
            Guid.NewGuid(),
            _ =>
            {
                events.Add("saga");
                return sagaCompletion;
            });
        TrackingSagaContext context = CreateContext(saga, TestContext.Current.CancellationToken);
        var next = new RecordingPipe(
            _ =>
            {
                events.Add("next");
                return Task.CompletedTask;
            });
        return new DispatchFixture(context, saga, next, events);
    }

    private static void AssertProbe(IProbeSite filter, string expectedFilterType)
    {
        ArgumentNullException missing = Assert.Throws<ArgumentNullException>(() => filter.Probe(null!));
        Assert.Equal("context", missing.ParamName);

        IProbeResult result = filter.GetProbeResult(TestContext.Current.CancellationToken);
        IReadOnlyDictionary<string, object> scope = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(
            Assert.Contains("filters", result.Results));
        Assert.Equal(expectedFilterType, Assert.Contains("filterType", scope));
        Assert.Equal(
            $"Consume({TypeCache<TestMessage>.ShortName} message)",
            Assert.Contains("method", scope));
    }

    private sealed class ActivityCapture : IDisposable
    {
        private readonly ConcurrentQueue<Activity> _stopped = new();
        private readonly ActivityListener _listener;
        private readonly ActivitySource _callerSource;

        public ActivityCapture()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source =>
                    source.Name == ServiceBusTelemetry.ActivitySourceName || source.Name == CallerActivitySource,
                Sample = (ref ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = _stopped.Enqueue,
            };
            ActivitySource.AddActivityListener(_listener);
            _callerSource = new ActivitySource(CallerActivitySource);
            Caller = Assert.IsType<Activity>(_callerSource.StartActivity("caller", ActivityKind.Internal));
        }

        public Activity Caller { get; }

        public IReadOnlyCollection<Activity> Stopped => _stopped.ToArray();

        public bool IsProcess(Activity activity, Guid correlationId) =>
            activity.Source.Name == ServiceBusTelemetry.ActivitySourceName
            && activity.TraceId == Caller.TraceId
            && activity.ParentSpanId == Caller.SpanId
            && Equals(
                activity.GetTagItem(ServiceBusTelemetry.Attributes.SagaId),
                correlationId.ToString("D"));

        public Activity SingleProcess(Guid correlationId) =>
            Assert.Single(_stopped, activity => IsProcess(activity, correlationId));

        public void Dispose()
        {
            Caller.Dispose();
            _callerSource.Dispose();
            _listener.Dispose();
        }
    }

    public sealed record TestMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    private sealed record DispatchFixture(
        TrackingSagaContext Context,
        TestSaga Saga,
        RecordingPipe Next,
        List<string> Events);

    private sealed class TestSaga(
        Guid correlationId,
        Func<ConsumeContext<TestMessage>, Task?> consume) :
        ISaga,
        IInitiatedBy<TestMessage>,
        IInitiatedByOrOrchestrates<TestMessage>
    {
        private int _consumeCount;

        public Guid CorrelationId { get; set; } = correlationId;

        public ConsumeContext<TestMessage>? Context { get; private set; }

        public int ConsumeCount => Volatile.Read(ref _consumeCount);

        public Task ConsumeAsync(ConsumeContext<TestMessage> context)
        {
            Context = context;
            Interlocked.Increment(ref _consumeCount);
            return consume(context)!;
        }
    }

    private sealed class TrackingSagaContext(
        ConsumeContext<TestMessage> context,
        TestSaga saga) :
        ConsumeContextProxy<TestMessage>(context),
        SagaConsumeContext<TestSaga, TestMessage>
    {
        public override IEnumerable<string> SupportedMessageTypes => [MessageUrn.ForTypeString<TestMessage>()];

        public override Guid? CorrelationId => Saga.CorrelationId;

        public TestSaga Saga { get; } = saga;

        public bool IsCompleted { get; private set; }

        public Task SetCompletedAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IsCompleted = true;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingPipe(
        Func<SagaConsumeContext<TestSaga, TestMessage>, Task?> send) :
        IPipe<SagaConsumeContext<TestSaga, TestMessage>>
    {
        private int _sendCount;

        public SagaConsumeContext<TestSaga, TestMessage>? Context { get; private set; }

        public int SendCount => Volatile.Read(ref _sendCount);

        public Task SendAsync(SagaConsumeContext<TestSaga, TestMessage> context)
        {
            Context = context;
            Interlocked.Increment(ref _sendCount);
            return send(context)!;
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class ExpectedFailure(string message) : Exception(message);
}
