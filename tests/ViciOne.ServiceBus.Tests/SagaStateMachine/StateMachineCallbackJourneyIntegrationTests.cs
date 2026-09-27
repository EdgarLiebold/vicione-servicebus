using System.Collections.Concurrent;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineCallbackJourneyIntegrationTests
{
    [Theory]
    [InlineData(Journey.Immediate)]
    [InlineData(Journey.Deferred)]
    [InlineData(Journey.FactoryFailure)]
    [InlineData(Journey.CallbackFailure)]
    [InlineData(Journey.DeferredSend)]
    [InlineData(Journey.SendFailure)]
    [InlineData(Journey.RecoveryCallbackFailure)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-TRANSPORT", "callback-ownership-pending-factory-and-recovery-delivery")]
    public async Task CallbackJourney_PreservesOwnershipAndCompletesOnlyTheSelectedOutcomeAsync(Journey journey)
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;
        CancellationToken token = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"callback-journey-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        string firstName = $"callback-first-{NewId.NextGuid():N}";
        string secondName = $"callback-second-{NewId.NextGuid():N}";
        Uri firstAddress = new(harness.BaseAddress, firstName);
        Uri secondAddress = new(harness.BaseAddress, secondName);
        Guid firstId = NewId.NextGuid();
        Guid secondId = NewId.NextGuid();
        Guid firstWireId = NewId.NextGuid();
        Guid secondWireId = NewId.NextGuid();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sendEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var trace = new ConcurrentQueue<(Guid Id, string Stage)>();
        var deliveries = new ConcurrentQueue<(string Endpoint, ConsumeContext<Dispatch> Context)>();
        var recoveries = new ConcurrentQueue<ConsumeContext<Recovery>>();
        var firstDelivered = new TaskCompletionSource<ConsumeContext<Dispatch>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondDelivered = new TaskCompletionSource<ConsumeContext<Dispatch>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var recovered = new TaskCompletionSource<ConsumeContext<Recovery>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new ExpectedDispatchFailure($"dispatch rejected: {journey}");
        var recoveryFailure = new ExpectedRecoveryFailure("recovery callback rejected alpha-event");
        var caught = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var machine = new CallbackMachine(firstId, journey, entered, release, trace, failure, recoveryFailure, caught, timeout);
        harness.InMemoryBusConfiguring += bus =>
        {
            bus.ConfigureSend(send => send.UseExecuteAwaited(async context =>
            {
                if (journey is Journey.DeferredSend or Journey.SendFailure
                    && context is SendContext<Dispatch> dispatch && dispatch.Message.SagaId == firstId)
                {
                    trace.Enqueue((firstId, "send-enter"));
                    sendEntered.TrySetResult();
                    await release.Task.WaitAsync(timeout, context.CancellationToken);
                    if (journey == Journey.SendFailure)
                        throw failure;
                    trace.Enqueue((firstId, "send-release"));
                }
            }));
            bus.ReceiveEndpoint(firstName, endpoint =>
            {
                endpoint.Handler<Dispatch>(context =>
                {
                    deliveries.Enqueue((firstName, context));
                    firstDelivered.TrySetResult(context);
                    return Task.CompletedTask;
                });
                endpoint.Handler<Recovery>(context =>
                {
                    recoveries.Enqueue(context);
                    recovered.TrySetResult(context);
                    return Task.CompletedTask;
                });
            });
            bus.ReceiveEndpoint(secondName, endpoint => endpoint.Handler<Dispatch>(context =>
            {
                deliveries.Enqueue((secondName, context));
                secondDelivered.TrySetResult(context);
                return Task.CompletedTask;
            }));
        };
        ISagaStateMachineTestHarness<CallbackMachine, CallbackState> sagas = harness.AddSagaStateMachine<CallbackMachine, CallbackState>(machine);

        await harness.StartAsync(token).WaitAsync(timeout, token);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new Prime(firstId, "alpha-owner", firstAddress), token);
            await harness.InputQueueSendEndpoint.SendAsync(new Prime(secondId, "beta-owner", secondAddress), token);
            Assert.Equal(firstId, await sagas.WaitForSagaInStateAsync(firstId, machine.Ready, timeout, token));
            Assert.Equal(secondId, await sagas.WaitForSagaInStateAsync(secondId, machine.Ready, timeout, token));

            await harness.InputQueueSendEndpoint.SendAsync(new Start(firstId, "alpha-event", firstWireId), token);
            await entered.Task.WaitAsync(timeout, token);
            if (journey is Journey.DeferredSend or Journey.SendFailure)
                await sendEntered.Task.WaitAsync(timeout, token);
            await harness.InputQueueSendEndpoint.SendAsync(new Start(secondId, "beta-event", secondWireId), token);
            ConsumeContext<Dispatch> control = await secondDelivered.Task.WaitAsync(timeout, token);
            Assert.Equal(secondId, await sagas.WaitForSagaInStateAsync(secondId, machine.Succeeded, timeout, token));
            AssertDispatch(control, secondId, secondWireId, "beta-owner", "beta-event", secondAddress, harness.InputQueueAddress);

            if (journey != Journey.Immediate)
            {
                string[] pendingTrace = journey is Journey.DeferredSend or Journey.SendFailure
                    ? ["factory-enter", "factory-return", "send-enter"]
                    : ["factory-enter"];
                Assert.Equal(pendingTrace, trace.Where(item => item.Id == firstId).Select(item => item.Stage));
                Assert.False(firstDelivered.Task.IsCompleted);
                Assert.False(recovered.Task.IsCompleted);
                CallbackState pending = Assert.IsType<CallbackState>(sagas.Sagas.FindById(firstId));
                Assert.Equal(machine.Ready.Name, pending.CurrentState);
                Assert.Equal(0, pending.SuccessCount);
                Assert.Equal(0, pending.RecoveryCount);
            }
            release.TrySetResult();

            if (journey == Journey.RecoveryCallbackFailure)
            {
                IPublishedMessage<Fault<Start>> published = await harness.Published.SelectAsync<Fault<Start>>(token)
                    .FirstObservedAsync(cancellationToken: token).WaitAsync(timeout, token);
                Assert.Same(failure, await caught.Task.WaitAsync(timeout, token));
                Assert.Equal(new Start(firstId, "alpha-event", firstWireId), published.Context.Message.Message);
                ExceptionInfo error = Assert.Single(published.Context.Message.Exceptions);
                Assert.Equal(TypeCache<ExpectedRecoveryFailure>.ShortName, error.ExceptionType);
                Assert.Equal(recoveryFailure.Message, error.Message);
                Assert.Null(error.InnerException);
            }
            else if (journey is Journey.FactoryFailure or Journey.CallbackFailure or Journey.SendFailure)
            {
                ConsumeContext<Recovery> recovery = await recovered.Task.WaitAsync(timeout, token);
                Assert.Same(failure, await caught.Task.WaitAsync(timeout, token));
                Assert.Equal(firstId, await sagas.WaitForSagaInStateAsync(firstId, machine.Recovered, timeout, token));
                Assert.Equal(new Recovery(firstId, "alpha-owner", "alpha-event", failure.Message), recovery.Message);
                Assert.Equal(firstWireId, recovery.CorrelationId);
                Assert.Equal(firstId, recovery.InitiatorId);
                Assert.Equal(firstAddress, recovery.DestinationAddress);
                Assert.Equal(harness.InputQueueAddress, recovery.SourceAddress);
                Assert.Equal("alpha-owner/alpha-event", recovery.Headers.Get<string>("callback-owner-event"));
                Assert.Equal(failure.Message, recovery.Headers.Get<string>("callback-failure"));
                Assert.Equal(typeof(ExpectedDispatchFailure).FullName, recovery.Headers.Get<string>("callback-failure-type"));
            }
            else
            {
                ConsumeContext<Dispatch> active = await firstDelivered.Task.WaitAsync(timeout, token);
                AssertDispatch(active, firstId, firstWireId, "alpha-owner", "alpha-event", firstAddress, harness.InputQueueAddress);
                Assert.Equal(firstId, await sagas.WaitForSagaInStateAsync(firstId, machine.Succeeded, timeout, token));
            }
        }
        finally
        {
            release.TrySetResult();
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        bool recoveryFailed = journey == Journey.RecoveryCallbackFailure;
        bool failed = journey is Journey.FactoryFailure or Journey.CallbackFailure or Journey.SendFailure || recoveryFailed;
        CallbackState first = Assert.IsType<CallbackState>(sagas.Sagas.FindById(firstId));
        CallbackState second = Assert.IsType<CallbackState>(sagas.Sagas.FindById(secondId));
        Assert.Equal(failed ? 0 : 1, first.SuccessCount);
        Assert.Equal(failed && !recoveryFailed ? 1 : 0, first.RecoveryCount);
        Assert.Equal(recoveryFailed ? machine.Ready.Name : failed ? machine.Recovered.Name : machine.Succeeded.Name, first.CurrentState);
        Assert.Equal(1, second.SuccessCount);
        Assert.Equal(0, second.RecoveryCount);
        Assert.Equal(new[] { "factory-enter", "factory-return", "callback", "success" },
            trace.Where(item => item.Id == secondId).Select(item => item.Stage));
        string[] expected = journey switch
        {
            Journey.FactoryFailure => ["factory-enter", "caught", "recovery-callback", "recovered"],
            Journey.CallbackFailure => ["factory-enter", "factory-return", "callback", "caught", "recovery-callback", "recovered"],
            Journey.SendFailure => ["factory-enter", "factory-return", "send-enter", "caught", "recovery-callback", "recovered"],
            Journey.DeferredSend => ["factory-enter", "factory-return", "send-enter", "send-release", "callback", "success"],
            Journey.RecoveryCallbackFailure => ["factory-enter", "factory-return", "callback", "caught", "recovery-callback"],
            _ => ["factory-enter", "factory-return", "callback", "success"],
        };
        Assert.Equal(expected, trace.Where(item => item.Id == firstId).Select(item => item.Stage));
        Assert.Single(deliveries, item => item.Endpoint == secondName && item.Context.Message.SagaId == secondId);
        Assert.Equal(failed ? 1 : 2, deliveries.Count);
        Assert.Equal(failed ? 1 : 2, harness.Sent.Snapshot<Dispatch>().Count());
        Assert.Equal(failed && !recoveryFailed ? 1 : 0, recoveries.Count);
        Assert.Equal(failed && !recoveryFailed ? 1 : 0, harness.Sent.Snapshot<Recovery>().Count());
        Assert.Equal(recoveryFailed ? 1 : 0, harness.Published.Snapshot<Fault<Start>>().Count());
        Assert.All(sagas.Consumed.Snapshot<Start>(), message =>
        {
            if (recoveryFailed && message.Context.Message.CorrelationId == firstId)
            {
                Assert.Same(recoveryFailure, message.Exception);
            }
            else
                Assert.Null(message.Exception);
        });
        Assert.Equal(2, sagas.Consumed.Snapshot<Start>().Count());
    }

    private static void AssertDispatch(ConsumeContext<Dispatch> actual, Guid sagaId, Guid wireId,
        string owner, string value, Uri destination, Uri source)
    {
        Assert.Equal(new Dispatch(sagaId, owner, value), actual.Message);
        Assert.Equal(wireId, actual.CorrelationId);
        Assert.Equal(sagaId, actual.InitiatorId);
        Assert.Equal(destination, actual.DestinationAddress);
        Assert.Equal(source, actual.SourceAddress);
        Assert.Equal(owner + "/" + value, actual.Headers.Get<string>("callback-owner-event"));
        Assert.Equal("normal", actual.Headers.Get<string>("callback-kind"));
    }

    public enum Journey { Immediate, Deferred, FactoryFailure, CallbackFailure, DeferredSend, SendFailure, RecoveryCallbackFailure }
    public sealed record Prime(Guid CorrelationId, string Owner, Uri Destination) : ICorrelatedBy<Guid>;
    public sealed record Start(Guid CorrelationId, string Value, Guid WireId) : ICorrelatedBy<Guid>;
    public sealed record Dispatch(Guid SagaId, string Owner, string Value);
    public sealed record Recovery(Guid SagaId, string Owner, string Value, string Error);
    public sealed class ExpectedDispatchFailure(string message) : Exception(message);
    public sealed class ExpectedRecoveryFailure(string message) : Exception(message);

    public sealed class CallbackState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public Uri Destination { get; set; } = null!;
        public int SuccessCount { get; set; }
        public int RecoveryCount { get; set; }
    }

    public sealed class CallbackMachine : ViciOneServiceBusStateMachine<CallbackState>
    {
        public CallbackMachine(Guid activeId, Journey journey, TaskCompletionSource entered, TaskCompletionSource release,
            ConcurrentQueue<(Guid Id, string Stage)> trace, ExpectedDispatchFailure failure, ExpectedRecoveryFailure recoveryFailure,
            TaskCompletionSource<Exception> caught, TimeSpan timeout)
        {
            InstanceState(state => state.CurrentState);
            Initially(When(Primed).Then(context =>
            {
                context.Saga.Owner = context.Message.Owner;
                context.Saga.Destination = context.Message.Destination;
            }).TransitionTo(Ready));
            During(Ready, When(Started)
                .SendAwaited(context => context.Saga.Destination, async context =>
                {
                    trace.Enqueue((context.Saga.CorrelationId, "factory-enter"));
                    if (context.Saga.CorrelationId == activeId)
                    {
                        entered.TrySetResult();
                        if (journey is Journey.Deferred or Journey.FactoryFailure or Journey.CallbackFailure or Journey.RecoveryCallbackFailure)
                            await release.Task.WaitAsync(timeout, context.CancellationToken);
                        if (journey == Journey.FactoryFailure)
                            throw failure;
                    }
                    trace.Enqueue((context.Saga.CorrelationId, "factory-return"));
                    return new Dispatch(context.Saga.CorrelationId, context.Saga.Owner, context.Message.Value);
                }, (context, send) =>
                {
                    trace.Enqueue((context.Saga.CorrelationId, "callback"));
                    send.CorrelationId = context.Message.WireId;
                    send.Headers.Set("callback-owner-event", context.Saga.Owner + "/" + context.Message.Value);
                    send.Headers.Set("callback-kind", "normal");
                    if (context.Saga.CorrelationId == activeId && journey is Journey.CallbackFailure or Journey.RecoveryCallbackFailure)
                        throw failure;
                })
                .Then(context =>
                {
                    context.Saga.SuccessCount++;
                    trace.Enqueue((context.Saga.CorrelationId, "success"));
                })
                .TransitionTo(Succeeded)
                .Catch<ExpectedDispatchFailure>(exception => exception
                    .Then(context =>
                    {
                        caught.TrySetResult(context.Exception);
                        trace.Enqueue((context.Saga.CorrelationId, "caught"));
                    })
                    .Send(context => context.Saga.Destination,
                        context => new Recovery(context.Saga.CorrelationId, context.Saga.Owner, context.Message.Value, context.Exception.Message),
                        (context, send) =>
                        {
                            trace.Enqueue((context.Saga.CorrelationId, "recovery-callback"));
                            send.CorrelationId = context.Message.WireId;
                            send.Headers.Set("callback-owner-event", context.Saga.Owner + "/" + context.Message.Value);
                            send.Headers.Set("callback-failure", context.Exception.Message);
                            send.Headers.Set("callback-failure-type", context.Exception.GetType().FullName);
                            if (journey == Journey.RecoveryCallbackFailure)
                                throw recoveryFailure;
                        })
                    .Then(context =>
                    {
                        context.Saga.RecoveryCount++;
                        trace.Enqueue((context.Saga.CorrelationId, "recovered"));
                    })
                    .TransitionTo(Recovered)));
        }

        public IEvent<Prime> Primed { get; } = null!;
        public IEvent<Start> Started { get; } = null!;
        public IState Ready { get; } = null!;
        public IState Succeeded { get; } = null!;
        public IState Recovered { get; } = null!;
    }
}
