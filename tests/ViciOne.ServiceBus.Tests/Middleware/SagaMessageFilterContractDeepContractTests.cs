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
using ViciOne.ServiceBus.Tests.InternalAccess.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Monitoring;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class SagaMessageFilterContractDeepContractTests
{
    const string CallerActivitySource = "ViciOne.ServiceBus.Tests.SagaMessageFilterCaller";

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "message-filter-public-shape")]
    public void SagaMessageFilter_ExposesOnlyTheCanonicalConstrainedFilterContract()
    {
        Type contract = typeof(ISagaMessageFilter<,>);
        Type[] parameters = contract.GetGenericArguments();

        Assert.True(contract.IsPublic);
        Assert.True(contract.IsInterface);
        Assert.Equal(["TSaga", "TMessage"], parameters.Select(parameter => parameter.Name));
        Assert.Equal(GenericParameterAttributes.ReferenceTypeConstraint,
            parameters[0].GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Equal([typeof(ISaga)], parameters[0].GetGenericParameterConstraints());
        Assert.Equal(GenericParameterAttributes.ReferenceTypeConstraint,
            parameters[1].GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Empty(parameters[1].GetGenericParameterConstraints());

        Type filter = Assert.Single(contract.GetInterfaces(), type =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IFilter<>));
        Type sagaContext = Assert.Single(filter.GetGenericArguments());
        Assert.Equal(typeof(SagaConsumeContext<,>), sagaContext.GetGenericTypeDefinition());
        Assert.Same(parameters[0], sagaContext.GetGenericArguments()[0]);
        Assert.Same(parameters[1], sagaContext.GetGenericArguments()[1]);
        Assert.Empty(contract.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "message-filter-required-input-validation-precedence")]
    public async Task RequiredInputs_FailInDeterministicPrecedenceBeforeExecutionAsync()
    {
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        var inner = new ContractStateMachine();
        var machine = new DelegatingStateMachine(inner);
        ISagaMessageFilter<ContractSaga, ContractMessage> filter = CreateFilter(machine);
        CompletionSagaContext context = CreateContext(testCancellation);
        var next = new RecordingPipe();

        Assert.Equal("machine", Assert.Throws<ArgumentNullException>(() =>
            SagaStateMachineExecutionTestDriver.CreateMessageFilter<ContractSaga, ContractMessage>(null!, null!)).ParamName);
        Assert.Equal("event", Assert.Throws<ArgumentNullException>(() =>
            SagaStateMachineExecutionTestDriver.CreateMessageFilter<ContractSaga, ContractMessage>(inner, null!)).ParamName);

        ArgumentNullException missingContext = await Assert.ThrowsAsync<ArgumentNullException>(() => filter.SendAsync(null!, null!));
        ArgumentNullException missingNext = await Assert.ThrowsAsync<ArgumentNullException>(() => filter.SendAsync(context, null!));
        ArgumentNullException missingProbe = Assert.Throws<ArgumentNullException>(() => ((IProbeSite)filter).Probe(null!));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        CompletionSagaContext canceledContext = CreateContext(cancellation.Token);
        ArgumentNullException nextWinsCancellation = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            filter.SendAsync(canceledContext, null!));
        OperationCanceledException cancellationAfterValidation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            filter.SendAsync(canceledContext, next));

        Assert.Equal("context", missingContext.ParamName);
        Assert.Equal("next", missingNext.ParamName);
        Assert.Equal("context", missingProbe.ParamName);
        Assert.Equal("next", nextWinsCancellation.ParamName);
        Assert.Equal(cancellation.Token, cancellationAfterValidation.CancellationToken);
        Assert.Equal(0, machine.RaiseCount);
        Assert.Equal(0, next.SendCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "message-filter-probe-exact-event-filtering")]
    public void Probe_UsesCanonicalScopeAndIncludesOnlyStatesMatchingTheSelectedEvent(bool hasMatchingState)
    {
        var inner = new ContractStateMachine();
        IEvent<ContractMessage> selectedEvent = hasMatchingState ? inner.Signal : inner.Unused;
        var machine = new DelegatingStateMachine(inner, selectedEvent);
        ISagaMessageFilter<ContractSaga, ContractMessage> filter = CreateFilter(machine);
        var root = new RecordingProbeContext();

        ((IProbeSite)filter).Probe(root);

        RecordingProbeContext scope = Assert.Single(root.Children);
        Assert.Equal("filters", scope.Key);
        Assert.Equal("sagaStateMachine", scope.Values["filterType"]);
        Assert.Equal(selectedEvent.Name, scope.Values["Event"]);
        Assert.Equal(TypeCache<ContractMessage>.ShortName, scope.Values["DataType"]);
        Assert.Equal(TypeCache<ContractSaga>.ShortName, scope.Values["InstanceType"]);
        if (hasMatchingState)
            Assert.Equal([inner.Initial.Name], Assert.IsType<string[]>(scope.Values["states"]));
        else
            Assert.False(scope.Values.ContainsKey("states"));
        Assert.Same(scope, Assert.Single(machine.ProbeContexts));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "message-filter-typed-raise-context-forwarding")]
    public async Task Send_UsesOnlyTypedRaiseAndForwardsExactContextEventSagaMessageAndTokenAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var saga = new ContractSaga { CorrelationId = Guid.Parse("11111111-1111-1111-1111-111111111111") };
        var message = new ContractMessage(saga.CorrelationId);
        var machine = new DelegatingStateMachine(new ContractStateMachine());
        IBehaviorContext<ContractSaga, ContractMessage>? observedContext = null;
        CancellationToken observedToken = default;
        machine.TypedRaiseHandler = (behaviorContext, token) =>
        {
            observedContext = behaviorContext;
            observedToken = token;
            return Task.CompletedTask;
        };
        CompletionSagaContext context = CreateContext(cancellationToken, saga, message);
        var next = new RecordingPipe();

        await CreateFilter(machine).SendAsync(context, next);

        Assert.NotNull(observedContext);
        Assert.Same(machine, observedContext.StateMachine);
        Assert.Same(machine.Event, observedContext.Event);
        Assert.Same(saga, observedContext.Saga);
        Assert.Same(message, observedContext.Message);
        Assert.Equal(cancellationToken, observedContext.CancellationToken);
        Assert.Equal(cancellationToken, observedToken);
        Assert.Equal(1, machine.TypedRaiseCount);
        Assert.Equal(0, machine.UntypedRaiseCount);
        Assert.Equal(0, next.SendCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "message-filter-terminal-success-ordering")]
    public async Task TerminalSuccess_CompletesOnlyAfterThePredicateAndNeverInvokesNextAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ILogContext? previousLogContext = LogContext.Current;
        await using ServiceProvider provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        using var observations = new MetricObservationSession(provider.GetRequiredService<IMeterFactory>());
        var events = new ConcurrentQueue<string>();
        var completionStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var machine = new DelegatingStateMachine(new ContractStateMachine())
        {
            TypedRaiseHandler = (_, _) =>
            {
                events.Enqueue("raise");
                return Task.CompletedTask;
            },
            IsCompletedHandler = async (_, token) =>
            {
                events.Enqueue("completion-started");
                completionStarted.SetResult();
                await releaseCompletion.Task.WaitAsync(token);
                events.Enqueue("completion-finished");
                return true;
            },
        };
        CompletionSagaContext context = CreateContext(cancellationToken, events: events);
        var next = new RecordingPipe();

        try
        {
            LogContext.Current = null;
            LogContext.ConfigureCurrentLogContextIfNull(provider);

            Task send = CreateFilter(machine).SendAsync(context, next);
            await completionStarted.Task.WaitAsync(cancellationToken);
            Assert.False(send.IsCompleted);
            Assert.False(context.IsCompleted);
            Assert.Equal(["raise", "completion-started"], events);

            MetricMeasurement started = Assert.Single(observations.Measurements, measurement =>
                measurement.Name == ServiceBusTelemetry.Metrics.ActiveOperations
                && measurement.Value == 1);
            Assert.Equal("saga", started.Tag(ServiceBusTelemetry.Attributes.OperationName));
            Assert.Equal("process", started.Tag(ServiceBusTelemetry.Attributes.OperationType));
            Assert.Equal("saga_state_machine", started.Tag(ServiceBusTelemetry.Attributes.ProcessorKind));
            Assert.DoesNotContain(observations.Measurements, measurement =>
                measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration);

            releaseCompletion.SetResult();
            await send;

            Assert.True(context.IsCompleted);
            Assert.Equal(["raise", "completion-started", "completion-finished", "set-completed"], events);
            MetricMeasurement completed = Assert.Single(observations.Measurements, measurement =>
                measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration);
            Assert.DoesNotContain(completed.Tags, tag => tag.Key == ServiceBusTelemetry.Attributes.ErrorType);
            Assert.Equal(
                [1d, -1d],
                observations.Measurements
                    .Where(measurement => measurement.Name == ServiceBusTelemetry.Metrics.ActiveOperations)
                    .Select(measurement => measurement.Value));
            Assert.Equal(0, next.SendCount);
        }
        finally
        {
            releaseCompletion.TrySetResult();
            LogContext.Current = previousLogContext;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "message-filter-set-completed-await")]
    public async Task SetCompleted_TaskIsAwaitedBeforeSendCompletesAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var completionStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var machine = new DelegatingStateMachine(new ContractStateMachine())
        {
            IsCompletedHandler = (_, _) => Task.FromResult(true),
        };
        CompletionSagaContext context = CreateContext(cancellationToken, completionHandler: async token =>
        {
            Assert.Equal(cancellationToken, token);
            completionStarted.SetResult();
            await releaseCompletion.Task.WaitAsync(token);
        });
        var next = new RecordingPipe();

        Task send = CreateFilter(machine).SendAsync(context, next);
        await completionStarted.Task.WaitAsync(cancellationToken);
        Assert.False(send.IsCompleted);
        Assert.False(context.IsCompleted);
        Assert.Equal(1, context.CompletionCount);

        releaseCompletion.SetResult();
        await send;

        Assert.True(context.IsCompleted);
        Assert.Equal(0, next.SendCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "message-filter-successful-activity-state-tags")]
    public async Task SuccessfulActivity_RecordsExactBeforeAndAfterStateTagsAndReleasesAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var stopped = new ConcurrentQueue<Activity>();
        using ActivityListener listener = CreateAllDataListener(stopped);
        ActivitySource.AddActivityListener(listener);
        using var callerSource = new ActivitySource(CallerActivitySource);
        using Activity? caller = callerSource.StartActivity("caller", ActivityKind.Internal);
        Assert.NotNull(caller);

        var inner = new ContractStateMachine();
        var accessor = new SequencedStateAccessor(inner.Accessor)
        {
            GetHandler = (call, _, _) => Task.FromResult<IState<ContractSaga>?>(
                Assert.IsAssignableFrom<IState<ContractSaga>>(call == 1 ? inner.Initial : inner.Running)),
        };
        var machine = new DelegatingStateMachine(inner) { Accessor = accessor };
        CompletionSagaContext context = CreateContext(cancellationToken);
        var next = new RecordingPipe();

        await CreateFilter(machine).SendAsync(context, next);

        Activity process = Assert.Single(stopped, activity => activity.Source.Name == ServiceBusTelemetry.ActivitySourceName);
        Assert.Equal(inner.Initial.Name, process.GetTagItem(ServiceBusTelemetry.Attributes.SagaStateBefore));
        Assert.Equal(inner.Running.Name, process.GetTagItem(ServiceBusTelemetry.Attributes.SagaStateAfter));
        Assert.Equal(context.Saga.CorrelationId.ToString("D"), process.GetTagItem(ServiceBusTelemetry.Attributes.SagaId));
        Assert.Equal(machine.Name, process.GetTagItem(ServiceBusTelemetry.Attributes.ProcessorName));
        Assert.Equal(MessageTypeCache<ContractMessage>.DiagnosticAddress,
            process.GetTagItem(ServiceBusTelemetry.Attributes.MessageContract));
        Assert.Equal("process", process.GetTagItem(ServiceBusTelemetry.Attributes.OperationType));
        Assert.Equal(ActivityStatusCode.Ok, process.Status);
        Assert.Same(caller, Activity.Current);
        Assert.Equal(2, accessor.GetCount);
        Assert.Equal(0, next.SendCount);
    }

    [Theory]
    [InlineData(DiagnosticOutcome.Success)]
    [InlineData(DiagnosticOutcome.Failure)]
    [InlineData(DiagnosticOutcome.Unhandled)]
    [InlineData(DiagnosticOutcome.Cancellation)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "message-filter-pending-diagnostic-state-read-nonblocking-finally")]
    public async Task PendingDiagnosticStateRead_NeverBlocksOutcomeOrFinallyReleaseAsync(DiagnosticOutcome outcome)
    {
        using var deliveryCancellation = new CancellationTokenSource();
        var stopped = new ConcurrentQueue<Activity>();
        using ActivityListener listener = CreateAllDataListener(stopped);
        ActivitySource.AddActivityListener(listener);
        using var callerSource = new ActivitySource(CallerActivitySource);
        using Activity? caller = callerSource.StartActivity("caller", ActivityKind.Internal);
        Assert.NotNull(caller);

        var pendingState = new TaskCompletionSource<IState<ContractSaga>?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var eventFailure = new ExpectedLifecycleException("event");
        var unhandled = new UnhandledEventException("ContractMachine", "Signal", "Waiting");
        var inner = new ContractStateMachine();
        var accessor = new SequencedStateAccessor(inner.Accessor) { GetHandler = (_, _, _) => pendingState.Task };
        var machine = new DelegatingStateMachine(inner)
        {
            Accessor = accessor,
            TypedRaiseHandler = (_, _) => outcome switch
            {
                DiagnosticOutcome.Failure => Task.FromException(eventFailure),
                DiagnosticOutcome.Unhandled => Task.FromException(unhandled),
                DiagnosticOutcome.Cancellation => CancelAndReturnAsync(deliveryCancellation),
                _ => Task.CompletedTask,
            },
        };
        CompletionSagaContext context = CreateContext(deliveryCancellation.Token);
        var next = new RecordingPipe();

        Task send = CreateFilter(machine).SendAsync(context, next);
        Assert.True(send.IsCompleted);
        if (outcome == DiagnosticOutcome.Failure)
            Assert.Same(eventFailure, await Assert.ThrowsAsync<ExpectedLifecycleException>(() => send));
        else if (outcome == DiagnosticOutcome.Unhandled)
            Assert.Same(unhandled, (await Assert.ThrowsAsync<NotAcceptedStateMachineException>(() => send)).InnerException);
        else if (outcome == DiagnosticOutcome.Cancellation)
            Assert.Equal(deliveryCancellation.Token,
                (await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send)).CancellationToken);
        else
            await send;

        Assert.Single(stopped, activity => activity.Source.Name == ServiceBusTelemetry.ActivitySourceName);
        Assert.False(pendingState.Task.IsCompleted);
        Assert.Equal(2, accessor.GetCount);
        Assert.Same(caller, Activity.Current);
        Assert.Equal(0, next.SendCount);
    }

    [Theory]
    [InlineData(UnhandledStateSource.ExceptionState, "WaitingForApproval", 0, true)]
    [InlineData(UnhandledStateSource.BlankExceptionState, "Initial", 1, true)]
    [InlineData(UnhandledStateSource.WhitespaceExceptionState, "Initial", 1, true)]
    [InlineData(UnhandledStateSource.AccessorState, "Initial", 1, true)]
    [InlineData(UnhandledStateSource.BlankAccessorState, "(not initialized)", 1, true)]
    [InlineData(UnhandledStateSource.WhitespaceAccessorState, "(not initialized)", 1, true)]
    [InlineData(UnhandledStateSource.NullState, "(not initialized)", 1, true)]
    [InlineData(UnhandledStateSource.SynchronousAccessorFailure, "(not initialized)", 1, true)]
    [InlineData(UnhandledStateSource.FaultedTask, "(not initialized)", 1, true)]
    [InlineData(UnhandledStateSource.CanceledTask, "(not initialized)", 1, true)]
    [InlineData(UnhandledStateSource.NullTask, "(not initialized)", 1, true)]
    [InlineData(UnhandledStateSource.PendingTask, "(not initialized)", 1, true)]
    [InlineData(UnhandledStateSource.NullCorrelationId, "(not initialized)", 1, false)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "message-filter-unhandled-state-fallback-matrix")]
    public async Task UnhandledEvent_UsesEveryCausalStateFallbackWithoutSecondaryMaskingAsync(
        UnhandledStateSource source, string expectedState, int expectedAccessorCalls, bool hasCorrelationId)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ILogContext? previousLogContext = LogContext.Current;
        await using ServiceProvider provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        using var observations = new MetricObservationSession(provider.GetRequiredService<IMeterFactory>());
        var inner = new ContractStateMachine();
        var pending = new TaskCompletionSource<IState<ContractSaga>?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var accessorFailure = new ExpectedLifecycleException("accessor");
        using var accessorCancellation = new CancellationTokenSource();
        accessorCancellation.Cancel();
        var accessor = new SequencedStateAccessor(inner.Accessor)
        {
            GetHandler = (_, _, _) => source switch
            {
                UnhandledStateSource.AccessorState => Task.FromResult<IState<ContractSaga>?>(
                    Assert.IsAssignableFrom<IState<ContractSaga>>(inner.Initial)),
                UnhandledStateSource.BlankExceptionState => Task.FromResult<IState<ContractSaga>?>(
                    Assert.IsAssignableFrom<IState<ContractSaga>>(inner.Initial)),
                UnhandledStateSource.WhitespaceExceptionState => Task.FromResult<IState<ContractSaga>?>(
                    Assert.IsAssignableFrom<IState<ContractSaga>>(inner.Initial)),
                UnhandledStateSource.BlankAccessorState => Task.FromResult<IState<ContractSaga>?>(
                    SetStateName(inner.Initial, string.Empty)),
                UnhandledStateSource.WhitespaceAccessorState => Task.FromResult<IState<ContractSaga>?>(
                    SetStateName(inner.Initial, " \t ")),
                UnhandledStateSource.NullState => Task.FromResult<IState<ContractSaga>?>(null),
                UnhandledStateSource.SynchronousAccessorFailure => ThrowStateAccessorFailureAsync(accessorFailure),
                UnhandledStateSource.FaultedTask => Task.FromException<IState<ContractSaga>?>(accessorFailure),
                UnhandledStateSource.CanceledTask => Task.FromCanceled<IState<ContractSaga>?>(accessorCancellation.Token),
                UnhandledStateSource.NullTask => null!,
                UnhandledStateSource.PendingTask => pending.Task,
                UnhandledStateSource.NullCorrelationId => Task.FromResult<IState<ContractSaga>?>(null),
                _ => Task.FromException<IState<ContractSaga>?>(accessorFailure),
            },
        };
        UnhandledEventException unhandled = source switch
        {
            UnhandledStateSource.ExceptionState =>
                new UnhandledEventException("ContractMachine", inner.Signal.Name, expectedState),
            UnhandledStateSource.BlankExceptionState => SetUnhandledStateName(string.Empty),
            UnhandledStateSource.WhitespaceExceptionState => SetUnhandledStateName(" \t "),
            _ => new UnhandledEventException(),
        };
        var machine = new DelegatingStateMachine(inner)
        {
            Accessor = accessor,
            TypedRaiseHandler = (_, _) => Task.FromException(unhandled),
        };
        CompletionSagaContext context = CreateContext(cancellationToken, omitCorrelationId: !hasCorrelationId);
        var next = new RecordingPipe();

        try
        {
            LogContext.Current = null;
            LogContext.ConfigureCurrentLogContextIfNull(provider);

            Task send = CreateFilter(machine).SendAsync(context, next);
            Assert.True(send.IsCompleted);
            NotAcceptedStateMachineException actual = await Assert.ThrowsAsync<NotAcceptedStateMachineException>(() => send);

            Assert.Equal(expectedState, actual.CurrentState);
            Assert.Same(unhandled, actual.InnerException);
            Assert.Equal(typeof(ContractSaga), actual.SagaType);
            Assert.Equal(typeof(ContractMessage), actual.MessageType);
            Assert.Equal(hasCorrelationId ? context.Saga.CorrelationId : Guid.Empty, actual.CorrelationId);
            Assert.Equal(expectedAccessorCalls, accessor.GetCount);
            Assert.False(pending.Task.IsCompleted);
            Assert.Equal(0, machine.IsCompletedCount);
            Assert.Equal(0, next.SendCount);

            Assert.Equal(
                [1d, -1d],
                observations.Measurements
                    .Where(measurement => measurement.Name == ServiceBusTelemetry.Metrics.ActiveOperations)
                    .Select(measurement => measurement.Value));
            MetricMeasurement completed = Assert.Single(observations.Measurements, measurement =>
                measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration);
            Assert.Equal("saga", completed.Tag(ServiceBusTelemetry.Attributes.OperationName));
            Assert.Equal("process", completed.Tag(ServiceBusTelemetry.Attributes.OperationType));
            Assert.Equal("saga_state_machine", completed.Tag(ServiceBusTelemetry.Attributes.ProcessorKind));
            Assert.Equal(typeof(UnhandledEventException).FullName,
                completed.Tag(ServiceBusTelemetry.Attributes.ErrorType));
        }
        finally
        {
            LogContext.Current = previousLogContext;
        }
    }

    [Theory]
    [InlineData(LifecycleStage.RaiseEvent, "The state machine returned a null task from RaiseEventAsync.")]
    [InlineData(LifecycleStage.IsCompleted, "The state machine returned a null task from IsCompletedAsync.")]
    [InlineData(LifecycleStage.SetCompleted, "The saga consume context returned a null task from SetCompletedAsync.")]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "message-filter-null-task-boundaries")]
    public async Task CollaboratorNullTasks_FailAtTheirExactBoundaryAsync(LifecycleStage stage, string expectedMessage)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var machine = new DelegatingStateMachine(new ContractStateMachine())
        {
            TypedRaiseHandler = (_, _) => stage == LifecycleStage.RaiseEvent ? null! : Task.CompletedTask,
            IsCompletedHandler = (_, _) => stage == LifecycleStage.IsCompleted ? null! : Task.FromResult(true),
        };
        CompletionSagaContext context = CreateContext(
            cancellationToken, returnNullCompletionTask: stage == LifecycleStage.SetCompleted);
        var next = new RecordingPipe();

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateFilter(machine).SendAsync(context, next));

        Assert.Equal(expectedMessage, actual.Message);
        Assert.Equal(1, machine.TypedRaiseCount);
        Assert.Equal(stage == LifecycleStage.RaiseEvent ? 0 : 1, machine.IsCompletedCount);
        Assert.Equal(stage == LifecycleStage.SetCompleted ? 1 : 0, context.CompletionCount);
        Assert.False(context.IsCompleted);
        Assert.Equal(0, next.SendCount);
    }

    [Theory]
    [InlineData(LifecycleStage.RaiseEvent)]
    [InlineData(LifecycleStage.IsCompleted)]
    [InlineData(LifecycleStage.SetCompleted)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "message-filter-lifecycle-exception-identity-matrix")]
    public async Task LifecycleFailures_PreserveExactExceptionIdentityAndStopLaterStagesAsync(LifecycleStage stage)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ILogContext? previousLogContext = LogContext.Current;
        await using ServiceProvider provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        using var observations = new MetricObservationSession(provider.GetRequiredService<IMeterFactory>());
        var stopped = new ConcurrentQueue<Activity>();
        using ActivityListener listener = CreateAllDataListener(stopped);
        ActivitySource.AddActivityListener(listener);
        using var callerSource = new ActivitySource(CallerActivitySource);
        using Activity? caller = callerSource.StartActivity("caller", ActivityKind.Internal);
        Assert.NotNull(caller);
        var expected = new ExpectedLifecycleException(stage.ToString());
        var machine = new DelegatingStateMachine(new ContractStateMachine())
        {
            TypedRaiseHandler = (_, _) => stage == LifecycleStage.RaiseEvent ? Task.FromException(expected) : Task.CompletedTask,
            IsCompletedHandler = (_, _) => stage == LifecycleStage.IsCompleted
                ? Task.FromException<bool>(expected)
                : Task.FromResult(true),
        };
        CompletionSagaContext context = CreateContext(cancellationToken,
            completionHandler: stage == LifecycleStage.SetCompleted ? _ => Task.FromException(expected) : null);
        var next = new RecordingPipe();

        try
        {
            LogContext.Current = null;
            LogContext.ConfigureCurrentLogContextIfNull(provider);

            ExpectedLifecycleException actual = await Assert.ThrowsAsync<ExpectedLifecycleException>(() =>
                CreateFilter(machine).SendAsync(context, next));

            Assert.Same(expected, actual);
            Assert.Equal(1, machine.TypedRaiseCount);
            Assert.Equal(stage == LifecycleStage.RaiseEvent ? 0 : 1, machine.IsCompletedCount);
            Assert.Equal(stage == LifecycleStage.SetCompleted ? 1 : 0, context.CompletionCount);
            Assert.False(context.IsCompleted);

            MetricMeasurement completed = Assert.Single(observations.Measurements, measurement =>
                measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration);
            Assert.Equal(typeof(ExpectedLifecycleException).FullName,
                completed.Tag(ServiceBusTelemetry.Attributes.ErrorType));
            Assert.Equal(
                [1d, -1d],
                observations.Measurements
                    .Where(measurement => measurement.Name == ServiceBusTelemetry.Metrics.ActiveOperations)
                    .Select(measurement => measurement.Value));

            Activity process = Assert.Single(stopped, activity =>
                activity.Source.Name == ServiceBusTelemetry.ActivitySourceName);
            Assert.Equal(ActivityStatusCode.Error, process.Status);
            Assert.Equal(expected.Message, process.StatusDescription);
            ActivityEvent exceptionEvent = Assert.Single(process.Events, activityEvent =>
                activityEvent.Name == ServiceBusTelemetry.Events.Exception);
            Assert.Equal(expected.Message, Assert.Single(exceptionEvent.Tags, tag =>
                tag.Key == ServiceBusTelemetry.Attributes.ExceptionMessage).Value);
            Assert.Equal(TypeCache<ExpectedLifecycleException>.ShortName, Assert.Single(exceptionEvent.Tags, tag =>
                tag.Key == ServiceBusTelemetry.Attributes.ExceptionType).Value);
            Assert.Same(caller, Activity.Current);
            Assert.Equal(0, next.SendCount);
        }
        finally
        {
            LogContext.Current = previousLogContext;
        }
    }

    [Theory]
    [InlineData(LifecycleStage.RaiseEvent)]
    [InlineData(LifecycleStage.IsCompleted)]
    [InlineData(LifecycleStage.SetCompleted)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "message-filter-lifecycle-cancellation-token-matrix")]
    public async Task LifecycleCancellation_PreservesReturnedTokenAndForwardsDeliveryTokenAsync(LifecycleStage stage)
    {
        CancellationToken deliveryToken = TestContext.Current.CancellationToken;
        ILogContext? previousLogContext = LogContext.Current;
        await using ServiceProvider provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        using var observations = new MetricObservationSession(provider.GetRequiredService<IMeterFactory>());
        var stopped = new ConcurrentQueue<Activity>();
        using ActivityListener listener = CreateAllDataListener(stopped);
        ActivitySource.AddActivityListener(listener);
        using var callerSource = new ActivitySource(CallerActivitySource);
        using Activity? caller = callerSource.StartActivity("caller", ActivityKind.Internal);
        Assert.NotNull(caller);
        using var dependencyCancellation = new CancellationTokenSource();
        dependencyCancellation.Cancel();
        var machine = new DelegatingStateMachine(new ContractStateMachine())
        {
            TypedRaiseHandler = (_, _) => stage == LifecycleStage.RaiseEvent
                ? Task.FromCanceled(dependencyCancellation.Token)
                : Task.CompletedTask,
            IsCompletedHandler = (_, _) => stage == LifecycleStage.IsCompleted
                ? Task.FromCanceled<bool>(dependencyCancellation.Token)
                : Task.FromResult(true),
        };
        CompletionSagaContext context = CreateContext(deliveryToken,
            completionHandler: stage == LifecycleStage.SetCompleted
                ? _ => Task.FromCanceled(dependencyCancellation.Token)
                : null);
        var next = new RecordingPipe();

        try
        {
            LogContext.Current = null;
            LogContext.ConfigureCurrentLogContextIfNull(provider);

            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                CreateFilter(machine).SendAsync(context, next));

            Assert.Equal(dependencyCancellation.Token, actual.CancellationToken);
            Assert.All(machine.TypedRaiseTokens, token => Assert.Equal(deliveryToken, token));
            Assert.All(machine.IsCompletedTokens, token => Assert.Equal(deliveryToken, token));
            Assert.All(context.CompletionTokens, token => Assert.Equal(deliveryToken, token));
            Assert.Equal(1, machine.TypedRaiseCount);
            Assert.Equal(stage == LifecycleStage.RaiseEvent ? 0 : 1, machine.IsCompletedCount);
            Assert.Equal(stage == LifecycleStage.SetCompleted ? 1 : 0, context.CompletionCount);
            Assert.False(context.IsCompleted);

            Assert.Equal(
                [1d, -1d],
                observations.Measurements
                    .Where(measurement => measurement.Name == ServiceBusTelemetry.Metrics.ActiveOperations)
                    .Select(measurement => measurement.Value));
            MetricMeasurement completed = Assert.Single(observations.Measurements, measurement =>
                measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration);
            Assert.Equal("saga", completed.Tag(ServiceBusTelemetry.Attributes.OperationName));
            Assert.Equal("process", completed.Tag(ServiceBusTelemetry.Attributes.OperationType));
            Assert.Equal("saga_state_machine", completed.Tag(ServiceBusTelemetry.Attributes.ProcessorKind));
            Assert.Equal(actual.GetType().FullName, completed.Tag(ServiceBusTelemetry.Attributes.ErrorType));

            Activity process = Assert.Single(stopped, activity =>
                activity.Source.Name == ServiceBusTelemetry.ActivitySourceName);
            Assert.Equal(ActivityStatusCode.Error, process.Status);
            ActivityEvent exceptionEvent = Assert.Single(process.Events, activityEvent =>
                activityEvent.Name == ServiceBusTelemetry.Events.Exception);
            Assert.Equal(TypeCache.GetShortName(actual.GetType()), Assert.Single(exceptionEvent.Tags, tag =>
                tag.Key == ServiceBusTelemetry.Attributes.ExceptionType).Value);
            Assert.Same(caller, Activity.Current);
            Assert.Equal(0, next.SendCount);
        }
        finally
        {
            LogContext.Current = previousLogContext;
        }
    }

    [Theory]
    [InlineData(DeliveryCancellationPoint.BeforeEvent, 0, 0)]
    [InlineData(DeliveryCancellationPoint.AfterEvent, 1, 0)]
    [InlineData(DeliveryCancellationPoint.AfterFalseCompletion, 1, 1)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "message-filter-delivery-cancellation-checkpoints")]
    public async Task DeliveryCancellation_PreventsEveryLaterLifecycleStageAsync(
        DeliveryCancellationPoint point, int expectedRaiseCount, int expectedCompletionCount)
    {
        ILogContext? previousLogContext = LogContext.Current;
        await using ServiceProvider provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        using var observations = new MetricObservationSession(provider.GetRequiredService<IMeterFactory>());
        var stopped = new ConcurrentQueue<Activity>();
        using ActivityListener listener = CreateAllDataListener(stopped);
        ActivitySource.AddActivityListener(listener);
        using var callerSource = new ActivitySource(CallerActivitySource);
        using Activity? caller = callerSource.StartActivity("caller", ActivityKind.Internal);
        Assert.NotNull(caller);
        using var cancellation = new CancellationTokenSource();
        var machine = new DelegatingStateMachine(new ContractStateMachine());
        machine.TypedRaiseHandler = (_, _) =>
        {
            if (point == DeliveryCancellationPoint.AfterEvent)
                cancellation.Cancel();
            return Task.CompletedTask;
        };
        machine.IsCompletedHandler = (_, _) =>
        {
            if (point == DeliveryCancellationPoint.AfterFalseCompletion)
                cancellation.Cancel();
            return Task.FromResult(false);
        };
        if (point == DeliveryCancellationPoint.BeforeEvent)
            cancellation.Cancel();
        CompletionSagaContext context = CreateContext(cancellation.Token);
        var next = new RecordingPipe();

        try
        {
            LogContext.Current = null;
            LogContext.ConfigureCurrentLogContextIfNull(provider);

            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                CreateFilter(machine).SendAsync(context, next));

            Assert.Equal(cancellation.Token, actual.CancellationToken);
            Assert.Equal(expectedRaiseCount, machine.TypedRaiseCount);
            Assert.Equal(expectedCompletionCount, machine.IsCompletedCount);
            Assert.Equal(0, context.CompletionCount);
            Assert.False(context.IsCompleted);
            if (point == DeliveryCancellationPoint.BeforeEvent)
            {
                Assert.DoesNotContain(stopped, activity =>
                    activity.Source.Name == ServiceBusTelemetry.ActivitySourceName);
                Assert.Empty(observations.Measurements);
            }
            else
            {
                Activity process = Assert.Single(stopped, activity =>
                    activity.Source.Name == ServiceBusTelemetry.ActivitySourceName);
                Assert.Equal(ActivityStatusCode.Ok, process.Status);
                Assert.DoesNotContain(process.Events, activityEvent =>
                    activityEvent.Name == ServiceBusTelemetry.Events.Exception);

                Assert.Equal(
                    [1d, -1d],
                    observations.Measurements
                        .Where(measurement => measurement.Name == ServiceBusTelemetry.Metrics.ActiveOperations)
                        .Select(measurement => measurement.Value));
                MetricMeasurement completed = Assert.Single(observations.Measurements, measurement =>
                    measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration);
                Assert.Equal("saga", completed.Tag(ServiceBusTelemetry.Attributes.OperationName));
                Assert.Equal("process", completed.Tag(ServiceBusTelemetry.Attributes.OperationType));
                Assert.Equal("saga_state_machine", completed.Tag(ServiceBusTelemetry.Attributes.ProcessorKind));
                Assert.DoesNotContain(completed.Tags, tag =>
                    tag.Key == ServiceBusTelemetry.Attributes.ErrorType);
            }
            Assert.Same(caller, Activity.Current);
            Assert.Equal(0, next.SendCount);
        }
        finally
        {
            LogContext.Current = previousLogContext;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "message-filter-shared-concurrency-isolation")]
    public async Task SharedFilter_IsolatesConcurrentSagaContextsAndCompletesEachExactlyOnceAsync()
    {
        const int Count = 32;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observed = new ConcurrentDictionary<Guid, byte>();
        var allEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var machine = new DelegatingStateMachine(new ContractStateMachine())
        {
            TypedRaiseHandler = async (behaviorContext, token) =>
            {
                Assert.True(observed.TryAdd(behaviorContext.Saga.CorrelationId, 0));
                if (observed.Count == Count)
                    allEntered.TrySetResult();
                await release.Task.WaitAsync(token);
            },
            IsCompletedHandler = (_, _) => Task.FromResult(true),
        };
        ISagaMessageFilter<ContractSaga, ContractMessage> filter = CreateFilter(machine);
        var next = new RecordingPipe();
        CompletionSagaContext[] contexts = Enumerable.Range(1, Count)
            .Select(index => CreateContext(cancellationToken, new ContractSaga
            {
                CorrelationId = new Guid(index, 0, 0, new byte[8]),
            }))
            .ToArray();

        Task[] sends = contexts.Select(context => filter.SendAsync(context, next)).ToArray();
        await allEntered.Task.WaitAsync(cancellationToken);
        Assert.All(sends, send => Assert.False(send.IsCompleted));
        release.SetResult();
        await Task.WhenAll(sends);

        Assert.Equal(Count, observed.Count);
        Assert.Equal(Count, machine.TypedRaiseCount);
        Assert.Equal(0, machine.UntypedRaiseCount);
        Assert.Equal(Count, machine.IsCompletedCount);
        Assert.All(contexts, context => Assert.True(context.IsCompleted));
        Assert.All(contexts, context => Assert.Equal(1, context.CompletionCount));
        Assert.Equal(0, next.SendCount);
    }

    static ActivityListener CreateAllDataListener(ConcurrentQueue<Activity> stopped) => new()
    {
        ShouldListenTo = source =>
            source.Name == ServiceBusTelemetry.ActivitySourceName || source.Name == CallerActivitySource,
        Sample = (ref ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
        ActivityStopped = stopped.Enqueue,
    };

    static Task CancelAndReturnAsync(CancellationTokenSource cancellation)
    {
        cancellation.Cancel();
        return Task.FromCanceled(cancellation.Token);
    }

    static Task<IState<ContractSaga>?> ThrowStateAccessorFailureAsync(Exception exception) => throw exception;

    static IState<ContractSaga> SetStateName(IState state, string name)
    {
        IState<ContractSaga> typedState = Assert.IsAssignableFrom<IState<ContractSaga>>(state);
        FieldInfo nameField = state.GetType().GetField("<Name>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Expected the test state to expose its generated name backing field.");
        nameField.SetValue(state, name);
        return typedState;
    }

    static UnhandledEventException SetUnhandledStateName(string stateName)
    {
        var exception = new UnhandledEventException();
        FieldInfo stateNameField = typeof(UnhandledEventException).GetField(
            "<StateName>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Expected the unhandled-event exception to expose its generated state-name backing field.");
        stateNameField.SetValue(exception, stateName);
        return exception;
    }

    static ISagaMessageFilter<ContractSaga, ContractMessage> CreateFilter(DelegatingStateMachine machine) =>
        SagaStateMachineExecutionTestDriver.CreateMessageFilter(machine, machine.Event);

    static CompletionSagaContext CreateContext(
        CancellationToken cancellationToken,
        ContractSaga? saga = null,
        ContractMessage? message = null,
        ConcurrentQueue<string>? events = null,
        bool returnNullCompletionTask = false,
        Func<CancellationToken, Task>? completionHandler = null,
        bool omitCorrelationId = false)
    {
        saga ??= new ContractSaga { CorrelationId = Guid.NewGuid() };
        message ??= new ContractMessage(saga.CorrelationId);
        ConsumeContext<ContractMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(
            message, cancellationToken, correlationId: omitCorrelationId ? null : saga.CorrelationId);
        return new CompletionSagaContext(
            consumeContext,
            saga,
            events,
            returnNullCompletionTask,
            completionHandler,
            omitCorrelationId);
    }

    public enum DiagnosticOutcome { Success, Failure, Unhandled, Cancellation }
    public enum UnhandledStateSource
    {
        ExceptionState,
        BlankExceptionState,
        WhitespaceExceptionState,
        AccessorState,
        BlankAccessorState,
        WhitespaceAccessorState,
        NullState,
        SynchronousAccessorFailure,
        FaultedTask,
        CanceledTask,
        NullTask,
        PendingTask,
        NullCorrelationId,
    }
    public enum LifecycleStage { RaiseEvent, IsCompleted, SetCompleted }
    public enum DeliveryCancellationPoint { BeforeEvent, AfterEvent, AfterFalseCompletion }

    public sealed record ContractMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed class ContractSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public IState? CurrentState { get; set; }
    }

    sealed class ContractStateMachine : ViciOneServiceBusStateMachine<ContractSaga>
    {
        public ContractStateMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            Initially(When(Signal).Then(_ => { }));
        }

        public IState Running { get; private set; } = null!;
        public IEvent<ContractMessage> Signal { get; private set; } = null!;
        public IEvent<ContractMessage> Unused { get; private set; } = null!;
    }

    sealed class DelegatingStateMachine : ISagaStateMachine<ContractSaga>
    {
        readonly ContractStateMachine _inner;
        int _isCompletedCount;
        int _typedRaiseCount;
        int _untypedRaiseCount;

        public DelegatingStateMachine(ContractStateMachine inner, IEvent<ContractMessage>? @event = null)
        {
            _inner = inner;
            Accessor = inner.Accessor;
            Event = @event ?? inner.Signal;
        }

        public Func<IBehaviorContext<ContractSaga, ContractMessage>, CancellationToken, Task> TypedRaiseHandler { get; set; } =
            (_, _) => Task.CompletedTask;
        public Func<IBehaviorContext<ContractSaga>, CancellationToken, Task> UntypedRaiseHandler { get; set; } =
            (_, _) => Task.CompletedTask;
        public Func<IBehaviorContext<ContractSaga>, CancellationToken, Task<bool>> IsCompletedHandler { get; set; } =
            (_, _) => Task.FromResult(false);
        public IStateAccessor<ContractSaga> Accessor { get; set; }
        public IEvent<ContractMessage> Event { get; }
        public int IsCompletedCount => Volatile.Read(ref _isCompletedCount);
        public int TypedRaiseCount => Volatile.Read(ref _typedRaiseCount);
        public int UntypedRaiseCount => Volatile.Read(ref _untypedRaiseCount);
        public int RaiseCount => TypedRaiseCount + UntypedRaiseCount;
        public ConcurrentQueue<CancellationToken> TypedRaiseTokens { get; } = [];
        public ConcurrentQueue<CancellationToken> IsCompletedTokens { get; } = [];
        public List<ProbeContext> ProbeContexts { get; } = [];
        public IEnumerable<IEventCorrelation> Correlations => _inner.Correlations;
        public string Name => ((IStateMachine)_inner).Name;
        public IEnumerable<IEvent> Events => _inner.Events;
        public IEnumerable<IState> States => _inner.States;
        public Type InstanceType => typeof(ContractSaga);
        public IState Initial => _inner.Initial;
        public IState Final => _inner.Final;

        public Task<bool> IsCompletedAsync(IBehaviorContext<ContractSaga> context, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _isCompletedCount);
            IsCompletedTokens.Enqueue(cancellationToken);
            return IsCompletedHandler(context, cancellationToken);
        }

        public IEvent GetEvent(string name) => ((IStateMachine)_inner).GetEvent(name);
        IState IStateMachine.GetState(string name) => ((IStateMachine)_inner).GetState(name);
        public IState<ContractSaga> GetState(string name) => _inner.GetState(name);
        public IEnumerable<IEvent> NextEvents(IState state) => _inner.NextEvents(state);
        public bool IsCompositeEvent(IEvent @event) => _inner.IsCompositeEvent(@event);

        public Task RaiseEventAsync(IBehaviorContext<ContractSaga> context, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _untypedRaiseCount);
            return UntypedRaiseHandler(context, cancellationToken);
        }

        public Task RaiseEventAsync<TMessage>(IBehaviorContext<ContractSaga, TMessage> context,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            Interlocked.Increment(ref _typedRaiseCount);
            TypedRaiseTokens.Enqueue(cancellationToken);
            if (context is not IBehaviorContext<ContractSaga, ContractMessage> typedContext)
                return Task.FromException(new InvalidOperationException("The filter raised an unexpected message type."));
            return TypedRaiseHandler(typedContext, cancellationToken);
        }

        public IDisposable ConnectEventObserver(IEventObserver<ContractSaga> observer) => _inner.ConnectEventObserver(observer);
        public IDisposable ConnectEventObserver(IEvent @event, IEventObserver<ContractSaga> observer) =>
            _inner.ConnectEventObserver(@event, observer);
        public IDisposable ConnectStateObserver(IStateObserver<ContractSaga> observer) => _inner.ConnectStateObserver(observer);
        public void Accept(IStateMachineVisitor visitor) => _inner.Accept(visitor);
        public void Probe(ProbeContext context) => ProbeContexts.Add(context);
    }

    sealed class SequencedStateAccessor(IStateAccessor<ContractSaga> inner) : IStateAccessor<ContractSaga>
    {
        int _getCount;
        public Func<int, IBehaviorContext<ContractSaga>, CancellationToken, Task<IState<ContractSaga>?>> GetHandler { get; init; } =
            (_, context, cancellationToken) => inner.GetAsync(context, cancellationToken);
        public int GetCount => Volatile.Read(ref _getCount);
        public Task<IState<ContractSaga>?> GetAsync(IBehaviorContext<ContractSaga> context,
            CancellationToken cancellationToken = default) =>
            GetHandler(Interlocked.Increment(ref _getCount), context, cancellationToken);
        public Task SetAsync(IBehaviorContext<ContractSaga> context, IState<ContractSaga> state,
            CancellationToken cancellationToken = default) => inner.SetAsync(context, state, cancellationToken);
        public Expression<Func<ContractSaga, bool>> GetStateExpression(params IState[] states) => inner.GetStateExpression(states);
        public void Probe(ProbeContext context) => inner.Probe(context);
    }

    sealed class CompletionSagaContext(
        ConsumeContext<ContractMessage> context,
        ContractSaga saga,
        ConcurrentQueue<string>? events,
        bool returnNullCompletionTask,
        Func<CancellationToken, Task>? completionHandler,
        bool omitCorrelationId) :
        DefaultSagaConsumeContext<ContractSaga, ContractMessage>(context, saga),
        SagaConsumeContext<ContractSaga, ContractMessage>
    {
        int _completionCount;
        public override IEnumerable<string> SupportedMessageTypes => [MessageUrn.ForTypeString<ContractMessage>()];
        public override Guid? CorrelationId => omitCorrelationId ? null : base.CorrelationId;
        public int CompletionCount => Volatile.Read(ref _completionCount);
        public ConcurrentQueue<CancellationToken> CompletionTokens { get; } = [];

        Task SagaConsumeContext<ContractSaga>.SetCompletedAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _completionCount);
            CompletionTokens.Enqueue(cancellationToken);
            events?.Enqueue("set-completed");
            if (returnNullCompletionTask)
                return null!;
            if (completionHandler is null)
                return base.SetCompletedAsync(cancellationToken);
            Task task = completionHandler(cancellationToken);
            return task is null ? null! : CompleteAfterAsync(task, cancellationToken);
        }

        async Task CompleteAfterAsync(Task task, CancellationToken cancellationToken)
        {
            await task.ConfigureAwait(false);
            await base.SetCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    sealed class RecordingPipe : IPipe<SagaConsumeContext<ContractSaga, ContractMessage>>
    {
        int _sendCount;
        public int SendCount => Volatile.Read(ref _sendCount);
        public Task SendAsync(SagaConsumeContext<ContractSaga, ContractMessage> context)
        {
            Interlocked.Increment(ref _sendCount);
            return Task.FromException(new InvalidOperationException("The terminal state-machine filter invoked its continuation."));
        }
        public void Probe(ProbeContext context) { }
    }

    sealed class RecordingProbeContext(string? key = null) : ProbeContext
    {
        public CancellationToken CancellationToken => default;
        public string? Key { get; } = key;
        public Dictionary<string, object?> Values { get; } = [];
        public List<RecordingProbeContext> Children { get; } = [];
        public void Add(string key, string? value) => Values[key] = value;
        public void Add(string key, object? value) => Values[key] = value;
        public void Set(object values)
        {
            foreach (PropertyInfo property in values.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                Values[property.Name] = property.GetValue(values);
        }
        public void Set(IEnumerable<KeyValuePair<string, object?>> values)
        {
            foreach (KeyValuePair<string, object?> pair in values)
                Values[pair.Key] = pair.Value;
        }
        public ProbeContext CreateScope(string key)
        {
            var child = new RecordingProbeContext(key);
            Children.Add(child);
            return child;
        }
    }

    sealed class ExpectedLifecycleException(string stage) : Exception(stage);
}
