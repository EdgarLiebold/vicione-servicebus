using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.RetryPolicies;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineCompositeConversionRetryActivitiesDeepContractTests
{
    const BindingFlags DeclaredPublic = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-220-composite-converter-retry-public-contract")]
    public void PublicSurface_PreservesGenericContractsNullabilityAndAsyncNaming()
    {
        AssertType(typeof(CompositeEventActivity<>), ["TSaga"], 6);
        AssertType(typeof(DataConverterActivity<,>), ["TSaga", "TMessage"], 6);
        AssertType(typeof(RetryActivity<>), ["TInstance"], 6);
        AssertType(typeof(RetryActivity<,>), ["TInstance", "TMessage"], 6);

        foreach (Type type in ActivityTypes())
        {
            Type[] interfaces = type.GetInterfaces();
            Assert.Equal(4, interfaces.Length);
            Assert.Contains(typeof(IStateMachineActivity<TestSaga>), interfaces);
            Assert.Contains(typeof(IStateMachineActivity), interfaces);
            Assert.Contains(typeof(IVisitable), interfaces);
            Assert.Contains(typeof(IProbeSite), interfaces);
        }

        var nullability = new NullabilityInfoContext();
        foreach (Type type in ActivityTypes())
        {
            ConstructorInfo constructor = Assert.Single(type.GetConstructors());
            Assert.All(constructor.GetParameters(), parameter =>
                Assert.Equal(NullabilityState.NotNull, nullability.Create(parameter).ReadState));

            foreach (MethodInfo method in type.GetMethods(DeclaredPublic).Where(method => !method.IsSpecialName))
            {
                if (method.ReturnType == typeof(Task))
                    Assert.EndsWith("Async", method.Name, StringComparison.Ordinal);

                Assert.All(method.GetParameters(), parameter =>
                    Assert.Equal(NullabilityState.NotNull, nullability.Create(parameter).ReadState));
            }
        }

        PropertyInfo eventProperty = Assert.Single(typeof(CompositeEventActivity<TestSaga>).GetProperties(DeclaredPublic));
        Assert.Equal("Event", eventProperty.Name);
        Assert.Equal(typeof(IEvent), eventProperty.PropertyType);
        Assert.False(eventProperty.CanWrite);
        Assert.Equal(NullabilityState.NotNull, nullability.Create(eventProperty).ReadState);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-220-composite-converter-retry-required-input-boundaries")]
    public async Task RequiredInputs_RejectNullBeforeAnyCollaboratorIsObservedAsync()
    {
        var accessor = new RecordingAccessor();
        var @event = new TriggerEvent("Composite");
        var typedActivity = new RecordingTypedActivity();
        var retryBehavior = new RecordingBehavior();

        AssertArgument("accessor", () => new CompositeEventActivity<TestSaga>(null!, 1, new CompositeEventStatus(1), @event,
            CompositeEventOptions.None));
        AssertArgument("event", () => new CompositeEventActivity<TestSaga>(accessor, 1, new CompositeEventStatus(1), null!,
            CompositeEventOptions.None));
        AssertArgument("activity", () => new DataConverterActivity<TestSaga, Message>(null!));
        AssertArgument("retryPolicy", () => new RetryActivity<TestSaga>(null!, retryBehavior));
        AssertArgument("retryBehavior", () => new RetryActivity<TestSaga>(Retry.None, null!));
        AssertArgument("retryPolicy", () => new RetryActivity<TestSaga, Message>(null!, retryBehavior));
        AssertArgument("retryBehavior", () => new RetryActivity<TestSaga, Message>(Retry.None, null!));

        var composite = new CompositeEventActivity<TestSaga>(accessor, 1, new CompositeEventStatus(1), @event,
            CompositeEventOptions.None);
        var converter = new DataConverterActivity<TestSaga, Message>(typedActivity);
        var retry = new RetryActivity<TestSaga>(Retry.None, retryBehavior);
        var typedRetry = new RetryActivity<TestSaga, Message>(Retry.None, retryBehavior);
        IBehaviorContext<TestSaga> context = ContextProxy.Create<IBehaviorContext<TestSaga>>(new TestSaga());
        IBehaviorContext<TestSaga, Message> dataContext = ContextProxy.Create<IBehaviorContext<TestSaga, Message>>(
            new TestSaga(), new Message(1));
        IBehaviorExceptionContext<TestSaga, MarkerException> fault =
            ContextProxy.Create<IBehaviorExceptionContext<TestSaga, MarkerException>>(new TestSaga(), failure: new MarkerException("fault"));
        IBehaviorExceptionContext<TestSaga, Message, MarkerException> dataFault =
            ContextProxy.Create<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>(
                new TestSaga(), new Message(2), failure: new MarkerException("fault"));
        var next = new RecordingBehavior();
        var dataNext = new RecordingTypedBehavior<Message>();

        foreach (IStateMachineActivity<TestSaga> activity in new IStateMachineActivity<TestSaga>[] { composite, converter, retry, typedRetry })
        {
            AssertArgument("visitor", () => activity.Accept(null!));
            AssertArgument("context", () => activity.Probe(null!));
        }

        await AssertArgumentAsync("context", () => composite.ExecuteAsync(null!, next));
        await AssertArgumentAsync("next", () => composite.ExecuteAsync(context, null!));
        await AssertArgumentAsync("context", () => composite.ExecuteAsync<Message>(null!, dataNext));
        await AssertArgumentAsync("next", () => composite.ExecuteAsync(dataContext, null!));
        AssertArgument("context", () => composite.FaultedAsync<MarkerException>(null!, next));
        AssertArgument("next", () => composite.FaultedAsync(fault, null!));
        AssertArgument("context", () => composite.FaultedAsync<Message, MarkerException>(null!, dataNext));
        AssertArgument("next", () => composite.FaultedAsync(dataFault, null!));

        AssertAllLifecycleNulls(converter, context, dataContext, fault, dataFault, next, dataNext);
        await AssertAllAsyncLifecycleNullsAsync(retry, context, dataContext, fault, dataFault, next, dataNext);
        await AssertAllAsyncLifecycleNullsAsync(typedRetry, context, dataContext, fault, dataFault, next, dataNext);

        Assert.Empty(accessor.Calls);
        Assert.Equal(0, typedActivity.TotalCalls);
        Assert.Equal(0, retryBehavior.TotalCalls);
        Assert.Equal(0, next.TotalCalls);
        Assert.Equal(0, dataNext.TotalCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-COMPOSITE", "iteration-220-composite-status-raise-once-and-ordering")]
    public async Task CompositeExecution_UsesExactCompletionRaiseOnceAndStatusBeforeRaiseAndContinuationAsync()
    {
        var trace = new List<string>();
        var accessor = new RecordingAccessor(trace);
        var @event = new TriggerEvent("Composite");
        var next = new RecordingBehavior
        {
            ExecuteUntyped = _ =>
            {
                trace.Add("next");
                return Task.CompletedTask;
            }
        };
        var saga = new TestSaga { Status = new CompositeEventStatus(1) };
        IBehaviorContext<TestSaga> context = ContextProxy.Create<IBehaviorContext<TestSaga>>(
            saga,
            raise: raised =>
            {
                trace.Add("raise");
                Assert.Same(@event, raised);
                return Task.CompletedTask;
            });
        var activity = new CompositeEventActivity<TestSaga>(accessor, 2, new CompositeEventStatus(3), @event,
            CompositeEventOptions.None);

        await activity.ExecuteAsync(context, next);

        Assert.Equal(["get", "set:00000003", "raise", "next"], trace);
        Assert.Equal(3, saga.Status.Bits);
        Assert.Equal(1, next.ExecuteUntypedCalls);

        trace.Clear();
        saga.Status = new CompositeEventStatus(2);
        var repeatAllowed = new CompositeEventActivity<TestSaga>(accessor, 2, new CompositeEventStatus(2), @event,
            CompositeEventOptions.None);
        await repeatAllowed.ExecuteAsync(context, next);
        Assert.Equal(["get", "set:00000002", "raise", "next"], trace);
        Assert.Equal(2, saga.Status.Bits);

        trace.Clear();
        var raiseOnce = new CompositeEventActivity<TestSaga>(accessor, 2, new CompositeEventStatus(2), @event,
            CompositeEventOptions.RaiseOnce);
        await raiseOnce.ExecuteAsync(context, next);
        Assert.Equal(["get", "next"], trace);
        Assert.Equal(2, saga.Status.Bits);

        trace.Clear();
        saga.Status = new CompositeEventStatus(4);
        await activity.ExecuteAsync(context, next);
        Assert.Equal(["get", "set:00000006", "next"], trace);
        Assert.Equal(6, saga.Status.Bits);

        var typedTrace = new List<string>();
        var typedAccessor = new RecordingAccessor(typedTrace);
        var typedNext = new RecordingTypedBehavior<Message>
        {
            ExecuteHandler = _ =>
            {
                typedTrace.Add("next");
                return Task.CompletedTask;
            }
        };
        var typedSaga = new TestSaga { Status = new CompositeEventStatus(1) };
        IBehaviorContext<TestSaga, Message> typedContext = ContextProxy.Create<IBehaviorContext<TestSaga, Message>>(
            typedSaga,
            new Message(7),
            raise: raised =>
            {
                typedTrace.Add("raise");
                Assert.Same(@event, raised);
                return Task.CompletedTask;
            });
        var typed = new CompositeEventActivity<TestSaga>(typedAccessor, 2, new CompositeEventStatus(3), @event,
            CompositeEventOptions.None);

        await typed.ExecuteAsync(typedContext, typedNext);

        Assert.Equal(["get", "set:00000003", "raise", "next"], typedTrace);
        Assert.Same(typedContext, typedNext.LastExecuteContext);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-COMPOSITE", "iteration-220-composite-failure-cancellation-and-concurrency")]
    public async Task CompositeExecution_PreservesRaiseOutcomeAndConcurrentContextPairingWithoutContinuationAsync()
    {
        var @event = new TriggerEvent("Composite");
        var failure = new MarkerException("raise");
        var failedSaga = new TestSaga { Status = new CompositeEventStatus(1) };
        var failedNext = new RecordingBehavior();
        IBehaviorContext<TestSaga> failedContext = ContextProxy.Create<IBehaviorContext<TestSaga>>(
            failedSaga, raise: _ => Task.FromException(failure));
        var activity = new CompositeEventActivity<TestSaga>(new RecordingAccessor(), 2, new CompositeEventStatus(3), @event,
            CompositeEventOptions.None);

        MarkerException caught = await Assert.ThrowsAsync<MarkerException>(() => activity.ExecuteAsync(failedContext, failedNext));
        Assert.Same(failure, caught);
        Assert.Equal(3, failedSaga.Status.Bits);
        Assert.Equal(0, failedNext.TotalCalls);

        using var source = new CancellationTokenSource();
        source.Cancel();
        var canceledSaga = new TestSaga { Status = new CompositeEventStatus(1) };
        var canceledNext = new RecordingBehavior();
        IBehaviorContext<TestSaga> canceledContext = ContextProxy.Create<IBehaviorContext<TestSaga>>(
            canceledSaga, raise: _ => Task.FromCanceled(source.Token));
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => activity.ExecuteAsync(canceledContext, canceledNext));
        Assert.Equal(source.Token, canceled.CancellationToken);
        Assert.Equal(3, canceledSaga.Status.Bits);
        Assert.Equal(0, canceledNext.TotalCalls);

        var fault = new MarkerException("fault");
        IBehaviorExceptionContext<TestSaga, MarkerException> faultContext =
            ContextProxy.Create<IBehaviorExceptionContext<TestSaga, MarkerException>>(new TestSaga(), failure: fault);
        var faultNext = new RecordingBehavior { FaultUntypedResult = NewIncompleteTaskAsync() };
        Task faultTask = activity.FaultedAsync(faultContext, faultNext);
        Assert.Same(faultNext.FaultUntypedResult, faultTask);
        Assert.Same(faultContext, faultNext.LastFaultContext);
        Assert.Equal(1, faultNext.FaultUntypedCalls);

        IBehaviorExceptionContext<TestSaga, Message, MarkerException> typedFaultContext =
            ContextProxy.Create<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>(
                new TestSaga(), new Message(1), failure: fault);
        var typedFaultNext = new RecordingTypedBehavior<Message> { FaultResult = NewIncompleteTaskAsync() };
        Task typedFaultTask = activity.FaultedAsync(typedFaultContext, typedFaultNext);
        Assert.Same(typedFaultNext.FaultResult, typedFaultTask);
        Assert.Same(typedFaultContext, typedFaultNext.LastFaultContext);
        Assert.Equal(1, typedFaultNext.FaultCalls);

        var continuationFailure = new MarkerException("continuation");
        var continuationSaga = new TestSaga();
        IBehaviorContext<TestSaga> continuationContext =
            ContextProxy.Create<IBehaviorContext<TestSaga>>(continuationSaga);
        var failingContinuation = new RecordingBehavior
        {
            ExecuteUntyped = _ => Task.FromException(continuationFailure)
        };
        var continuationActivity = new CompositeEventActivity<TestSaga>(
            new RecordingAccessor(), 1, new CompositeEventStatus(2), @event, CompositeEventOptions.None);
        MarkerException continuationCaught = await Assert.ThrowsAsync<MarkerException>(
            () => continuationActivity.ExecuteAsync(continuationContext, failingContinuation));
        Assert.Same(continuationFailure, continuationCaught);
        Assert.Equal(1, failingContinuation.ExecuteUntypedCalls);

        using var continuationCancellation = new CancellationTokenSource();
        continuationCancellation.Cancel();
        IBehaviorContext<TestSaga, Message> typedContinuationContext =
            ContextProxy.Create<IBehaviorContext<TestSaga, Message>>(new TestSaga(), new Message(2));
        var canceledContinuation = new RecordingTypedBehavior<Message>
        {
            ExecuteHandler = _ => Task.FromCanceled(continuationCancellation.Token)
        };
        OperationCanceledException continuationCanceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => continuationActivity.ExecuteAsync(typedContinuationContext, canceledContinuation));
        Assert.Equal(continuationCancellation.Token, continuationCanceled.CancellationToken);
        Assert.Equal(1, canceledContinuation.ExecuteCalls);

        const int count = 48;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var raised = new ConcurrentQueue<(int Id, IEvent Event)>();
        var nextPairs = new ConcurrentQueue<int>();
        var tasks = new Task[count];
        var sagas = new TestSaga[count];
        var sharedAccessor = new RecordingAccessor();
        var shared = new CompositeEventActivity<TestSaga>(sharedAccessor, 2, new CompositeEventStatus(3), @event,
            CompositeEventOptions.None);

        for (int index = 0; index < count; index++)
        {
            int id = index;
            sagas[index] = new TestSaga { Id = id, Status = new CompositeEventStatus(1) };
            IBehaviorContext<TestSaga> context = ContextProxy.Create<IBehaviorContext<TestSaga>>(
                sagas[index],
                raise: async currentEvent =>
                {
                    raised.Enqueue((id, currentEvent));
                    await gate.Task;
                });
            var next = new RecordingBehavior { ExecuteUntyped = _ => { nextPairs.Enqueue(id); return Task.CompletedTask; } };
            tasks[index] = shared.ExecuteAsync(context, next);
        }

        Assert.Equal(count, raised.Count);
        Assert.All(tasks, task => Assert.False(task.IsCompleted));
        gate.SetResult();
        await Task.WhenAll(tasks);

        Assert.Equal(Enumerable.Range(0, count), raised.Select(pair => pair.Id).Order());
        Assert.All(raised, pair => Assert.Same(@event, pair.Event));
        Assert.Equal(Enumerable.Range(0, count), nextPairs.Order());
        Assert.All(sagas, saga => Assert.Equal(3, saga.Status.Bits));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-220-data-converter-body-type-next-and-route-identity")]
    public async Task DataConverter_DiagnosesIncompatibleRoutesAndPreservesExactTypedExecutionAndFaultTasksAsync()
    {
        var nested = new RecordingTypedActivity();
        var converter = new DataConverterActivity<TestSaga, Message>(nested);
        var untypedContext = ContextProxy.Create<IBehaviorContext<TestSaga>>(new TestSaga());
        var untypedNext = new RecordingBehavior();
        SagaStateMachineException body = Assert.Throws<SagaStateMachineException>(
            () => { _ = converter.ExecuteAsync(untypedContext, untypedNext); });
        Assert.Equal("This activity requires a body with the event, but no body was specified.", body.Message);
        Assert.Equal(0, untypedNext.TotalCalls);

        IBehaviorContext<TestSaga, OtherMessage> wrongContext =
            ContextProxy.Create<IBehaviorContext<TestSaga, OtherMessage>>(new TestSaga(), new OtherMessage("wrong"));
        var wrongNext = new RecordingTypedBehavior<OtherMessage>();
        SagaStateMachineException wrongBody = Assert.Throws<SagaStateMachineException>(
            () => { _ = converter.ExecuteAsync(wrongContext, wrongNext); });
        Assert.Equal("Expected Type Message but was OtherMessage", wrongBody.Message);

        IBehaviorContext<TestSaga, OtherMessage> missingBodyContext =
            ContextProxy.Create<IBehaviorContext<TestSaga, OtherMessage>>(new TestSaga());
        SagaStateMachineException missingBody = Assert.Throws<SagaStateMachineException>(
            () => { _ = converter.ExecuteAsync(missingBodyContext, wrongNext); });
        Assert.Equal("Expected Type Message but was null", missingBody.Message);
        Assert.Equal(0, nested.TotalCalls);
        Assert.Equal(0, wrongNext.TotalCalls);

        IBehaviorContext<TestSaga, Message> context =
            ContextProxy.Create<IBehaviorContext<TestSaga, Message>>(new TestSaga(), new Message(9));
        IBehaviorContext<TestSaga, DerivedMessage> derivedContext =
            ContextProxy.Create<IBehaviorContext<TestSaga, DerivedMessage>>(new TestSaga(), new DerivedMessage(9));
        var derivedNext = new RecordingTypedBehavior<DerivedMessage>();
        SagaStateMachineException wrongBehavior = Assert.Throws<SagaStateMachineException>(
            () => { _ = converter.ExecuteAsync(derivedContext, derivedNext); });
        Assert.Equal("The next behavior was not a valid type", wrongBehavior.Message);

        var next = new RecordingTypedBehavior<Message>();
        var executeCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        nested.ExecuteResult = executeCompletion.Task;
        Task executeTask = converter.ExecuteAsync(context, next);
        Assert.Same(executeCompletion.Task, executeTask);
        Assert.Same(context, nested.LastExecuteContext);
        Assert.Same(next, nested.LastExecuteNext);
        executeCompletion.SetResult();
        await executeTask;

        var failure = new MarkerException("fault");
        IBehaviorExceptionContext<TestSaga, Message, MarkerException> fault =
            ContextProxy.Create<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>(
                new TestSaga(), new Message(10), failure: failure);
        var faultCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        nested.FaultResult = faultCompletion.Task;
        Task faultTask = converter.FaultedAsync(fault, next);
        Assert.Same(faultCompletion.Task, faultTask);
        Assert.Same(fault, nested.LastFaultContext);
        Assert.Same(next, nested.LastFaultNext);
        faultCompletion.SetResult();
        await faultTask;

        IBehaviorExceptionContext<TestSaga, OtherMessage, MarkerException> wrongFault =
            ContextProxy.Create<IBehaviorExceptionContext<TestSaga, OtherMessage, MarkerException>>(
                new TestSaga(), new OtherMessage("wrong"), failure: failure);
        SagaStateMachineException wrongFaultBody = Assert.Throws<SagaStateMachineException>(
            () => { _ = converter.FaultedAsync(wrongFault, wrongNext); });
        Assert.Equal("Expected Type Message but was OtherMessage", wrongFaultBody.Message);

        IBehaviorExceptionContext<TestSaga, OtherMessage, MarkerException> missingFaultBody =
            ContextProxy.Create<IBehaviorExceptionContext<TestSaga, OtherMessage, MarkerException>>(
                new TestSaga(), failure: failure);
        SagaStateMachineException missingFault = Assert.Throws<SagaStateMachineException>(
            () => { _ = converter.FaultedAsync(missingFaultBody, wrongNext); });
        Assert.Equal("Expected Type Message but was null", missingFault.Message);

        IBehaviorExceptionContext<TestSaga, DerivedMessage, MarkerException> derivedFault =
            ContextProxy.Create<IBehaviorExceptionContext<TestSaga, DerivedMessage, MarkerException>>(
                new TestSaga(), new DerivedMessage(10), failure: failure);
        SagaStateMachineException wrongFaultNext = Assert.Throws<SagaStateMachineException>(
            () => { _ = converter.FaultedAsync(derivedFault, derivedNext); });
        Assert.Equal("The next behavior was not a valid type", wrongFaultNext.Message);

        var forwardingCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        untypedNext.FaultUntypedResult = forwardingCompletion.Task;
        IBehaviorExceptionContext<TestSaga, MarkerException> untypedFault =
            ContextProxy.Create<IBehaviorExceptionContext<TestSaga, MarkerException>>(new TestSaga(), failure: failure);
        Task forwardingTask = converter.FaultedAsync(untypedFault, untypedNext);
        Assert.Same(forwardingCompletion.Task, forwardingTask);
        Assert.Same(untypedFault, untypedNext.LastFaultContext);
        forwardingCompletion.SetResult();
        await forwardingTask;
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RETRY", "iteration-220-retry-attempt-cancellation-gating-and-continuation")]
    [SuppressMessage("Usage", "xUnit1051", Justification = "This contract test verifies exact cancellation-token ownership and identity.")]
    public async Task RetryExecution_ControlsAttemptsCancellationTypedGatingAndSingleContinuationAsync()
    {
        var trace = new List<string>();
        var retryBehavior = new RecordingBehavior
        {
            ExecuteUntyped = _ =>
            {
                int attempt = trace.Count(entry => entry.StartsWith("attempt", StringComparison.Ordinal)) + 1;
                trace.Add($"attempt:{attempt}");
                return attempt < 3 ? Task.FromException(new MarkerException($"failure-{attempt}")) : Task.CompletedTask;
            }
        };
        var next = new RecordingBehavior
        {
            ExecuteUntyped = _ => { trace.Add("next"); return Task.CompletedTask; }
        };
        IBehaviorContext<TestSaga> context = ContextProxy.Create<IBehaviorContext<TestSaga>>(new TestSaga());
        var retry = new RetryActivity<TestSaga>(Retry.Immediate(2), retryBehavior);

        await retry.ExecuteAsync(context, next);

        Assert.Equal(["attempt:1", "attempt:2", "attempt:3", "next"], trace);
        Assert.Equal(3, retryBehavior.ExecuteUntypedCalls);
        Assert.Equal(1, next.ExecuteUntypedCalls);
        Assert.Same(context, next.LastExecuteContext);

        var retryGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var awaitTrace = new List<string>();
        var awaitingBehavior = new RecordingBehavior
        {
            ExecuteUntyped = _ =>
            {
                awaitTrace.Add("retry");
                return retryGate.Task;
            }
        };
        var awaitingNext = new RecordingBehavior
        {
            ExecuteUntyped = _ =>
            {
                awaitTrace.Add("next");
                return Task.CompletedTask;
            }
        };
        Task awaitingTask = new RetryActivity<TestSaga>(Retry.None, awaitingBehavior).ExecuteAsync(context, awaitingNext);
        Assert.Equal(["retry"], awaitTrace);
        Assert.False(awaitingTask.IsCompleted);
        Assert.Equal(0, awaitingNext.ExecuteUntypedCalls);
        retryGate.SetResult();
        await awaitingTask;
        Assert.Equal(["retry", "next"], awaitTrace);
        Assert.Equal(1, awaitingBehavior.ExecuteUntypedCalls);
        Assert.Equal(1, awaitingNext.ExecuteUntypedCalls);

        using var source = new CancellationTokenSource();
        source.Cancel();
        var canceledBehavior = new RecordingBehavior();
        var canceledNext = new RecordingBehavior();
        IBehaviorContext<TestSaga> canceledContext = ContextProxy.CreateWithCancellation<IBehaviorContext<TestSaga>>(
            new TestSaga(), source.Token);
        var canceledRetry = new RetryActivity<TestSaga>(Retry.Immediate(2), canceledBehavior);
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => canceledRetry.ExecuteAsync(canceledContext, canceledNext));
        Assert.Equal(source.Token, canceled.CancellationToken);
        Assert.Equal(0, canceledBehavior.TotalCalls);
        Assert.Equal(0, canceledNext.TotalCalls);

        using var operationSource = new CancellationTokenSource();
        operationSource.Cancel();
        var operationBehavior = new RecordingBehavior
        {
            ExecuteUntyped = _ => Task.FromCanceled(operationSource.Token)
        };
        var operationNext = new RecordingBehavior();
        var operationRetry = new RetryActivity<TestSaga>(Retry.Immediate(2), operationBehavior);
        OperationCanceledException operationCanceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => operationRetry.ExecuteAsync(context, operationNext));
        Assert.Equal(operationSource.Token, operationCanceled.CancellationToken);
        Assert.Equal(1, operationBehavior.ExecuteUntypedCalls);
        Assert.Equal(0, operationNext.TotalCalls);

        var continuationFailure = new MarkerException("continuation");
        var successfulBehavior = new RecordingBehavior();
        var failingNext = new RecordingBehavior
        {
            ExecuteUntyped = _ => Task.FromException(continuationFailure)
        };
        var continuationRetry = new RetryActivity<TestSaga>(Retry.Immediate(2), successfulBehavior);
        MarkerException continuationCaught = await Assert.ThrowsAsync<MarkerException>(
            () => continuationRetry.ExecuteAsync(context, failingNext));
        Assert.Same(continuationFailure, continuationCaught);
        Assert.Equal(1, successfulBehavior.ExecuteUntypedCalls);
        Assert.Equal(1, failingNext.ExecuteUntypedCalls);

        using var continuationCancellation = new CancellationTokenSource();
        continuationCancellation.Cancel();
        var typedContinuationBehavior = new RecordingBehavior();
        var canceledTypedNext = new RecordingTypedBehavior<Message>
        {
            ExecuteHandler = _ => Task.FromCanceled(continuationCancellation.Token)
        };
        IBehaviorContext<TestSaga, Message> typedContinuationContext =
            ContextProxy.Create<IBehaviorContext<TestSaga, Message>>(new TestSaga(), new Message(10));
        var typedContinuationRetry = new RetryActivity<TestSaga>(Retry.Immediate(2), typedContinuationBehavior);
        OperationCanceledException typedContinuationCanceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => typedContinuationRetry.ExecuteAsync(typedContinuationContext, canceledTypedNext));
        Assert.Equal(continuationCancellation.Token, typedContinuationCanceled.CancellationToken);
        Assert.Equal(1, typedContinuationBehavior.ExecuteTypedCalls);
        Assert.Equal(1, canceledTypedNext.ExecuteCalls);

        var gatedBehavior = new RecordingBehavior();
        var typedRetry = new RetryActivity<TestSaga, Message>(Retry.Immediate(1), gatedBehavior);
        var matchingNext = new RecordingTypedBehavior<Message>();
        IBehaviorContext<TestSaga, Message> matching =
            ContextProxy.Create<IBehaviorContext<TestSaga, Message>>(new TestSaga(), new Message(11));
        await typedRetry.ExecuteAsync(matching, matchingNext);
        Assert.Equal(1, gatedBehavior.ExecuteTypedCalls);
        Assert.Same(matching, gatedBehavior.LastExecuteContext);
        Assert.Equal(1, matchingNext.ExecuteCalls);

        var derivedNext = new RecordingTypedBehavior<DerivedMessage>();
        IBehaviorContext<TestSaga, DerivedMessage> derived =
            ContextProxy.Create<IBehaviorContext<TestSaga, DerivedMessage>>(new TestSaga(), new DerivedMessage(12));
        await typedRetry.ExecuteAsync(derived, derivedNext);
        Assert.Equal(2, gatedBehavior.ExecuteTypedCalls);
        Assert.Same(derived, gatedBehavior.LastExecuteContext);
        Assert.Equal(1, derivedNext.ExecuteCalls);
        Assert.Same(derived, derivedNext.LastExecuteContext);

        var foreignNext = new RecordingTypedBehavior<OtherMessage>();
        IBehaviorContext<TestSaga, OtherMessage> foreign =
            ContextProxy.Create<IBehaviorContext<TestSaga, OtherMessage>>(new TestSaga(), new OtherMessage("skip"));
        await typedRetry.ExecuteAsync(foreign, foreignNext);
        Assert.Equal(2, gatedBehavior.ExecuteTypedCalls);
        Assert.Equal(1, foreignNext.ExecuteCalls);
        Assert.Same(foreign, foreignNext.LastExecuteContext);

        var covariantContinuationFailure = new MarkerException("covariant-continuation");
        var covariantBehavior = new RecordingBehavior();
        var covariantNext = new RecordingTypedBehavior<DerivedMessage>
        {
            ExecuteHandler = _ => Task.FromException(covariantContinuationFailure)
        };
        var covariantRetry = new RetryActivity<TestSaga, Message>(Retry.Immediate(2), covariantBehavior);
        MarkerException covariantCaught = await Assert.ThrowsAsync<MarkerException>(
            () => covariantRetry.ExecuteAsync(derived, covariantNext));
        Assert.Same(covariantContinuationFailure, covariantCaught);
        Assert.Equal(1, covariantBehavior.ExecuteTypedCalls);
        Assert.Equal(1, covariantNext.ExecuteCalls);

        using var foreignCancellation = new CancellationTokenSource();
        foreignCancellation.Cancel();
        var canceledForeignNext = new RecordingTypedBehavior<OtherMessage>
        {
            ExecuteHandler = _ => Task.FromCanceled(foreignCancellation.Token)
        };
        var skippedBehavior = new RecordingBehavior();
        var skippedRetry = new RetryActivity<TestSaga, Message>(Retry.Immediate(2), skippedBehavior);
        OperationCanceledException foreignCanceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => skippedRetry.ExecuteAsync(foreign, canceledForeignNext));
        Assert.Equal(foreignCancellation.Token, foreignCanceled.CancellationToken);
        Assert.Equal(0, skippedBehavior.TotalCalls);
        Assert.Equal(1, canceledForeignNext.ExecuteCalls);

        SagaStateMachineException noBody = Assert.Throws<SagaStateMachineException>(
            () => { _ = typedRetry.ExecuteAsync(context, next); });
        Assert.Equal("This activity requires a body with the event, but no body was specified.", noBody.Message);

        var faultCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        matchingNext.FaultResult = faultCompletion.Task;
        IBehaviorExceptionContext<TestSaga, Message, MarkerException> fault =
            ContextProxy.Create<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>(
                new TestSaga(), new Message(12), failure: new MarkerException("fault"));
        Task faultTask = typedRetry.FaultedAsync(fault, matchingNext);
        Assert.Same(faultCompletion.Task, faultTask);
        Assert.Same(fault, matchingNext.LastFaultContext);
        Assert.Equal(2, gatedBehavior.ExecuteTypedCalls);
        faultCompletion.SetResult();
        await faultTask;

        IBehaviorExceptionContext<TestSaga, MarkerException> untypedFault =
            ContextProxy.Create<IBehaviorExceptionContext<TestSaga, MarkerException>>(
                new TestSaga(), failure: new MarkerException("untyped-fault"));
        var retryUntypedFaultNext = new RecordingBehavior { FaultUntypedResult = NewIncompleteTaskAsync() };
        Assert.Same(retryUntypedFaultNext.FaultUntypedResult, retry.FaultedAsync(untypedFault, retryUntypedFaultNext));
        Assert.Same(untypedFault, retryUntypedFaultNext.LastFaultContext);

        var retryTypedFaultNext = new RecordingTypedBehavior<Message> { FaultResult = NewIncompleteTaskAsync() };
        Assert.Same(retryTypedFaultNext.FaultResult, retry.FaultedAsync(fault, retryTypedFaultNext));
        Assert.Same(fault, retryTypedFaultNext.LastFaultContext);

        var gatedUntypedFaultNext = new RecordingBehavior { FaultUntypedResult = NewIncompleteTaskAsync() };
        Assert.Same(gatedUntypedFaultNext.FaultUntypedResult, typedRetry.FaultedAsync(untypedFault, gatedUntypedFaultNext));
        Assert.Same(untypedFault, gatedUntypedFaultNext.LastFaultContext);

        var gatedTypedFaultNext = new RecordingTypedBehavior<Message> { FaultResult = NewIncompleteTaskAsync() };
        Assert.Same(gatedTypedFaultNext.FaultResult, typedRetry.FaultedAsync(fault, gatedTypedFaultNext));
        Assert.Same(fault, gatedTypedFaultNext.LastFaultContext);

        int untypedPosts = await CountSynchronizationContextPostsAsync(gate =>
        {
            var behavior = new RecordingBehavior { ExecuteUntyped = _ => gate };
            return new RetryActivity<TestSaga>(Retry.None, behavior).ExecuteAsync(context, new RecordingBehavior());
        });
        int typedPosts = await CountSynchronizationContextPostsAsync(gate =>
        {
            var behavior = new RecordingBehavior { ExecuteTyped = _ => gate };
            return new RetryActivity<TestSaga>(Retry.None, behavior).ExecuteAsync(matching, new RecordingTypedBehavior<Message>());
        });
        int gatedPosts = await CountSynchronizationContextPostsAsync(gate =>
        {
            var behavior = new RecordingBehavior { ExecuteTyped = _ => gate };
            return new RetryActivity<TestSaga, Message>(Retry.None, behavior).ExecuteAsync(matching, new RecordingTypedBehavior<Message>());
        });
        int untypedContinuationPosts = await CountSynchronizationContextPostsAsync(gate =>
        {
            var continuation = new RecordingBehavior { ExecuteUntyped = _ => gate };
            return new RetryActivity<TestSaga>(Retry.None, new RecordingBehavior()).ExecuteAsync(context, continuation);
        });
        int typedContinuationPosts = await CountSynchronizationContextPostsAsync(gate =>
        {
            var continuation = new RecordingTypedBehavior<Message> { ExecuteHandler = _ => gate };
            return new RetryActivity<TestSaga>(Retry.None, new RecordingBehavior()).ExecuteAsync(matching, continuation);
        });
        int gatedContinuationPosts = await CountSynchronizationContextPostsAsync(gate =>
        {
            var continuation = new RecordingTypedBehavior<Message> { ExecuteHandler = _ => gate };
            return new RetryActivity<TestSaga, Message>(Retry.None, new RecordingBehavior()).ExecuteAsync(matching, continuation);
        });
        Assert.Equal(0, untypedPosts);
        Assert.Equal(0, typedPosts);
        Assert.Equal(0, gatedPosts);
        Assert.Equal(0, untypedContinuationPosts);
        Assert.Equal(0, typedContinuationPosts);
        Assert.Equal(0, gatedContinuationPosts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RETRY", "iteration-220-retry-event-execution-unwrapping-stack-and-identity")]
    public async Task RetryExecution_UnwrapsEventFailureWithOriginalIdentityAndStackWithoutContinuationAsync()
    {
        MarkerException inner = CaptureMarkerFailure();
        var wrapper = new EventExecutionException("wrapped", inner);
        IBehaviorContext<TestSaga> context = ContextProxy.Create<IBehaviorContext<TestSaga>>(new TestSaga());
        var next = new RecordingBehavior();
        var behavior = new RecordingBehavior { ExecuteUntyped = _ => Task.FromException(wrapper) };
        var retry = new RetryActivity<TestSaga>(Retry.Immediate(1), behavior);

        MarkerException caught = await Assert.ThrowsAsync<MarkerException>(() => retry.ExecuteAsync(context, next));

        Assert.Same(inner, caught);
        Assert.Contains(nameof(ThrowMarkerFailure), caught.StackTrace, StringComparison.Ordinal);
        Assert.Equal(2, behavior.ExecuteUntypedCalls);
        Assert.Equal(0, next.TotalCalls);

        var typedBehavior = new RecordingBehavior { ExecuteTyped = _ => Task.FromException(wrapper) };
        var typedNext = new RecordingTypedBehavior<Message>();
        IBehaviorContext<TestSaga, Message> typedContext =
            ContextProxy.Create<IBehaviorContext<TestSaga, Message>>(new TestSaga(), new Message(13));
        var typedRetry = new RetryActivity<TestSaga>(Retry.Immediate(1), typedBehavior);
        MarkerException typedCaught = await Assert.ThrowsAsync<MarkerException>(
            () => typedRetry.ExecuteAsync(typedContext, typedNext));
        Assert.Same(inner, typedCaught);
        Assert.Contains(nameof(ThrowMarkerFailure), typedCaught.StackTrace, StringComparison.Ordinal);
        Assert.Equal(2, typedBehavior.ExecuteTypedCalls);
        Assert.Equal(0, typedNext.TotalCalls);

        var gatedBehavior = new RecordingBehavior { ExecuteTyped = _ => Task.FromException(wrapper) };
        var gatedNext = new RecordingTypedBehavior<Message>();
        var gatedRetry = new RetryActivity<TestSaga, Message>(Retry.Immediate(1), gatedBehavior);
        MarkerException gatedCaught = await Assert.ThrowsAsync<MarkerException>(
            () => gatedRetry.ExecuteAsync(typedContext, gatedNext));
        Assert.Same(inner, gatedCaught);
        Assert.Contains(nameof(ThrowMarkerFailure), gatedCaught.StackTrace, StringComparison.Ordinal);
        Assert.Equal(2, gatedBehavior.ExecuteTypedCalls);
        Assert.Equal(0, gatedNext.TotalCalls);

        var wrapperWithoutInner = new EventExecutionException("without-inner");
        var bareBehavior = new RecordingBehavior { ExecuteUntyped = _ => Task.FromException(wrapperWithoutInner) };
        var bareRetry = new RetryActivity<TestSaga>(Retry.None, bareBehavior);
        EventExecutionException bareCaught = await Assert.ThrowsAsync<EventExecutionException>(
            () => bareRetry.ExecuteAsync(context, next));
        Assert.Same(wrapperWithoutInner, bareCaught);
        Assert.Equal(1, bareBehavior.ExecuteUntypedCalls);
        Assert.Equal(0, next.TotalCalls);

        var bareTypedBehavior = new RecordingBehavior { ExecuteTyped = _ => Task.FromException(wrapperWithoutInner) };
        var bareTypedNext = new RecordingTypedBehavior<Message>();
        var bareTypedRetry = new RetryActivity<TestSaga>(Retry.None, bareTypedBehavior);
        EventExecutionException bareTypedCaught = await Assert.ThrowsAsync<EventExecutionException>(
            () => bareTypedRetry.ExecuteAsync(typedContext, bareTypedNext));
        Assert.Same(wrapperWithoutInner, bareTypedCaught);
        Assert.Equal(1, bareTypedBehavior.ExecuteTypedCalls);
        Assert.Equal(0, bareTypedNext.TotalCalls);

        var bareGatedBehavior = new RecordingBehavior { ExecuteTyped = _ => Task.FromException(wrapperWithoutInner) };
        var bareGatedNext = new RecordingTypedBehavior<Message>();
        var bareGatedRetry = new RetryActivity<TestSaga, Message>(Retry.None, bareGatedBehavior);
        EventExecutionException bareGatedCaught = await Assert.ThrowsAsync<EventExecutionException>(
            () => bareGatedRetry.ExecuteAsync(typedContext, bareGatedNext));
        Assert.Same(wrapperWithoutInner, bareGatedCaught);
        Assert.Equal(1, bareGatedBehavior.ExecuteTypedCalls);
        Assert.Equal(0, bareGatedNext.TotalCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-220-composite-converter-retry-visitor-probe-and-failure-identity")]
    public void VisitorAndProbe_ExposeOrderedNestedStructureAndPreserveCollaboratorFailures()
    {
        var visitor = new RecordingVisitor();
        var probe = new RecordingProbeContext();
        var accessor = new RecordingAccessor();
        var @event = new TriggerEvent("Composite");
        var composite = new CompositeEventActivity<TestSaga>(accessor, 0x20, new CompositeEventStatus(0x20), @event,
            CompositeEventOptions.None);
        var nested = new RecordingTypedActivity();
        var converter = new DataConverterActivity<TestSaga, Message>(nested);
        var behavior = new RecordingBehavior();
        var retry = new RetryActivity<TestSaga>(Retry.None, behavior);
        var typedRetry = new RetryActivity<TestSaga, Message>(Retry.None, behavior);

        composite.Accept(visitor);
        converter.Accept(visitor);
        retry.Accept(visitor);
        typedRetry.Accept(visitor);

        Assert.Equal([composite, converter, nested, retry, behavior, typedRetry, behavior], visitor.Visited);

        composite.Probe(probe);
        converter.Probe(probe);
        retry.Probe(probe);
        typedRetry.Probe(probe);
        Assert.Equal(
            [
                "scope:compositeEvent",
                "compositeEvent/accessor:status",
                "compositeEvent/event:Composite",
                "compositeEvent/flag:00000020",
                "typed:activity",
                "scope:retry",
                "retry/behavior:retry",
                "scope:retry",
                "retry/behavior:retry"
            ],
            probe.Calls);

        var visitorFailure = new MarkerException("visitor");
        var failingVisitor = new RecordingVisitor { Failure = visitorFailure };
        int nestedAcceptCalls = nested.AcceptCalls;
        MarkerException visitorCaught = Assert.Throws<MarkerException>(() => converter.Accept(failingVisitor));
        Assert.Same(visitorFailure, visitorCaught);
        Assert.Equal(nestedAcceptCalls, nested.AcceptCalls);

        var nestedVisitor = new RecordingVisitor();
        nested.AcceptFailure = visitorFailure;
        MarkerException nestedVisitorCaught = Assert.Throws<MarkerException>(() => converter.Accept(nestedVisitor));
        Assert.Same(visitorFailure, nestedVisitorCaught);
        Assert.Equal([converter, nested], nestedVisitor.Visited);
        Assert.Equal(nestedAcceptCalls + 1, nested.AcceptCalls);
        nested.AcceptFailure = null;

        behavior.AcceptFailure = visitorFailure;
        var retryVisitor = new RecordingVisitor();
        MarkerException retryVisitorCaught = Assert.Throws<MarkerException>(() => retry.Accept(retryVisitor));
        Assert.Same(visitorFailure, retryVisitorCaught);
        Assert.Equal([retry, behavior], retryVisitor.Visited);
        behavior.AcceptFailure = null;

        var probeFailure = new MarkerException("probe");
        var failingProbe = new RecordingProbeContext(probeFailure);
        MarkerException probeCaught = Assert.Throws<MarkerException>(() => retry.Probe(failingProbe));
        Assert.Same(probeFailure, probeCaught);
        Assert.Equal(2, behavior.ProbeCalls);

        accessor.ProbeFailure = probeFailure;
        var accessorProbe = new RecordingProbeContext();
        MarkerException accessorProbeCaught = Assert.Throws<MarkerException>(() => composite.Probe(accessorProbe));
        Assert.Same(probeFailure, accessorProbeCaught);
        Assert.Equal(["scope:compositeEvent"], accessorProbe.Calls);
        accessor.ProbeFailure = null;

        nested.ProbeFailure = probeFailure;
        var nestedProbe = new RecordingProbeContext();
        MarkerException nestedProbeCaught = Assert.Throws<MarkerException>(() => converter.Probe(nestedProbe));
        Assert.Same(probeFailure, nestedProbeCaught);
        Assert.Empty(nestedProbe.Calls);
        nested.ProbeFailure = null;

        behavior.ProbeFailure = probeFailure;
        var behaviorProbe = new RecordingProbeContext();
        MarkerException behaviorProbeCaught = Assert.Throws<MarkerException>(() => typedRetry.Probe(behaviorProbe));
        Assert.Same(probeFailure, behaviorProbeCaught);
        Assert.Equal(["scope:retry"], behaviorProbe.Calls);
        behavior.ProbeFailure = null;
    }

    static void AssertType(Type openType, string[] genericNames, int publicMethodCount)
    {
        Assert.True(openType.IsPublic);
        Assert.False(openType.IsAbstract);
        Assert.False(openType.IsSealed);
        Type[] typeArguments = openType.GetGenericArguments();
        Assert.Equal(genericNames, typeArguments.Select(argument => argument.Name));
        Assert.Equal(
            GenericParameterAttributes.ReferenceTypeConstraint,
            typeArguments[0].GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Equal([typeof(ISagaStateMachineInstance)], typeArguments[0].GetGenericParameterConstraints());
        foreach (Type argument in typeArguments.Skip(1))
            AssertReferenceTypeParameter(argument);

        Type closedType = typeArguments.Length == 1
            ? openType.MakeGenericType(typeof(TestSaga))
            : openType.MakeGenericType(typeof(TestSaga), typeof(Message));
        MethodInfo[] methods = closedType.GetMethods(DeclaredPublic).Where(method => !method.IsSpecialName).ToArray();
        Assert.Equal(publicMethodCount, methods.Length);

        MethodInfo accept = Assert.Single(methods, method => method.Name == "Accept");
        AssertMethodSignature(accept, typeof(void), ("visitor", typeof(IStateMachineVisitor)));
        MethodInfo probe = Assert.Single(methods, method => method.Name == "Probe");
        AssertMethodSignature(probe, typeof(void), ("context", typeof(ProbeContext)));

        MethodInfo execute = Assert.Single(methods, method => method.Name == "ExecuteAsync" && !method.IsGenericMethod);
        AssertMethodSignature(
            execute,
            typeof(Task),
            ("context", typeof(IBehaviorContext<TestSaga>)),
            ("next", typeof(IBehavior<TestSaga>)));

        MethodInfo typedExecute = Assert.Single(methods, method => method.Name == "ExecuteAsync" && method.IsGenericMethod);
        Type messageType = Assert.Single(typedExecute.GetGenericArguments());
        Assert.Equal(openType == typeof(CompositeEventActivity<>) ? "TData" : "T", messageType.Name);
        AssertReferenceTypeParameter(messageType);
        AssertMethodSignature(
            typedExecute,
            typeof(Task),
            ("context", typeof(IBehaviorContext<,>).MakeGenericType(typeof(TestSaga), messageType)),
            ("next", typeof(IBehavior<,>).MakeGenericType(typeof(TestSaga), messageType)));

        MethodInfo fault = Assert.Single(methods,
            method => method.Name == "FaultedAsync" && method.GetGenericArguments().Length == 1);
        Type faultType = Assert.Single(fault.GetGenericArguments());
        AssertExceptionTypeParameter(faultType);
        AssertMethodSignature(
            fault,
            typeof(Task),
            ("context", typeof(IBehaviorExceptionContext<,>).MakeGenericType(typeof(TestSaga), faultType)),
            ("next", typeof(IBehavior<TestSaga>)));

        MethodInfo typedFault = Assert.Single(methods,
            method => method.Name == "FaultedAsync" && method.GetGenericArguments().Length == 2);
        Type[] typedFaultArguments = typedFault.GetGenericArguments();
        Assert.Equal("T", typedFaultArguments[0].Name);
        AssertReferenceTypeParameter(typedFaultArguments[0]);
        AssertExceptionTypeParameter(typedFaultArguments[1]);
        AssertMethodSignature(
            typedFault,
            typeof(Task),
            ("context", typeof(IBehaviorExceptionContext<,,>).MakeGenericType(
                typeof(TestSaga), typedFaultArguments[0], typedFaultArguments[1])),
            ("next", typeof(IBehavior<,>).MakeGenericType(typeof(TestSaga), typedFaultArguments[0])));
    }

    static void AssertReferenceTypeParameter(Type argument)
    {
        Assert.Equal(
            GenericParameterAttributes.ReferenceTypeConstraint,
            argument.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Empty(argument.GetGenericParameterConstraints());
    }

    static void AssertExceptionTypeParameter(Type argument)
    {
        Assert.Equal("TException", argument.Name);
        Assert.Equal(
            GenericParameterAttributes.None,
            argument.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Equal([typeof(Exception)], argument.GetGenericParameterConstraints());
    }

    static void AssertMethodSignature(
        MethodInfo method,
        Type returnType,
        params (string Name, Type Type)[] expectedParameters)
    {
        Assert.Equal(returnType, method.ReturnType);
        var nullability = new NullabilityInfoContext();
        if (returnType == typeof(Task))
            Assert.Equal(NullabilityState.NotNull, nullability.Create(method.ReturnParameter).ReadState);

        ParameterInfo[] actualParameters = method.GetParameters();
        Assert.Equal(expectedParameters.Length, actualParameters.Length);
        for (int index = 0; index < expectedParameters.Length; index++)
        {
            Assert.Equal(expectedParameters[index].Name, actualParameters[index].Name);
            Assert.Equal(expectedParameters[index].Type, actualParameters[index].ParameterType);
            Assert.Equal(NullabilityState.NotNull, nullability.Create(actualParameters[index]).ReadState);
        }
    }

    static Type[] ActivityTypes() =>
    [
        typeof(CompositeEventActivity<TestSaga>),
        typeof(DataConverterActivity<TestSaga, Message>),
        typeof(RetryActivity<TestSaga>),
        typeof(RetryActivity<TestSaga, Message>)
    ];

    static void AssertAllLifecycleNulls(
        IStateMachineActivity<TestSaga> activity,
        IBehaviorContext<TestSaga> context,
        IBehaviorContext<TestSaga, Message> dataContext,
        IBehaviorExceptionContext<TestSaga, MarkerException> fault,
        IBehaviorExceptionContext<TestSaga, Message, MarkerException> dataFault,
        IBehavior<TestSaga> next,
        IBehavior<TestSaga, Message> dataNext)
    {
        AssertArgument("context", () => activity.ExecuteAsync(null!, next));
        AssertArgument("next", () => activity.ExecuteAsync(context, null!));
        AssertArgument("context", () => activity.ExecuteAsync<Message>(null!, dataNext));
        AssertArgument("next", () => activity.ExecuteAsync(dataContext, null!));
        AssertArgument("context", () => activity.FaultedAsync<MarkerException>(null!, next));
        AssertArgument("next", () => activity.FaultedAsync(fault, null!));
        AssertArgument("context", () => activity.FaultedAsync<Message, MarkerException>(null!, dataNext));
        AssertArgument("next", () => activity.FaultedAsync(dataFault, null!));
    }

    static async Task AssertAllAsyncLifecycleNullsAsync(
        IStateMachineActivity<TestSaga> activity,
        IBehaviorContext<TestSaga> context,
        IBehaviorContext<TestSaga, Message> dataContext,
        IBehaviorExceptionContext<TestSaga, MarkerException> fault,
        IBehaviorExceptionContext<TestSaga, Message, MarkerException> dataFault,
        IBehavior<TestSaga> next,
        IBehavior<TestSaga, Message> dataNext)
    {
        await AssertArgumentAsync("context", () => activity.ExecuteAsync(null!, next));
        await AssertArgumentAsync("next", () => activity.ExecuteAsync(context, null!));
        await AssertArgumentAsync("context", () => activity.ExecuteAsync<Message>(null!, dataNext));
        await AssertArgumentAsync("next", () => activity.ExecuteAsync(dataContext, null!));
        AssertArgument("context", () => activity.FaultedAsync<MarkerException>(null!, next));
        AssertArgument("next", () => activity.FaultedAsync(fault, null!));
        AssertArgument("context", () => activity.FaultedAsync<Message, MarkerException>(null!, dataNext));
        AssertArgument("next", () => activity.FaultedAsync(dataFault, null!));
    }

    static void AssertArgument(string parameterName, Action action)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(parameterName, exception.ParamName);
    }

    static async Task AssertArgumentAsync(string parameterName, Func<Task> action)
    {
        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(action);
        Assert.Equal(parameterName, exception.ParamName);
    }

    static Task NewIncompleteTaskAsync() =>
        new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task;

    static async Task<int> CountSynchronizationContextPostsAsync(Func<Task, Task> execute)
    {
        var result = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            var synchronizationContext = new RecordingSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);

            try
            {
                var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                Task operation = execute(gate.Task);
                ThreadPool.QueueUserWorkItem(_ => gate.SetResult());
                operation.GetAwaiter().GetResult();
                result.TrySetResult(synchronizationContext.PostCount);
            }
            catch (Exception exception)
            {
                result.TrySetException(exception);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(null);
            }
        })
        {
            IsBackground = true
        };

        thread.Start();
        return await result.Task.ConfigureAwait(false);
    }

    static MarkerException CaptureMarkerFailure()
    {
        try
        {
            ThrowMarkerFailure();
        }
        catch (MarkerException exception)
        {
            return exception;
        }

        throw new InvalidOperationException("The marker failure was not thrown.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ThrowMarkerFailure() => throw new MarkerException("inner");

    public sealed class TestSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = Guid.NewGuid();
        public int Id { get; init; }
        public CompositeEventStatus Status { get; set; }
    }

    public record Message(int Value);
    public sealed record DerivedMessage(int Value) : Message(Value);
    public sealed record OtherMessage(string Value);
    public sealed class MarkerException(string message) : Exception(message);

    sealed class RecordingAccessor(List<string>? trace = null) : ICompositeEventStatusAccessor<TestSaga>
    {
        public ConcurrentQueue<string> Calls { get; } = [];
        public MarkerException? ProbeFailure { get; set; }

        public CompositeEventStatus Get(TestSaga instance)
        {
            Calls.Enqueue($"get:{instance.Id}");
            trace?.Add("get");
            return instance.Status;
        }

        public void Set(TestSaga instance, CompositeEventStatus status)
        {
            Calls.Enqueue($"set:{instance.Id}:{status.Bits:X8}");
            trace?.Add($"set:{status.Bits:X8}");
            instance.Status = status;
        }

        public void Probe(ProbeContext context)
        {
            if (ProbeFailure is not null)
                throw ProbeFailure;

            context.Add("accessor", "status");
        }
    }

    public class ContextProxy : DispatchProxy
    {
        TestSaga _saga = null!;
        object? _message;
        Exception? _failure;
        CancellationToken _cancellationToken;
        Func<IEvent, Task>? _raise;

        public static TInterface Create<TInterface>(
            TestSaga saga,
            object? message = null,
            Exception? failure = null,
            Func<IEvent, Task>? raise = null)
            where TInterface : class
        {
            return CreateCore<TInterface>(saga, message, failure, default, raise);
        }

        public static TInterface CreateWithCancellation<TInterface>(TestSaga saga, CancellationToken cancellationToken)
            where TInterface : class
        {
            return CreateCore<TInterface>(saga, null, null, cancellationToken, null);
        }

        static TInterface CreateCore<TInterface>(
            TestSaga saga,
            object? message,
            Exception? failure,
            CancellationToken cancellationToken,
            Func<IEvent, Task>? raise)
            where TInterface : class
        {
            TInterface result = DispatchProxy.Create<TInterface, ContextProxy>();
            var proxy = (ContextProxy)(object)result;
            proxy._saga = saga;
            proxy._message = message;
            proxy._failure = failure;
            proxy._cancellationToken = cancellationToken;
            proxy._raise = raise;
            return result;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_Saga" => _saga,
                "get_Message" => _message,
                "get_Exception" => _failure,
                "get_CancellationToken" => _cancellationToken,
                "RaiseAsync" when args is [IEvent raised, ..] => (_raise ?? (_ => Task.CompletedTask))(raised),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected context member: {targetMethod?.Name}.")
            };
        }
    }

    sealed class RecordingBehavior : IBehavior<TestSaga>
    {
        public Func<IBehaviorContext<TestSaga>, Task>? ExecuteUntyped { get; init; }
        public Func<object, Task>? ExecuteTyped { get; init; }
        public Task FaultUntypedResult { get; set; } = Task.CompletedTask;
        public int ExecuteUntypedCalls { get; private set; }
        public int ExecuteTypedCalls { get; private set; }
        public int FaultUntypedCalls { get; private set; }
        public int FaultTypedCalls { get; private set; }
        public int AcceptCalls { get; private set; }
        public int ProbeCalls { get; private set; }
        public MarkerException? AcceptFailure { get; set; }
        public MarkerException? ProbeFailure { get; set; }
        public object? LastExecuteContext { get; private set; }
        public object? LastFaultContext { get; private set; }
        public int TotalCalls => ExecuteUntypedCalls + ExecuteTypedCalls + FaultUntypedCalls + FaultTypedCalls + AcceptCalls + ProbeCalls;

        public Task ExecuteAsync(IBehaviorContext<TestSaga> context)
        {
            ExecuteUntypedCalls++;
            LastExecuteContext = context;
            return ExecuteUntyped?.Invoke(context) ?? Task.CompletedTask;
        }

        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context) where T : class
        {
            ExecuteTypedCalls++;
            LastExecuteContext = context;
            return ExecuteTyped?.Invoke(context) ?? Task.CompletedTask;
        }

        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TestSaga, T, TException> context)
            where T : class where TException : Exception
        {
            FaultTypedCalls++;
            LastFaultContext = context;
            return Task.CompletedTask;
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, TException> context) where TException : Exception
        {
            FaultUntypedCalls++;
            LastFaultContext = context;
            return FaultUntypedResult;
        }

        public void Accept(IStateMachineVisitor visitor)
        {
            AcceptCalls++;
            visitor.Visit(this);
            if (AcceptFailure is not null)
                throw AcceptFailure;
        }

        public void Probe(ProbeContext context)
        {
            ProbeCalls++;
            if (ProbeFailure is not null)
                throw ProbeFailure;

            context.Add("behavior", "retry");
        }
    }

    sealed class RecordingTypedBehavior<TMessage> : IBehavior<TestSaga, TMessage> where TMessage : class
    {
        public Func<IBehaviorContext<TestSaga, TMessage>, Task>? ExecuteHandler { get; init; }
        public Task FaultResult { get; set; } = Task.CompletedTask;
        public int ExecuteCalls { get; private set; }
        public int FaultCalls { get; private set; }
        public int AcceptCalls { get; private set; }
        public int ProbeCalls { get; private set; }
        public MarkerException? AcceptFailure { get; set; }
        public MarkerException? ProbeFailure { get; set; }
        public object? LastExecuteContext { get; private set; }
        public object? LastFaultContext { get; private set; }
        public int TotalCalls => ExecuteCalls + FaultCalls + AcceptCalls + ProbeCalls;

        public Task ExecuteAsync(IBehaviorContext<TestSaga, TMessage> context)
        {
            ExecuteCalls++;
            LastExecuteContext = context;
            return ExecuteHandler?.Invoke(context) ?? Task.CompletedTask;
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, TMessage, TException> context)
            where TException : Exception
        {
            FaultCalls++;
            LastFaultContext = context;
            return FaultResult;
        }

        public void Accept(IStateMachineVisitor visitor)
        {
            AcceptCalls++;
            visitor.Visit(this);
            if (AcceptFailure is not null)
                throw AcceptFailure;
        }

        public void Probe(ProbeContext context)
        {
            ProbeCalls++;
            context.Add("behavior", typeof(TMessage).Name);
        }
    }

    sealed class RecordingTypedActivity : IStateMachineActivity<TestSaga, Message>
    {
        public Task ExecuteResult { get; set; } = Task.CompletedTask;
        public Task FaultResult { get; set; } = Task.CompletedTask;
        public int ExecuteCalls { get; private set; }
        public int FaultCalls { get; private set; }
        public int AcceptCalls { get; private set; }
        public int ProbeCalls { get; private set; }
        public MarkerException? AcceptFailure { get; set; }
        public MarkerException? ProbeFailure { get; set; }
        public object? LastExecuteContext { get; private set; }
        public object? LastExecuteNext { get; private set; }
        public object? LastFaultContext { get; private set; }
        public object? LastFaultNext { get; private set; }
        public int TotalCalls => ExecuteCalls + FaultCalls + AcceptCalls + ProbeCalls;

        public Task ExecuteAsync(IBehaviorContext<TestSaga, Message> context, IBehavior<TestSaga, Message> next)
        {
            ExecuteCalls++;
            LastExecuteContext = context;
            LastExecuteNext = next;
            return ExecuteResult;
        }

        public Task FaultedAsync<TException>(IBehaviorExceptionContext<TestSaga, Message, TException> context,
            IBehavior<TestSaga, Message> next) where TException : Exception
        {
            FaultCalls++;
            LastFaultContext = context;
            LastFaultNext = next;
            return FaultResult;
        }

        public void Accept(IStateMachineVisitor visitor)
        {
            AcceptCalls++;
            visitor.Visit(this);
            if (AcceptFailure is not null)
                throw AcceptFailure;
        }

        public void Probe(ProbeContext context)
        {
            ProbeCalls++;
            if (ProbeFailure is not null)
                throw ProbeFailure;

            context.Add("typed", "activity");
        }
    }

    sealed class RecordingSynchronizationContext : SynchronizationContext
    {
        int _postCount;

        public int PostCount => Volatile.Read(ref _postCount);

        public override void Post(SendOrPostCallback callback, object? state)
        {
            Interlocked.Increment(ref _postCount);
            callback(state);
        }
    }

    sealed class RecordingVisitor : IStateMachineVisitor
    {
        public List<object> Visited { get; } = [];
        public MarkerException? Failure { get; init; }

        public void Visit(IStateMachineActivity activity)
        {
            Visited.Add(activity);
            if (Failure is not null)
                throw Failure;
        }

        public void Visit(IStateMachineActivity activity, Action<IStateMachineActivity> next)
        {
            Visited.Add(activity);
            if (Failure is not null)
                throw Failure;
            next(activity);
        }

        public void Visit<T>(IBehavior<T> behavior) where T : class, ISagaStateMachineInstance
        {
            Visited.Add(behavior);
        }

        public void Visit<T>(IBehavior<T> behavior, Action<IBehavior<T>> next) where T : class, ISagaStateMachineInstance
        {
            Visited.Add(behavior);
            next(behavior);
        }

        public void Visit<T, TMessage>(IBehavior<T, TMessage> behavior)
            where T : class, ISagaStateMachineInstance where TMessage : class => Visited.Add(behavior);

        public void Visit<T, TMessage>(IBehavior<T, TMessage> behavior, Action<IBehavior<T, TMessage>> next)
            where T : class, ISagaStateMachineInstance where TMessage : class
        {
            Visited.Add(behavior);
            next(behavior);
        }

        public void Visit(IState state, Action<IState> next) => throw Unexpected();
        public void Visit(IEvent @event, Action<IEvent> next) => throw Unexpected();
        public void Visit<TMessage>(IEvent<TMessage> @event, Action<IEvent<TMessage>> next) where TMessage : class => throw Unexpected();
        public void Visit(IStateMachineExceptionActivity activity, Action<IStateMachineExceptionActivity> next) => throw Unexpected();

        static Exception Unexpected() => new Xunit.Sdk.XunitException("Unexpected visitor overload.");
    }

    sealed class RecordingProbeContext : ProbeContext
    {
        readonly List<string> _calls;
        readonly string _prefix;
        readonly MarkerException? _scopeFailure;

        public RecordingProbeContext(MarkerException? scopeFailure = null)
            : this([], string.Empty, scopeFailure)
        {
        }

        RecordingProbeContext(List<string> calls, string prefix, MarkerException? scopeFailure)
        {
            _calls = calls;
            _prefix = prefix;
            _scopeFailure = scopeFailure;
        }

        public IReadOnlyList<string> Calls => _calls;
        public CancellationToken CancellationToken => default;
        public void Add(string key, string? value) => _calls.Add($"{_prefix}{key}:{value}");
        public void Add(string key, object? value) => _calls.Add($"{_prefix}{key}:{value}");
        public void Set(object values) => throw Unexpected();
        public void Set(IEnumerable<KeyValuePair<string, object?>> values) => throw Unexpected();

        public ProbeContext CreateScope(string key)
        {
            if (_scopeFailure is not null)
                throw _scopeFailure;
            _calls.Add($"scope:{key}");
            return new RecordingProbeContext(_calls, $"{_prefix}{key}/", null);
        }

        static Exception Unexpected() => new Xunit.Sdk.XunitException("Unexpected probe operation.");
    }
}
