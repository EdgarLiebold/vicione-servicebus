using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using ViciOne.ServiceBus.Tests.Monitoring;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class SagaObservationMessageFilterDeepContractTests
{
    private const string Requirement = "REQ-VSB-SAGA-PIPE-LAYERS";

    [Fact]
    [RequirementCoverage(Requirement, "iteration-211-public-extensibility-and-async-entry-surface")]
    public void Surface_PreservesPublicExtensibilityAndOneAsyncEntryPoint()
    {
        foreach ((string filterType, ISagaMessageFilter<TestSaga, TestMessage> filter) in CreateFilters())
        {
            Type type = filter.GetType();

            Assert.True(type.IsPublic);
            Assert.False(type.IsAbstract);
            Assert.False(type.IsSealed);
            Assert.Contains(typeof(ISagaMessageFilter<TestSaga, TestMessage>), type.GetInterfaces());
            Assert.Empty(Assert.Single(type.GetConstructors()).GetParameters());

            MethodInfo method = Assert.Single(type.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
            Assert.Equal("SendAsync", method.Name);
            Assert.Equal(typeof(Task), method.ReturnType);
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal(
                [typeof(SagaConsumeContext<TestSaga, TestMessage>), typeof(IPipe<SagaConsumeContext<TestSaga, TestMessage>>)],
                parameters.Select(parameter => parameter.ParameterType));
            Assert.Equal(["context", "next"], parameters.Select(parameter => parameter.Name!));

            AssertExactGenericConstraints(type.GetGenericTypeDefinition(), filterType == "observes");
        }
    }

    [Fact]
    [RequirementCoverage(Requirement, "iteration-211-probe-null-boundary-and-compatible-metadata")]
    public void Probe_RejectsMissingContextAndPublishesCompatibleFilterMetadata()
    {
        foreach ((string filterType, ISagaMessageFilter<TestSaga, TestMessage> filter) in CreateFilters())
        {
            ArgumentNullException missing = Assert.Throws<ArgumentNullException>(() => ((IProbeSite)filter).Probe(null!));
            Assert.Equal("context", missing.ParamName);

            IProbeResult result = filter.GetProbeResult(TestContext.Current.CancellationToken);
            IReadOnlyDictionary<string, object> scope = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(
                Assert.Contains("filters", result.Results));
            Assert.Equal(filterType, Assert.Contains("filterType", scope));
            Assert.Equal(
                $"Consume({TypeCache<TestMessage>.ShortName} message)",
                Assert.Contains("method", scope));
        }
    }

    [Fact]
    [RequirementCoverage(Requirement, "iteration-211-send-null-boundaries-before-collaborator-effects")]
    public async Task SendAsync_RejectsMissingInputsBeforeCollaboratorEffectsAsync()
    {
        foreach ((_, ISagaMessageFilter<TestSaga, TestMessage> filter) in CreateFilters())
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var saga = new TestSaga(NewId.NextGuid());
            SagaConsumeContext<TestSaga, TestMessage> context = CreateContext(saga, cancellation.Token);
            var next = new RecordingPipe();

            ArgumentNullException missingContext = await Assert.ThrowsAsync<ArgumentNullException>(
                () => filter.SendAsync(null!, null!).WaitAsync(TestContext.Current.CancellationToken));
            ArgumentNullException missingNext = await Assert.ThrowsAsync<ArgumentNullException>(
                () => filter.SendAsync(context, null!).WaitAsync(TestContext.Current.CancellationToken));

            Assert.Equal("context", missingContext.ParamName);
            Assert.Equal("next", missingNext.ParamName);
            Assert.Equal(0, saga.ConsumeCount);
            Assert.Equal(0, next.SendCount);
        }
    }

    [Fact]
    [RequirementCoverage(Requirement, "iteration-211-pre-cancellation-before-user-saga-code")]
    public async Task SendAsync_PreCanceledDeliveryStopsBeforeSagaAndContinuationAsync()
    {
        ILogContext? previousLogContext = LogContext.Current;
        await using ServiceProvider provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        using var metrics = new MetricObservationSession(provider.GetRequiredService<IMeterFactory>());

        try
        {
            LogContext.Current = null;
            LogContext.ConfigureCurrentLogContextIfNull(provider);
            AssertMetricCaptureEnabled(metrics);

            foreach ((_, ISagaMessageFilter<TestSaga, TestMessage> filter) in CreateFilters())
            {
                int metricOffset = metrics.Measurements.Count;
                using var cancellation = new CancellationTokenSource();
                cancellation.Cancel();
                var saga = new TestSaga(NewId.NextGuid());
                SagaConsumeContext<TestSaga, TestMessage> context = CreateContext(saga, cancellation.Token);
                var next = new RecordingPipe();

                OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => filter.SendAsync(context, next).WaitAsync(TestContext.Current.CancellationToken));

                Assert.Equal(cancellation.Token, failure.CancellationToken);
                Assert.Equal(0, saga.ConsumeCount);
                Assert.Equal(0, next.SendCount);
                Assert.Empty(metrics.Measurements.Skip(metricOffset));
            }
        }
        finally
        {
            LogContext.Current = previousLogContext;
        }
    }

    [Fact]
    [RequirementCoverage(Requirement, "iteration-211-cancellation-between-saga-and-continuation")]
    public async Task SendAsync_CancellationAfterSagaCompletionStopsBeforeContinuationAsync()
    {
        foreach ((_, ISagaMessageFilter<TestSaga, TestMessage> filter) in CreateFilters())
        {
            using var cancellation = new CancellationTokenSource();
            var sagaEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseSaga = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var saga = new TestSaga(NewId.NextGuid())
            {
                Consume = async _ =>
                {
                    sagaEntered.SetResult();
                    await releaseSaga.Task.WaitAsync(TestContext.Current.CancellationToken);
                    cancellation.Cancel();
                },
            };
            SagaConsumeContext<TestSaga, TestMessage> context = CreateContext(saga, cancellation.Token);
            var next = new RecordingPipe();

            Task operation = filter.SendAsync(context, next);
            try
            {
                await sagaEntered.Task.WaitAsync(TestContext.Current.CancellationToken);

                Assert.False(operation.IsCompleted);
                Assert.Equal(1, saga.ConsumeCount);
                Assert.Equal(0, next.SendCount);
            }
            finally
            {
                releaseSaga.TrySetResult();
            }
            OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => operation.WaitAsync(TestContext.Current.CancellationToken));

            Assert.Equal(cancellation.Token, failure.CancellationToken);
            Assert.Equal(1, saga.ConsumeCount);
            Assert.Equal(0, next.SendCount);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage(Requirement, "iteration-211-canceled-collaborator-task-identity-and-token-matrix")]
    public async Task SendAsync_CanceledCollaboratorTaskPreservesExactExceptionAndTokenAsync(
        bool continuationStage,
        bool contextToken)
    {
        ILogContext? previousLogContext = LogContext.Current;
        await using ServiceProvider provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        using var metrics = new MetricObservationSession(provider.GetRequiredService<IMeterFactory>());

        try
        {
            LogContext.Current = null;
            LogContext.ConfigureCurrentLogContextIfNull(provider);
            AssertMetricCaptureEnabled(metrics);

            foreach ((_, ISagaMessageFilter<TestSaga, TestMessage> filter) in CreateFilters())
            {
                int metricOffset = metrics.Measurements.Count;
                using var contextCancellation = new CancellationTokenSource();
                using var unrelatedCancellation = new CancellationTokenSource();
                if (!contextToken)
                    unrelatedCancellation.Cancel();

                CancellationToken expectedToken = contextToken
                    ? contextCancellation.Token
                    : unrelatedCancellation.Token;
                var expected = new OperationCanceledException(
                    "collaborator cancellation",
                    innerException: null,
                    token: expectedToken);
                Task? collaboratorTask = null;

                Task CancelAtCollaboratorAsync()
                {
                    if (contextToken)
                        contextCancellation.Cancel();

                    collaboratorTask = ThrowCancellationAsync(expected);
                    return collaboratorTask;
                }

                var saga = new TestSaga(NewId.NextGuid())
                {
                    Consume = _ => continuationStage ? Task.CompletedTask : CancelAtCollaboratorAsync(),
                };
                var next = new RecordingPipe
                {
                    Send = _ => continuationStage ? CancelAtCollaboratorAsync() : Task.CompletedTask,
                };
                SagaConsumeContext<TestSaga, TestMessage> context = CreateContext(saga, contextCancellation.Token);

                OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                    filter.SendAsync(context, next).WaitAsync(TestContext.Current.CancellationToken));

                Assert.Same(expected, actual);
                Assert.Equal(expectedToken, actual.CancellationToken);
                Assert.NotNull(collaboratorTask);
                Assert.True(collaboratorTask!.IsCanceled);
                Assert.Equal(contextToken, contextCancellation.IsCancellationRequested);
                Assert.Equal(!contextToken, unrelatedCancellation.IsCancellationRequested);
                Assert.Equal(1, saga.ConsumeCount);
                Assert.Equal(continuationStage ? 1 : 0, next.SendCount);

                MetricMeasurement[] emitted = metrics.Measurements.Skip(metricOffset).ToArray();
                Assert.Equal(
                    [1d, -1d],
                    emitted
                        .Where(measurement => measurement.Name == ServiceBusTelemetry.Metrics.ActiveOperations)
                        .Select(measurement => measurement.Value));
                MetricMeasurement completed = Assert.Single(emitted, measurement =>
                    measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration);
                AssertSagaMetricTags(completed);
                if (contextToken)
                {
                    Assert.DoesNotContain(
                        completed.Tags,
                        tag => tag.Key == ServiceBusTelemetry.Attributes.ErrorType);
                }
                else
                {
                    Assert.Equal(
                        typeof(OperationCanceledException).FullName,
                        Assert.Single(
                            completed.Tags,
                            tag => tag.Key == ServiceBusTelemetry.Attributes.ErrorType).Value);
                }
            }
        }
        finally
        {
            LogContext.Current = previousLogContext;
        }
    }

    [Fact]
    [RequirementCoverage(Requirement, "iteration-211-null-collaborator-tasks-have-owned-diagnostics")]
    public async Task SendAsync_NullCollaboratorTasksFailAtTheirOwnerWithoutAdvancingAsync()
    {
        foreach ((_, ISagaMessageFilter<TestSaga, TestMessage> filter) in CreateFilters())
        {
            var nullSagaTask = new TestSaga(NewId.NextGuid()) { Consume = _ => null! };
            var unreachableNext = new RecordingPipe();

            InvalidOperationException sagaFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                filter.SendAsync(CreateContext(nullSagaTask, TestContext.Current.CancellationToken), unreachableNext)
                    .WaitAsync(TestContext.Current.CancellationToken));

            Assert.Equal("The saga returned a null task from ConsumeAsync.", sagaFailure.Message);
            Assert.Equal(1, nullSagaTask.ConsumeCount);
            Assert.Equal(0, unreachableNext.SendCount);

            var successfulSaga = new TestSaga(NewId.NextGuid());
            var nullNextTask = new RecordingPipe { Send = _ => null! };

            InvalidOperationException nextFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                filter.SendAsync(CreateContext(successfulSaga, TestContext.Current.CancellationToken), nullNextTask)
                    .WaitAsync(TestContext.Current.CancellationToken));

            Assert.Equal("The saga-message continuation returned a null task from SendAsync.", nextFailure.Message);
            Assert.Equal(1, successfulSaga.ConsumeCount);
            Assert.Equal(1, nullNextTask.SendCount);
        }
    }

    [Fact]
    [RequirementCoverage(Requirement, "iteration-211-exactly-once-order-and-context-identity")]
    public async Task SendAsync_AwaitsSagaBeforeExactlyOneContinuationWithTheSameContextAsync()
    {
        foreach ((_, ISagaMessageFilter<TestSaga, TestMessage> filter) in CreateFilters())
        {
            var events = new List<string>();
            var sagaEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseSaga = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            ConsumeContext<TestMessage>? sagaContext = null;
            SagaConsumeContext<TestSaga, TestMessage>? nextContext = null;
            var saga = new TestSaga(NewId.NextGuid())
            {
                Consume = context =>
                {
                    sagaContext = context;
                    events.Add("saga");
                    sagaEntered.SetResult();
                    return releaseSaga.Task;
                },
            };
            var next = new RecordingPipe
            {
                Send = context =>
                {
                    nextContext = context;
                    events.Add("next");
                    return Task.CompletedTask;
                },
            };
            SagaConsumeContext<TestSaga, TestMessage> context = CreateContext(saga, TestContext.Current.CancellationToken);

            Task operation = filter.SendAsync(context, next);
            try
            {
                await sagaEntered.Task.WaitAsync(TestContext.Current.CancellationToken);

                Assert.Equal(["saga"], events);
                Assert.Equal(1, saga.ConsumeCount);
                Assert.Equal(0, next.SendCount);
            }
            finally
            {
                releaseSaga.TrySetResult();
            }
            await operation.WaitAsync(TestContext.Current.CancellationToken);

            Assert.Equal(["saga", "next"], events);
            Assert.Same(context, sagaContext);
            Assert.Same(context, nextContext);
            Assert.Equal(1, saga.ConsumeCount);
            Assert.Equal(1, next.SendCount);
        }
    }

    [Fact]
    [RequirementCoverage(Requirement, "iteration-211-original-saga-and-continuation-failure-identity")]
    public async Task SendAsync_PreservesOriginalSagaAndContinuationFailureIdentityAsync()
    {
        foreach ((_, ISagaMessageFilter<TestSaga, TestMessage> filter) in CreateFilters())
        {
            var sagaException = new MarkerException("saga");
            var failingSaga = new TestSaga(NewId.NextGuid())
            {
                Consume = _ => Task.FromException(sagaException),
            };
            var unreachableNext = new RecordingPipe();

            MarkerException observedSagaException = await Assert.ThrowsAsync<MarkerException>(() =>
                filter.SendAsync(CreateContext(failingSaga, TestContext.Current.CancellationToken), unreachableNext)
                    .WaitAsync(TestContext.Current.CancellationToken));

            Assert.Same(sagaException, observedSagaException);
            Assert.Equal(0, unreachableNext.SendCount);

            var nextException = new MarkerException("next");
            var successfulSaga = new TestSaga(NewId.NextGuid());
            var failingNext = new RecordingPipe { Send = _ => Task.FromException(nextException) };

            MarkerException observedNextException = await Assert.ThrowsAsync<MarkerException>(() =>
                filter.SendAsync(CreateContext(successfulSaga, TestContext.Current.CancellationToken), failingNext)
                    .WaitAsync(TestContext.Current.CancellationToken));

            Assert.Same(nextException, observedNextException);
            Assert.Equal(1, successfulSaga.ConsumeCount);
            Assert.Equal(1, failingNext.SendCount);
        }
    }

    [Fact]
    [RequirementCoverage(Requirement, "iteration-211-activity-success-and-stage-failure-contract")]
    public async Task SendAsync_ActivityRecordsSuccessAndStageFailureWithoutReplacingFailureAsync()
    {
        using var capture = new ActivityCapture();
        using ServiceProvider provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        using var metrics = new MetricObservationSession(provider.GetRequiredService<IMeterFactory>());
        ILogContext? previousLogContext = LogContext.Current;
        LogContext.ConfigureCurrentLogContext();
        LogContext.ConfigureCurrentLogContextIfNull(provider);

        try
        {
            foreach ((_, ISagaMessageFilter<TestSaga, TestMessage> filter) in CreateFilters())
            {
                int successMetricOffset = metrics.Measurements.Count;
                Guid successCorrelation = NewId.NextGuid();
                var sagaEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var releaseSaga = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var successfulSaga = new TestSaga(successCorrelation)
                {
                    Consume = async _ =>
                    {
                        sagaEntered.SetResult();
                        await releaseSaga.Task.WaitAsync(TestContext.Current.CancellationToken);
                    },
                };
                using (Activity caller = capture.StartCaller(successCorrelation))
                {
                    Task operation = filter.SendAsync(
                        CreateContext(successfulSaga, TestContext.Current.CancellationToken),
                        new RecordingPipe());
                    try
                    {
                        await sagaEntered.Task.WaitAsync(TestContext.Current.CancellationToken);

                        MetricMeasurement started = Assert.Single(
                            metrics.Measurements.Skip(successMetricOffset),
                            measurement =>
                                measurement.Name == ServiceBusTelemetry.Metrics.ActiveOperations
                                && measurement.Value == 1);
                        AssertSagaMetricTags(started);
                        Assert.DoesNotContain(
                            metrics.Measurements.Skip(successMetricOffset),
                            measurement => measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration);
                    }
                    finally
                    {
                        releaseSaga.TrySetResult();
                    }
                    await operation.WaitAsync(TestContext.Current.CancellationToken);

                    Activity process = capture.AssertSingleProcess(caller, successCorrelation);
                    Assert.Equal(ActivityStatusCode.Ok, process.Status);
                    Assert.DoesNotContain(process.Events, activityEvent =>
                        activityEvent.Name == ServiceBusTelemetry.Events.Exception);

                    MetricMeasurement[] successMetrics = metrics.Measurements.Skip(successMetricOffset).ToArray();
                    Assert.Equal(
                        [1d, -1d],
                        successMetrics
                            .Where(measurement => measurement.Name == ServiceBusTelemetry.Metrics.ActiveOperations)
                            .Select(measurement => measurement.Value));
                    MetricMeasurement completed = Assert.Single(successMetrics, measurement =>
                        measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration);
                    AssertSagaMetricTags(completed);
                    Assert.DoesNotContain(
                        completed.Tags,
                        tag => tag.Key == ServiceBusTelemetry.Attributes.ErrorType);
                }

                foreach (bool continuationStage in new[] { false, true })
                {
                    int failureMetricOffset = metrics.Measurements.Count;
                    Guid failureCorrelation = NewId.NextGuid();
                    var expected = new MarkerException(continuationStage ? "next" : "saga");
                    var saga = new TestSaga(failureCorrelation)
                    {
                        Consume = _ => continuationStage ? Task.CompletedTask : Task.FromException(expected),
                    };
                    var next = new RecordingPipe
                    {
                        Send = _ => continuationStage ? Task.FromException(expected) : Task.CompletedTask,
                    };

                    using Activity caller = capture.StartCaller(failureCorrelation);
                    MarkerException actual = await Assert.ThrowsAsync<MarkerException>(() =>
                        filter.SendAsync(CreateContext(saga, TestContext.Current.CancellationToken), next)
                            .WaitAsync(TestContext.Current.CancellationToken));

                    Assert.Same(expected, actual);
                    Activity process = capture.AssertSingleProcess(caller, failureCorrelation);
                    Assert.Equal(ActivityStatusCode.Error, process.Status);
                    ActivityEvent exceptionEvent = Assert.Single(process.Events, activityEvent =>
                        activityEvent.Name == ServiceBusTelemetry.Events.Exception);
                    KeyValuePair<string, object?> exceptionType = Assert.Single(exceptionEvent.Tags, tag =>
                        tag.Key == ServiceBusTelemetry.Attributes.ExceptionType);
                    Assert.Equal(TypeCache<MarkerException>.ShortName, Assert.IsType<string>(exceptionType.Value));

                    MetricMeasurement[] failureMetrics = metrics.Measurements.Skip(failureMetricOffset).ToArray();
                    Assert.Equal(
                        [1d, -1d],
                        failureMetrics
                            .Where(measurement => measurement.Name == ServiceBusTelemetry.Metrics.ActiveOperations)
                            .Select(measurement => measurement.Value));
                    MetricMeasurement faulted = Assert.Single(failureMetrics, measurement =>
                        measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration);
                    AssertSagaMetricTags(faulted);
                    Assert.Equal(
                        typeof(MarkerException).FullName,
                        Assert.Single(
                            faulted.Tags,
                            tag => tag.Key == ServiceBusTelemetry.Attributes.ErrorType).Value);
                }
            }
        }
        finally
        {
            LogContext.Current = previousLogContext;
        }
    }

    [Fact]
    [RequirementCoverage(Requirement, "iteration-211-activity-cancellation-and-pre-cancellation-contract")]
    public async Task SendAsync_ActivityTreatsContextCancellationAsNonFailureAndSkipsPreCanceledDeliveryAsync()
    {
        using var capture = new ActivityCapture();

        foreach ((_, ISagaMessageFilter<TestSaga, TestMessage> filter) in CreateFilters())
        {
            using (var cancellation = new CancellationTokenSource())
            {
                Guid correlationId = NewId.NextGuid();
                var saga = new TestSaga(correlationId)
                {
                    Consume = _ =>
                    {
                        cancellation.Cancel();
                        return Task.CompletedTask;
                    },
                };
                var next = new RecordingPipe();

                using Activity caller = capture.StartCaller(correlationId);
                OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                    filter.SendAsync(CreateContext(saga, cancellation.Token), next)
                        .WaitAsync(TestContext.Current.CancellationToken));

                Assert.Equal(cancellation.Token, actual.CancellationToken);
                Assert.Equal(0, next.SendCount);
                Activity process = capture.AssertSingleProcess(caller, correlationId);
                Assert.Equal(ActivityStatusCode.Ok, process.Status);
                Assert.DoesNotContain(process.Events, activityEvent =>
                    activityEvent.Name == ServiceBusTelemetry.Events.Exception);
            }

            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                Guid correlationId = NewId.NextGuid();
                var saga = new TestSaga(correlationId);
                var next = new RecordingPipe();

                using Activity caller = capture.StartCaller(correlationId);
                OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                    filter.SendAsync(CreateContext(saga, cancellation.Token), next)
                        .WaitAsync(TestContext.Current.CancellationToken));

                Assert.Equal(cancellation.Token, actual.CancellationToken);
                Assert.Equal(0, saga.ConsumeCount);
                Assert.Equal(0, next.SendCount);
                capture.AssertNoProcess(caller, correlationId);
            }
        }
    }

    [Fact]
    [RequirementCoverage(Requirement, "iteration-211-shared-instance-concurrent-correlation-isolation")]
    public async Task SendAsync_SharedFilterKeepsConcurrentCorrelationsAndContextsIsolatedAsync()
    {
        const int Count = 32;

        foreach ((_, ISagaMessageFilter<TestSaga, TestMessage> filter) in CreateFilters())
        {
            var sagas = new TestSaga[Count];
            var contexts = new SagaConsumeContext<TestSaga, TestMessage>[Count];
            var consumedContexts = new ConcurrentDictionary<Guid, ConsumeContext<TestMessage>>();
            var allSagasEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseSagas = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var enteredCount = 0;
            var next = new RecordingPipe();

            for (var index = 0; index < Count; index++)
            {
                Guid correlationId = CreateDeterministicCorrelationId(index + 1);
                var saga = new TestSaga(correlationId)
                {
                    Consume = context =>
                    {
                        consumedContexts.TryAdd(correlationId, context);
                        if (Interlocked.Increment(ref enteredCount) == Count)
                            allSagasEntered.SetResult();

                        return releaseSagas.Task;
                    },
                };
                sagas[index] = saga;
                contexts[index] = CreateContext(saga, TestContext.Current.CancellationToken);
            }

            Task[] operations = contexts.Select(context => filter.SendAsync(context, next)).ToArray();
            try
            {
                await allSagasEntered.Task.WaitAsync(TestContext.Current.CancellationToken);

                Assert.All(operations, operation => Assert.False(operation.IsCompleted));
                Assert.Equal(Count, consumedContexts.Count);
                Assert.Equal(0, next.SendCount);
                Assert.All(sagas, saga => Assert.Equal(1, saga.ConsumeCount));
            }
            finally
            {
                releaseSagas.TrySetResult();
                await Task.WhenAll(operations).WaitAsync(TestContext.Current.CancellationToken);
            }

            Assert.Equal(Count, consumedContexts.Count);
            Assert.Equal(Count, next.SendCount);
            SagaConsumeContext<TestSaga, TestMessage>[] continuedContexts = next.Contexts.ToArray();
            Assert.Equal(Count, continuedContexts.Select(context => context.CorrelationId).Distinct().Count());
            Assert.All(contexts, expected => Assert.Single(
                continuedContexts,
                actual => ReferenceEquals(expected, actual)));
            Assert.All(sagas, saga =>
            {
                Assert.Equal(1, saga.ConsumeCount);
                Assert.Equal(0, saga.CorrelationExpressionReadCount);
                SagaConsumeContext<TestSaga, TestMessage> expectedContext = contexts.Single(
                    context => context.CorrelationId == saga.CorrelationId);
                ConsumeContext<TestMessage> consumedContext = Assert.Contains(saga.CorrelationId, consumedContexts);
                Assert.Same(expectedContext, consumedContext);
                Assert.Equal(saga.CorrelationId, consumedContext.Message.CorrelationId);
            });
        }
    }

    private static (string FilterType, ISagaMessageFilter<TestSaga, TestMessage> Filter)[] CreateFilters() =>
    [
        ("observes", new ObservesSagaMessageFilter<TestSaga, TestMessage>()),
        ("orchestrates", new OrchestratesSagaMessageFilter<TestSaga, TestMessage>()),
    ];

    private static void AssertExactGenericConstraints(Type filterDefinition, bool observes)
    {
        Type[] parameters = filterDefinition.GetGenericArguments();
        Assert.Equal(["TSaga", "TMessage"], parameters.Select(parameter => parameter.Name));
        Assert.All(parameters, parameter => Assert.Equal(
            GenericParameterAttributes.ReferenceTypeConstraint,
            parameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask));

        Type sagaParameter = parameters[0];
        Type messageParameter = parameters[1];
        Type expectedRole = observes
            ? typeof(IObserves<,>).MakeGenericType(messageParameter, sagaParameter)
            : typeof(IOrchestrates<>).MakeGenericType(messageParameter);
        Type[] sagaConstraints = sagaParameter.GetGenericParameterConstraints();

        Assert.Equal(2, sagaConstraints.Length);
        Assert.Contains(typeof(ISaga), sagaConstraints);
        Assert.Contains(expectedRole, sagaConstraints);

        Type[] messageConstraints = messageParameter.GetGenericParameterConstraints();
        if (observes)
            Assert.Empty(messageConstraints);
        else
            Assert.Equal([typeof(ICorrelatedBy<Guid>)], messageConstraints);
    }

    private static void AssertSagaMetricTags(MetricMeasurement measurement)
    {
        Assert.Equal("saga", measurement.Tag(ServiceBusTelemetry.Attributes.OperationName));
        Assert.Equal("process", measurement.Tag(ServiceBusTelemetry.Attributes.OperationType));
        Assert.Equal("saga", measurement.Tag(ServiceBusTelemetry.Attributes.ProcessorKind));
    }

    private static void AssertMetricCaptureEnabled(MetricObservationSession metrics)
    {
        Assert.Contains(
            metrics.Instruments,
            instrument => instrument.Name == ServiceBusTelemetry.Metrics.ActiveOperations);
        Assert.Contains(
            metrics.Instruments,
            instrument => instrument.Name == ServiceBusTelemetry.Metrics.ProcessDuration);
    }

    private static async Task ThrowCancellationAsync(OperationCanceledException exception)
    {
        await Task.CompletedTask;
        throw exception;
    }

    private static Guid CreateDeterministicCorrelationId(int value) =>
        new(value, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    private static SagaConsumeContext<TestSaga, TestMessage> CreateContext(
        TestSaga saga,
        CancellationToken cancellationToken = default)
    {
        var message = new TestMessage(saga.CorrelationId);
        ConsumeContext<TestMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(
            message,
            cancellationToken,
            correlationId: message.CorrelationId);
        return new TrackingSagaContext(consumeContext, saga);
    }

    private sealed class TrackingSagaContext(ConsumeContext<TestMessage> context, TestSaga saga) :
        DefaultSagaConsumeContext<TestSaga, TestMessage>(context, saga)
    {
        public override IEnumerable<string> SupportedMessageTypes => [MessageUrn.ForTypeString<TestMessage>()];
    }

    public sealed record TestMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    private sealed class TestSaga(Guid correlationId) :
        ISaga,
        IObserves<TestMessage, TestSaga>,
        IOrchestrates<TestMessage>
    {
        private int _consumeCount;
        private int _correlationExpressionReadCount;

        public Guid CorrelationId { get; set; } = correlationId;

        public int ConsumeCount => Volatile.Read(ref _consumeCount);

        public int CorrelationExpressionReadCount => Volatile.Read(ref _correlationExpressionReadCount);

        public Func<ConsumeContext<TestMessage>, Task> Consume { get; init; } = _ => Task.CompletedTask;

        public Expression<Func<TestSaga, TestMessage, bool>> CorrelationExpression
        {
            get
            {
                Interlocked.Increment(ref _correlationExpressionReadCount);
                return (saga, message) => saga.CorrelationId == message.CorrelationId;
            }
        }

        public Task ConsumeAsync(ConsumeContext<TestMessage> context)
        {
            Interlocked.Increment(ref _consumeCount);
            return Consume(context);
        }
    }

    private sealed class RecordingPipe : IPipe<SagaConsumeContext<TestSaga, TestMessage>>
    {
        private int _sendCount;

        public ConcurrentQueue<SagaConsumeContext<TestSaga, TestMessage>> Contexts { get; } = new();

        public Func<SagaConsumeContext<TestSaga, TestMessage>, Task> Send { get; init; } = _ => Task.CompletedTask;

        public int SendCount => Volatile.Read(ref _sendCount);

        public void Probe(ProbeContext context) => ArgumentNullException.ThrowIfNull(context);

        public Task SendAsync(SagaConsumeContext<TestSaga, TestMessage> context)
        {
            Interlocked.Increment(ref _sendCount);
            Contexts.Enqueue(context);
            return Send(context);
        }
    }

    private sealed class ActivityCapture : IDisposable
    {
        private const string CallerSourceName = "ViciOne.ServiceBus.Tests.SagaObservationMessageFilterCaller";
        private readonly ActivitySource _callerSource = new(CallerSourceName);
        private readonly ActivityListener _listener;
        private readonly ConcurrentQueue<Activity> _stopped = new();

        public ActivityCapture()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source =>
                    source.Name == ServiceBusTelemetry.ActivitySourceName || source.Name == CallerSourceName,
                Sample = (ref ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = _stopped.Enqueue,
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public Activity StartCaller(Guid correlationId) => Assert.IsType<Activity>(
            _callerSource.StartActivity($"observation-filter-{correlationId:D}", ActivityKind.Internal));

        public Activity AssertSingleProcess(Activity caller, Guid correlationId) => Assert.Single(
            MatchingProcesses(caller, correlationId));

        public void AssertNoProcess(Activity caller, Guid correlationId) => Assert.Empty(
            MatchingProcesses(caller, correlationId));

        public void Dispose()
        {
            _callerSource.Dispose();
            _listener.Dispose();
        }

        private IEnumerable<Activity> MatchingProcesses(Activity caller, Guid correlationId) => _stopped.Where(activity =>
            activity.Source.Name == ServiceBusTelemetry.ActivitySourceName
            && activity.TraceId == caller.TraceId
            && activity.ParentSpanId == caller.SpanId
            && Equals(
                activity.GetTagItem(ServiceBusTelemetry.Attributes.CorrelationId),
                correlationId.ToString("D")));
    }

    private sealed class MarkerException(string message) : Exception(message);
}
