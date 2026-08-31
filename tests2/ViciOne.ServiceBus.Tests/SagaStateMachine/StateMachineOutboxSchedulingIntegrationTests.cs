using System.Collections.Concurrent;
using System.Net.Mime;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineOutboxSchedulingIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-OUTBOX", "serializer-failure-retry-continues-scheduled-request-loop")]
    public async Task SerializerFailureDuringScheduledOutboxDelivery_RetriesFromTheCommittedSagaStateAndCompletesOnce()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var failures = new SerializerFailureRecorder();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(failures)
            .AddScoped(typeof(FailingScheduledSendFilter<>))
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.SetEndpointNameFormatter(
                    new KebabCaseEndpointNameFormatter($"scheduled-outbox-{NewId.NextGuid():N}"));
                configuration.AddHandler((ConsumeContext<LoopRequest> context) =>
                    context.RespondAsync(new LoopResponse(context.Message.Count >= 5 ? "Finished" : "Running")));
                configuration.AddSagaStateMachine<ScheduledOutboxMachine, ScheduledOutboxState, ScheduledOutboxDefinition>()
                    .InMemoryRepository();
                configuration.AddConfigureEndpointsCallback((context, _, endpoint) =>
                    endpoint.UseSendFilter(typeof(FailingScheduledSendFilter<>), context));
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.UseDelayedMessageScheduler();
                    bus.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);
        ISagaStateMachineTestHarness<ScheduledOutboxMachine, ScheduledOutboxState> sagaHarness =
            harness.GetSagaStateMachineHarness<ScheduledOutboxMachine, ScheduledOutboxState>();

        try
        {
            Guid correlationId = NewId.NextGuid();
            Task<IPublishedMessage<LoopCompleted>> completed = harness.Published
                .SelectAsync<LoopCompleted>(
                    message => message.Context.Message.CorrelationId == correlationId,
                    cancellationToken)
                .First();

            await harness.Bus.Publish(new StartLoop(correlationId), cancellationToken);
            LoopCompleted result = (await completed.WaitAsync(timeout, cancellationToken)).Context.Message;

            Assert.Equal(new LoopCompleted(correlationId, "Faulted"), result);
            Assert.Equal(1, failures.Count);
            Assert.Equal([2], failures.FailedCounts);
            Assert.Equal(correlationId, await sagaHarness.Exists(correlationId, machine => machine.Failed, timeout));
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(harness.Published.Select<LoopCompleted>(SnapshotOnlyToken()));
        Assert.Empty(harness.Published.Select<Fault<StartLoop>>(SnapshotOnlyToken()));
        Assert.DoesNotContain(
            sagaHarness.Consumed.Select<ScheduledLoopEvent>(SnapshotOnlyToken()),
            received => received.Context.Message.Count == 2);
        IReceivedMessage<LoopRequest>[] requests = harness.Consumed
            .Select<LoopRequest>(SnapshotOnlyToken())
            .ToArray();
        Assert.NotEmpty(requests);
        Assert.All(requests, received => Assert.True(received.Context.Message.Count > 0));
        Assert.Equal(requests.Length, harness.Sent.Select<LoopResponse>(SnapshotOnlyToken()).Count());
    }

    private static CancellationToken SnapshotOnlyToken() => new(canceled: true);

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public sealed record StartLoop(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record ScheduledLoopEvent(Guid CorrelationId, int Count) : CorrelatedBy<Guid>;

    public sealed record LoopRequest(int Count);

    public sealed record LoopResponse(string Status);

    public sealed record LoopCompleted(Guid CorrelationId, string Result);

    public sealed class ScheduledOutboxState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public Guid? ScheduleTokenId { get; set; }

        public int Count { get; set; }
    }

    public sealed class ScheduledOutboxMachine : ViciOneServiceBusStateMachine<ScheduledOutboxState>
    {
        public ScheduledOutboxMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Request(() => HandlerRequest, configuration => configuration.Timeout = TimeSpan.Zero);
            Schedule(
                () => Scheduled,
                instance => instance.ScheduleTokenId,
                configuration =>
                {
                    configuration.Delay = TimeSpan.Zero;
                    configuration.Received = received =>
                    {
                        received.CorrelateById(context => context.Message.CorrelationId);
                        received.ConfigureConsumeTopology = false;
                    };
                });

            Initially(
                When(Started)
                    .Schedule(
                        Scheduled,
                        context => new ScheduledLoopEvent(
                            context.Saga.CorrelationId,
                            context.Saga.Count++))
                    .TransitionTo(Running));

            During(
                Running,
                When(Scheduled.Received)
                    .Schedule(
                        Scheduled,
                        context => new ScheduledLoopEvent(
                            context.Saga.CorrelationId,
                            context.Saga.Count++))
                    .Request(HandlerRequest, context => new LoopRequest(context.Saga.Count))
                    .TransitionTo(Checking));

            During(
                Checking,
                When(Scheduled.Received)
                    .Schedule(
                        Scheduled,
                        context => new ScheduledLoopEvent(
                            context.Saga.CorrelationId,
                            context.Saga.Count++))
                    .Request(HandlerRequest, context => new LoopRequest(context.Saga.Count))
                    .TransitionTo(Suspect));

            During(
                Suspect,
                When(Scheduled.Received)
                    .Publish(context => new LoopCompleted(context.Saga.CorrelationId, "Faulted"))
                    .TransitionTo(Failed));

            During(
                Running,
                Checking,
                Suspect,
                When(HandlerRequest.Completed)
                    .IfElse(
                        context => context.Message.Status == "Running",
                        running => running.TransitionTo(Running),
                        finished => finished.Finalize()));

            WhenEnter(
                Final,
                activity => activity
                    .Unschedule(Scheduled)
                    .Publish(context => new LoopCompleted(context.Saga.CorrelationId, "Success")));
            SetCompletedWhenFinalized();
        }

        public State Running { get; } = null!;

        public State Checking { get; } = null!;

        public State Suspect { get; } = null!;

        public State Failed { get; } = null!;

        public Event<StartLoop> Started { get; } = null!;

        public Schedule<ScheduledOutboxState, ScheduledLoopEvent> Scheduled { get; } = null!;

        public Request<ScheduledOutboxState, LoopRequest, LoopResponse> HandlerRequest { get; } = null!;
    }

    public sealed class ScheduledOutboxDefinition : SagaDefinition<ScheduledOutboxState>
    {
        protected override void ConfigureSaga(
            IReceiveEndpointConfigurator endpointConfigurator,
            ISagaConfigurator<ScheduledOutboxState> sagaConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.UseMessageScope(context);
            endpointConfigurator.UseMessageRetry(retry => retry.Immediate(5));
            endpointConfigurator.UseInMemoryOutbox(context);
        }
    }

    public sealed class FailingScheduledSendFilter<T>(SerializerFailureRecorder failures) : IFilter<SendContext<T>>
        where T : class
    {
        public Task Send(SendContext<T> context, IPipe<SendContext<T>> next)
        {
            if (context.Message is ScheduledLoopEvent { Count: 2 } scheduled)
                context.Serializer = new ThrowingSerializer(failures, scheduled.Count);

            return next.Send(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("scheduledOutboxSerializerFailure");
    }

    public sealed class SerializerFailureRecorder
    {
        private readonly ConcurrentQueue<int> _failedCounts = new();

        public int Count => _failedCounts.Count;

        public int[] FailedCounts => _failedCounts.ToArray();

        public void Record(int count) => _failedCounts.Enqueue(count);
    }

    private sealed class ThrowingSerializer(SerializerFailureRecorder failures, int count) : IMessageSerializer
    {
        public ContentType ContentType { get; } = new("application/vnd.vicione.expected-scheduled-outbox-failure");

        public MessageBody GetMessageBody<T>(SendContext<T> context)
            where T : class
        {
            failures.Record(count);
            throw new ExpectedScheduledOutboxSerializerFailure();
        }
    }

    private sealed class ExpectedScheduledOutboxSerializerFailure : InvalidOperationException;
}
