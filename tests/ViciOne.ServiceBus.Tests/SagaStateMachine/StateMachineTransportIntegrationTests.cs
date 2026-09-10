using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineTransportIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-TRANSPORT", "factory-publish-send-and-context-metadata")]
    public async Task FactoryPublishAndSend_PreserveSagaIdentityPayloadAndTransportMetadataAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("transport-metadata", timeout);
        string observerName = $"transport-observer-{NewId.NextGuid():N}";
        Uri observerAddress = new(harness.BaseAddress, observerName);
        var recorder = new TransportRecorder();
        var machine = new TransportMachine(observerAddress);
        harness.InMemoryBusConfiguring += configurator => configurator.ReceiveEndpoint(
            observerName,
            endpoint =>
            {
                endpoint.Handler<PublishedNotice>(context => recorder.RecordAsync(context));
                endpoint.Handler<SentNotice>(context => recorder.RecordAsync(context));
            });
        ISagaStateMachineTestHarness<TransportMachine, TransportState> sagaHarness =
            harness.StateMachineSaga<TransportState, TransportMachine>(machine);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Guid correlationId = NewId.NextGuid();
            DateTime createdAt = new(2032, 4, 5, 6, 7, 8, DateTimeKind.Utc);
            var start = new TransportStart(correlationId, "owner", createdAt);

            await harness.InputQueueSendEndpoint.SendAsync(start, cancellationToken);
            ConsumeContext<PublishedNotice> published = await recorder.Published.Task.WaitAsync(timeout, cancellationToken);
            ConsumeContext<SentNotice> sent = await recorder.Sent.Task.WaitAsync(timeout, cancellationToken);
            Assert.Equal(correlationId, await sagaHarness.WaitForSagaInStateAsync(correlationId, machine.Running, timeout, TestContext.Current.CancellationToken));

            TransportState? instance = sagaHarness.Sagas.FindById(correlationId);
            Assert.NotNull(instance);
            Assert.Equal(correlationId, instance.CorrelationId);
            Assert.Equal("owner", instance.Name);
            Assert.Equal(createdAt, instance.CreatedAt);
            Assert.Equal(1, instance.StartCount);

            Assert.Equal(new PublishedNotice(correlationId, "owner"), published.Message);
            Assert.Equal(new SentNotice(correlationId, "owner"), sent.Message);
            Assert.Equal(correlationId, published.InitiatorId);
            Assert.Equal(correlationId, sent.InitiatorId);
            Assert.Equal(harness.InputQueueAddress, published.SourceAddress);
            Assert.Equal(harness.InputQueueAddress, sent.SourceAddress);
            Assert.Equal(observerAddress, sent.DestinationAddress);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(harness.Published.Snapshot<PublishedNotice>());
        Assert.Single(harness.Sent.Snapshot<SentNotice>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-DYNAMIC", "runtime-declared-event-lifecycle")]
    public async Task RuntimeDeclaredEvents_CreateRunAndFinalizeTheSameInstanceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("dynamic-events", timeout);
        var machine = new DynamicEventMachine();
        ISagaStateMachineTestHarness<DynamicEventMachine, DynamicEventState> sagaHarness =
            harness.StateMachineSaga<DynamicEventState, DynamicEventMachine>(machine);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Guid serviceId = NewId.NextGuid();
            await harness.Bus.PublishAsync(new DynamicStart(serviceId), cancellationToken);
            Assert.Equal(serviceId, await sagaHarness.WaitForSagaInStateAsync(serviceId, machine.Running, timeout, TestContext.Current.CancellationToken));

            await harness.Bus.PublishAsync(new DynamicStop(serviceId), cancellationToken);
            Assert.Equal(serviceId, await sagaHarness.WaitForSagaInStateAsync(serviceId, machine.Final, timeout, TestContext.Current.CancellationToken));

            DynamicEventState? instance = sagaHarness.Sagas.FindById(serviceId);
            Assert.NotNull(instance);
            Assert.Equal(1, instance.StartCount);
            Assert.Equal(1, instance.StopCount);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(sagaHarness.Consumed.Snapshot<DynamicStart>());
        Assert.Single(sagaHarness.Consumed.Snapshot<DynamicStop>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-TOPOLOGY", "disabled-consume-binding-routes-only-to-independent-handler")]
    public async Task DisabledConsumeTopology_LeavesTheSagaUntouchedWhileAnIndependentHandlerReceivesTheMessageAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("topology-disabled", timeout);
        string observerName = $"topology-observer-{NewId.NextGuid():N}";
        var suspended = new TaskCompletionSource<ConsumeContext<NoTopologySuspend>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var machine = new NoTopologyMachine();
        harness.InMemoryBusConfiguring += configurator => configurator.ReceiveEndpoint(
            observerName,
            endpoint => endpoint.Handler<NoTopologySuspend>(context =>
            {
                suspended.TrySetResult(context);
                return Task.CompletedTask;
            }));
        ISagaStateMachineTestHarness<NoTopologyMachine, NoTopologyState> sagaHarness =
            harness.StateMachineSaga<NoTopologyState, NoTopologyMachine>(machine);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Guid correlationId = NewId.NextGuid();
            await harness.Bus.PublishAsync(new NoTopologyStart(correlationId), cancellationToken);
            Assert.Equal(correlationId, await sagaHarness.WaitForSagaInStateAsync(correlationId, machine.Running, timeout, TestContext.Current.CancellationToken));

            await harness.Bus.PublishAsync(new NoTopologySuspend(correlationId), cancellationToken);
            ConsumeContext<NoTopologySuspend> independent = await suspended.Task.WaitAsync(timeout, cancellationToken);
            Assert.Equal(correlationId, independent.Message.CorrelationId);
            Assert.Equal(correlationId, await sagaHarness.WaitForSagaInStateAsync(correlationId, machine.Running, timeout, TestContext.Current.CancellationToken));
            Assert.Equal(0, Assert.IsType<NoTopologyState>(sagaHarness.Sagas.FindById(correlationId)).SuspendCount);

            await harness.Bus.PublishAsync(new NoTopologyStop(correlationId), cancellationToken);
            Assert.Equal(correlationId, await sagaHarness.WaitForSagaInStateAsync(correlationId, machine.Final, timeout, TestContext.Current.CancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Empty(sagaHarness.Consumed.Snapshot<NoTopologySuspend>());
        Assert.Single(harness.Consumed.Snapshot<NoTopologySuspend>());
        Assert.Single(sagaHarness.Consumed.Snapshot<NoTopologyStart>());
        Assert.Single(sagaHarness.Consumed.Snapshot<NoTopologyStop>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "request-issued-from-enter-hook-completes")]
    public async Task WhenEnterRequest_IsSentCompletedAndAppliedBeforeTheMachineWaitsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("enter-request", timeout);
        string serviceName = $"rule-service-{NewId.NextGuid():N}";
        Uri serviceAddress = new(harness.BaseAddress, serviceName);
        var machine = new EnterRequestMachine(serviceAddress);
        harness.InMemoryBusConfiguring += configurator => configurator.ReceiveEndpoint(
            serviceName,
            endpoint => endpoint.Handler<ExecuteRule>(context =>
                context.RespondAsync(new ExecuteRuleResponse(context.Message.CorrelationId, "executed"))));
        ISagaStateMachineTestHarness<EnterRequestMachine, EnterRequestState> sagaHarness =
            harness.StateMachineSaga<EnterRequestState, EnterRequestMachine>(machine);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Guid correlationId = NewId.NextGuid();
            Task<IConsumedMessage<ExecuteRule>> request = harness.Consumed
                .SelectAsync<ExecuteRule>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Task<IConsumedMessage<ExecuteRuleResponse>> response = sagaHarness.Consumed
                .SelectAsync<ExecuteRuleResponse>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            await harness.InputQueueSendEndpoint.SendAsync(new StartRule(correlationId), cancellationToken);
            Assert.Null((await request.WaitAsync(timeout, cancellationToken)).Exception);
            Assert.Null((await response.WaitAsync(timeout, cancellationToken)).Exception);
            Assert.Equal(correlationId, await sagaHarness.WaitForSagaInStateAsync(correlationId, machine.Waiting, timeout, TestContext.Current.CancellationToken));

            EnterRequestState? instance = sagaHarness.Sagas.FindById(correlationId);
            Assert.NotNull(instance);
            Assert.Equal(1, instance.EnterCount);
            Assert.Equal(1, instance.ResponseCount);
            Assert.Equal("executed", instance.Result);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        ISentMessage<ExecuteRule> sent = Assert.Single(harness.Sent.Snapshot<ExecuteRule>());
        Assert.Equal(serviceAddress, sent.Context.DestinationAddress);
        Assert.Equal(sent.Context.Message.CorrelationId, sent.Context.InitiatorId);
        Assert.Single(sagaHarness.Consumed.Snapshot<ExecuteRuleResponse>());
    }

    private static InMemoryTestHarness CreateHarness(string prefix, TimeSpan timeout) =>
        new($"{prefix}-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };


    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public sealed record TransportStart(Guid CorrelationId, string Name, DateTime Timestamp) : CorrelatedBy<Guid>;

    public sealed record PublishedNotice(Guid CorrelationId, string Name);

    public sealed record SentNotice(Guid CorrelationId, string Name);

    public sealed class TransportState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public int StartCount { get; set; }
    }

    public sealed class TransportMachine : ViciOneServiceBusStateMachine<TransportState>
    {
        public TransportMachine(Uri sendAddress)
        {
            InstanceState(instance => instance.CurrentState);
            Event(() => Started, configuration => configuration.SetSagaFactory(context => new TransportState
            {
                CorrelationId = context.CorrelationId
                    ?? throw new InvalidOperationException("The initiating correlation id was not available."),
                Name = context.Message.Name,
                CreatedAt = context.Message.Timestamp,
            }));
            Initially(
                When(Started)
                    .Then(context => context.Saga.StartCount++)
                    .Publish(context => new PublishedNotice(context.Saga.CorrelationId, context.Saga.Name))
                    .Send(sendAddress, context => new SentNotice(context.Saga.CorrelationId, context.Saga.Name))
                    .TransitionTo(Running));
        }

        public State Running { get; } = null!;

        public Event<TransportStart> Started { get; } = null!;
    }

    private sealed class TransportRecorder
    {
        public TaskCompletionSource<ConsumeContext<PublishedNotice>> Published { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<ConsumeContext<SentNotice>> Sent { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task RecordAsync(ConsumeContext<PublishedNotice> context)
        {
            Published.TrySetResult(context);
            return Task.CompletedTask;
        }

        public Task RecordAsync(ConsumeContext<SentNotice> context)
        {
            Sent.TrySetResult(context);
            return Task.CompletedTask;
        }
    }

    public sealed record DynamicStart(Guid ServiceId);

    public sealed record DynamicStop(Guid ServiceId);

    public sealed class DynamicEventState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public int StartCount { get; set; }

        public int StopCount { get; set; }
    }

    public sealed class DynamicEventMachine : ViciOneServiceBusStateMachine<DynamicEventState>
    {
        public DynamicEventMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Event<DynamicStart> started = Event<DynamicStart>(nameof(DynamicStart));
            Event<DynamicStop> stopped = Event<DynamicStop>(nameof(DynamicStop));
            Initially(
                When(started)
                    .Then(context => context.Saga.StartCount++)
                    .TransitionTo(Running));
            During(
                Running,
                When(stopped)
                    .Then(context => context.Saga.StopCount++)
                    .Finalize());
        }

        public State Running { get; } = null!;
    }

    public sealed record NoTopologyStart(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record NoTopologySuspend(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record NoTopologyStop(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class NoTopologyState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public int SuspendCount { get; set; }
    }

    public sealed class NoTopologyMachine : ViciOneServiceBusStateMachine<NoTopologyState>
    {
        public NoTopologyMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Event(() => Suspended, configuration => configuration.ConfigureConsumeTopology = false);
            Initially(When(Started).TransitionTo(Running));
            During(
                Running,
                When(Suspended)
                    .Then(context => context.Saga.SuspendCount++)
                    .TransitionTo(SuspendedState),
                When(Stopped).Finalize());
        }

        public State Running { get; } = null!;

        public State SuspendedState { get; } = null!;

        public Event<NoTopologyStart> Started { get; } = null!;

        public Event<NoTopologySuspend> Suspended { get; } = null!;

        public Event<NoTopologyStop> Stopped { get; } = null!;
    }

    public sealed record StartRule(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record ExecuteRule(Guid CorrelationId);

    public sealed record ExecuteRuleResponse(Guid CorrelationId, string Result);

    public sealed class EnterRequestState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public Guid? RequestId { get; set; }

        public int EnterCount { get; set; }

        public int ResponseCount { get; set; }

        public string Result { get; set; } = string.Empty;
    }

    public sealed class EnterRequestMachine : ViciOneServiceBusStateMachine<EnterRequestState>
    {
        public EnterRequestMachine(Uri serviceAddress)
        {
            InstanceState(instance => instance.CurrentState);
            Request(() => Execute, instance => instance.RequestId, configuration =>
            {
                configuration.ServiceAddress = serviceAddress;
                configuration.Timeout = TimeSpan.Zero;
            });
            Initially(When(Start).TransitionTo(Starting));
            WhenEnter(
                Starting,
                behavior => behavior
                    .Then(context => context.Saga.EnterCount++)
                    .Request(Execute, context => new ExecuteRule(context.Saga.CorrelationId))
                    .TransitionTo(Executing));
            During(
                Executing,
                When(Execute.Completed)
                    .Then(context =>
                    {
                        context.Saga.ResponseCount++;
                        context.Saga.Result = context.Message.Result;
                    })
                    .TransitionTo(Waiting));
        }

        public State Starting { get; } = null!;

        public State Executing { get; } = null!;

        public State Waiting { get; } = null!;

        public Event<StartRule> Start { get; } = null!;

        public Request<EnterRequestState, ExecuteRule, ExecuteRuleResponse> Execute { get; } = null!;
    }
}
