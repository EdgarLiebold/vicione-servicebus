using System.Collections.Concurrent;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineSignalSendCallbackIntegrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-TRANSPORT", "signal-send-awaits-message-and-suppresses-failed-delivery")]
    public async Task SignalSend_AwaitsMessageBeforeCallbackAndNeverSendsAFailedMessageAsync(bool failMessage)
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;
        CancellationToken token = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"signal-send-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        string destinationName = $"signal-target-{NewId.NextGuid():N}";
        Uri destination = new(harness.BaseAddress, destinationName);
        Guid sagaId = NewId.NextGuid();
        Guid wireId = NewId.NextGuid();
        var message = new Dispatch(sagaId, "payload");
        var pendingMessage = new TaskCompletionSource<Dispatch>(TaskCreationOptions.RunContinuationsAsynchronously);
        var signalEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var delivered = new TaskCompletionSource<ConsumeContext<Dispatch>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var trace = new ConcurrentQueue<string>();
        var machine = new SignalMachine(destination, pendingMessage.Task, signalEntered, trace, wireId);
        harness.InMemoryBusConfiguring += bus => bus.ReceiveEndpoint(destinationName,
            endpoint => endpoint.Handler<Dispatch>(context =>
            {
                delivered.TrySetResult(context);
                return Task.CompletedTask;
            }));
        ISagaStateMachineTestHarness<SignalMachine, SignalState> sagas =
            harness.AddSagaStateMachine<SignalMachine, SignalState>(machine);

        await harness.StartAsync(token).WaitAsync(timeout, token);
        try
        {
            Task<IConsumedMessage<Start>> consumed = sagas.Consumed.SelectAsync<Start>(token)
                .FirstObservedAsync(cancellationToken: token);
            Task start = harness.InputQueueSendEndpoint.SendAsync(new Start(sagaId), token);
            await signalEntered.Task.WaitAsync(timeout, token);
            Assert.Equal(["signal"], trace);
            Assert.False(delivered.Task.IsCompleted);
            Assert.Empty(harness.Sent.Snapshot<Dispatch>());

            if (failMessage)
            {
                pendingMessage.SetException(new MessageCreationFailure("dispatch unavailable"));
                IConsumedMessage<Start> failed = await consumed.WaitAsync(timeout, token);
                EventExecutionException execution = Assert.IsType<EventExecutionException>(failed.Exception);
                Exception cause = execution;
                while (cause is EventExecutionException wrapped)
                    cause = Assert.IsAssignableFrom<Exception>(wrapped.InnerException);
                MessageCreationFailure error = Assert.IsType<MessageCreationFailure>(cause);
                Assert.Equal("dispatch unavailable", error.Message);
                Assert.Equal(["signal"], trace);
                Assert.False(delivered.Task.IsCompleted);
                Assert.Empty(harness.Sent.Snapshot<Dispatch>());
                SignalState persisted = Assert.IsType<SignalState>(sagas.Sagas.FindById(sagaId));
                Assert.Equal(machine.Ready.Name, persisted.CurrentState);
            }
            else
            {
                pendingMessage.SetResult(message);
                ConsumeContext<Dispatch> delivery = await delivered.Task.WaitAsync(timeout, token);
                IConsumedMessage<Start> successful = await consumed.WaitAsync(timeout, token);
                Assert.Null(successful.Exception);
                SignalState persisted = Assert.IsType<SignalState>(sagas.Sagas.FindById(sagaId));
                Assert.Equal(machine.Succeeded.Name, persisted.CurrentState);
                Assert.Equal(message, delivery.Message);
                Assert.Equal(wireId, delivery.CorrelationId);
                Assert.Equal(sagaId, delivery.InitiatorId);
                Assert.Equal(destination, delivery.DestinationAddress);
                Assert.Equal($"signal:{sagaId:N}", delivery.Headers.Get<string>("signal-owner"));
                Assert.Equal(["signal", "callback", "success"], trace);
                Assert.Single(harness.Sent.Snapshot<Dispatch>());
            }

            await start.WaitAsync(timeout, token);
        }
        finally
        {
            pendingMessage.TrySetCanceled(token);
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    public sealed record Start(Guid CorrelationId) : ICorrelatedBy<Guid>;
    public sealed record Dispatch(Guid SagaId, string Body);
    public sealed class MessageCreationFailure(string message) : Exception(message);

    public sealed class SignalState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = string.Empty;
    }

    public sealed class SignalMachine : ViciOneServiceBusStateMachine<SignalState>
    {
        public SignalMachine(Uri destination, Task<Dispatch> pendingMessage, TaskCompletionSource signalEntered,
            ConcurrentQueue<string> trace, Guid wireId)
        {
            InstanceState(state => state.CurrentState);
            Initially(When(Started)
                .TransitionTo(Ready)
                .ThenAwaited(context => context.RaiseAsync(Signal)));
            During(Ready, When(Signal)
                .Then(_ =>
                {
                    trace.Enqueue("signal");
                    signalEntered.TrySetResult();
                })
                .SendAwaited(destination, pendingMessage, (context, send) =>
                {
                    trace.Enqueue("callback");
                    send.CorrelationId = wireId;
                    send.InitiatorId = context.Saga.CorrelationId;
                    send.Headers.Set("signal-owner", $"signal:{context.Saga.CorrelationId:N}");
                })
                .Then(_ => trace.Enqueue("success"))
                .TransitionTo(Succeeded));
        }

        public IEvent<Start> Started { get; } = null!;
        public IEvent Signal { get; } = null!;
        public IState Ready { get; } = null!;
        public IState Succeeded { get; } = null!;
    }
}
