using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Sagas.Configuration;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class ContainerStateMachineScopeTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-STATE-MACHINE", "activity-consume-and-three-saga-pipe-scope-layers")]
    public async Task ContainerStateMachine_UsesOneScopedOwnerAndExposesTheExpectedThreePipeLayersAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new StateMachineScopeObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddScoped<ScopeMarker>()
            .AddScoped<PublishStartedActivity>()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                ISagaRegistrationConfigurator registration = configuration
                    .AddSagaStateMachine<ContainerScopeMachine, ContainerScopeState>()
                    .InMemoryRepository();
                registration.ExcludeFromConfigureEndpoints();
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.UseConsumeFilter(typeof(StateMachineConsumeScopeFilter<>), context);
                    bus.ReceiveEndpoint("container-state-machine-scope", endpoint =>
                    {
                        endpoint.ConfigureSaga<ContainerScopeState>(context, saga =>
                        {
                            saga.Message<ContainerScopeStart>(message =>
                                message.UseFilter(new MessageLayerFilter(observation)));
                            saga.UseFilter(new SagaLayerFilter(observation));
                            saga.SagaMessage<ContainerScopeStart>(message =>
                                message.UseFilter(new SagaMessageLayerFilter(observation)));
                        });
                    });
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid correlationId = NewId.NextGuid();
            ISendEndpoint endpoint = await harness.Bus
                .GetSendEndpointAsync(new Uri("queue:container-state-machine-scope"), TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
            var start = new ContainerScopeStart(correlationId, "scope-key");

            await endpoint.SendAsync(start, cancellationToken);
            IPublishedMessage<ContainerScopeStarted> started = await harness.Published
                .SelectAsync<ContainerScopeStarted>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
            StateMachineScopeSnapshot snapshot = await observation.Completed.Task
                .WaitAsync(timeout, cancellationToken);

            Assert.Equal(new ContainerScopeStarted(correlationId, "scope-key"), started.Context.Message);
            Assert.Same(snapshot.ConsumeScope, snapshot.ActivityScope);
            Assert.Same(snapshot.ConsumeScope, snapshot.SagaScope);
            Assert.Same(snapshot.ConsumeScope, snapshot.SagaMessageScope);
            Assert.False(snapshot.MessageLayerHadServiceProvider);
            Assert.Equal(1, snapshot.ConsumeCount);
            Assert.Equal(1, snapshot.ActivityCount);
            Assert.Equal(1, snapshot.MessageLayerCount);
            Assert.Equal(1, snapshot.SagaLayerCount);
            Assert.Equal(1, snapshot.SagaMessageLayerCount);

            await endpoint.SendAsync(new ContainerScopeUpdate("scope-key"), cancellationToken);
            IPublishedMessage<ContainerScopeUpdated> updated = await harness.Published
                .SelectAsync<ContainerScopeUpdated>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

            Assert.Equal(new ContainerScopeUpdated(correlationId, "scope-key"), updated.Context.Message);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(harness.Published.Snapshot<ContainerScopeStarted>());
        Assert.Single(harness.Published.Snapshot<ContainerScopeUpdated>());
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions().OperationTimeout!.Value;


    public sealed record ContainerScopeStart(Guid CorrelationId, string Key) : ICorrelatedBy<Guid>;

    public sealed record ContainerScopeUpdate(string Key);

    public sealed record ContainerScopeStarted(Guid CorrelationId, string Key);

    public sealed record ContainerScopeUpdated(Guid CorrelationId, string Key);

    public sealed class ScopeMarker;

    public sealed class ContainerScopeState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public string Key { get; set; } = string.Empty;
    }

    public sealed class ContainerScopeMachine : ViciOneServiceBusStateMachine<ContainerScopeState>
    {
        public ContainerScopeMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Event(() => Updated, configuration => configuration.CorrelateBy(
                instance => instance.Key,
                context => context.Message.Key));
            Initially(
                When(Started)
                    .Then(context => context.Saga.Key = context.Message.Key)
                    .Activity(activity => activity.OfType<PublishStartedActivity>())
                    .TransitionTo(Running));
            During(
                Running,
                When(Updated)
                    .Publish(context => new ContainerScopeUpdated(
                        context.Saga.CorrelationId,
                        context.Saga.Key))
                    .Finalize());
            SetCompletedWhenFinalized();
        }

        public IState Running { get; } = null!;

        public IEvent<ContainerScopeStart> Started { get; } = null!;

        public IEvent<ContainerScopeUpdate> Updated { get; } = null!;
    }

    public sealed class StateMachineScopeObservation
    {
        private readonly object _lock = new();
        private ScopeMarker? _consumeScope;
        private ScopeMarker? _activityScope;
        private ScopeMarker? _sagaScope;
        private ScopeMarker? _sagaMessageScope;
        private bool _messageLayerHadServiceProvider;
        private int _consumeCount;
        private int _activityCount;
        private int _messageLayerCount;
        private int _sagaLayerCount;
        private int _sagaMessageLayerCount;

        public TaskCompletionSource<StateMachineScopeSnapshot> Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void RecordConsume(ScopeMarker marker)
        {
            lock (_lock)
            {
                _consumeScope = marker;
                _consumeCount++;
                TryComplete();
            }
        }

        public void RecordActivity(ScopeMarker marker)
        {
            lock (_lock)
            {
                _activityScope = marker;
                _activityCount++;
                TryComplete();
            }
        }

        public void RecordMessageLayer(bool hadServiceProvider)
        {
            lock (_lock)
            {
                _messageLayerHadServiceProvider = hadServiceProvider;
                _messageLayerCount++;
                TryComplete();
            }
        }

        public void RecordSagaLayer(ScopeMarker marker)
        {
            lock (_lock)
            {
                _sagaScope = marker;
                _sagaLayerCount++;
                TryComplete();
            }
        }

        public void RecordSagaMessageLayer(ScopeMarker marker)
        {
            lock (_lock)
            {
                _sagaMessageScope = marker;
                _sagaMessageLayerCount++;
                TryComplete();
            }
        }

        private void TryComplete()
        {
            if (_consumeScope is null || _activityScope is null || _sagaScope is null || _sagaMessageScope is null
                || _messageLayerCount == 0)
                return;

            Completed.TrySetResult(new StateMachineScopeSnapshot(
                _consumeScope,
                _activityScope,
                _sagaScope,
                _sagaMessageScope,
                _messageLayerHadServiceProvider,
                _consumeCount,
                _activityCount,
                _messageLayerCount,
                _sagaLayerCount,
                _sagaMessageLayerCount));
        }
    }

    public sealed record StateMachineScopeSnapshot(
        ScopeMarker ConsumeScope,
        ScopeMarker ActivityScope,
        ScopeMarker SagaScope,
        ScopeMarker SagaMessageScope,
        bool MessageLayerHadServiceProvider,
        int ConsumeCount,
        int ActivityCount,
        int MessageLayerCount,
        int SagaLayerCount,
        int SagaMessageLayerCount);

    public sealed class StateMachineConsumeScopeFilter<T>(
        ScopeMarker marker,
        StateMachineScopeObservation observation) : IFilter<ConsumeContext<T>>
        where T : class
    {
        public async Task SendAsync(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
        {
            if (context.Message is ContainerScopeStart)
                observation.RecordConsume(marker);

            await next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("containerStateMachineConsumeScope");
    }

    public sealed class PublishStartedActivity(
        ScopeMarker marker,
        StateMachineScopeObservation observation) :
        IStateMachineActivity<ContainerScopeState, ContainerScopeStart>
    {
        public async Task ExecuteAsync(
            IBehaviorContext<ContainerScopeState, ContainerScopeStart> context,
            IBehavior<ContainerScopeState, ContainerScopeStart> next)
        {
            observation.RecordActivity(marker);
            await context.PublishAsync(
                new ContainerScopeStarted(context.Saga.CorrelationId, context.Saga.Key),
                context.CancellationToken);
            await next.ExecuteAsync(context);
        }

        public Task FaultedAsync<TException>(
            IBehaviorExceptionContext<ContainerScopeState, ContainerScopeStart, TException> context,
            IBehavior<ContainerScopeState, ContainerScopeStart> next)
            where TException : Exception => next.FaultedAsync(context);

        public void Probe(ProbeContext context) => context.CreateScope("publishContainerScopeStarted");

        public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this);
    }

    private sealed class MessageLayerFilter(StateMachineScopeObservation observation) :
        IFilter<ConsumeContext<ContainerScopeStart>>
    {
        public Task SendAsync(
            ConsumeContext<ContainerScopeStart> context,
            IPipe<ConsumeContext<ContainerScopeStart>> next)
        {
            observation.RecordMessageLayer(context.TryGetPayload(out IServiceProvider? _));
            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("messageLayer");
    }

    private sealed class SagaLayerFilter(StateMachineScopeObservation observation) :
        IFilter<SagaConsumeContext<ContainerScopeState>>
    {
        public Task SendAsync(
            SagaConsumeContext<ContainerScopeState> context,
            IPipe<SagaConsumeContext<ContainerScopeState>> next)
        {
            Assert.True(context.TryGetPayload(out IServiceProvider? serviceProvider));
            observation.RecordSagaLayer(serviceProvider!.GetRequiredService<ScopeMarker>());
            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("sagaLayer");
    }

    private sealed class SagaMessageLayerFilter(StateMachineScopeObservation observation) :
        IFilter<SagaConsumeContext<ContainerScopeState, ContainerScopeStart>>
    {
        public Task SendAsync(
            SagaConsumeContext<ContainerScopeState, ContainerScopeStart> context,
            IPipe<SagaConsumeContext<ContainerScopeState, ContainerScopeStart>> next)
        {
            Assert.True(context.TryGetPayload(out IServiceProvider? serviceProvider));
            observation.RecordSagaMessageLayer(serviceProvider!.GetRequiredService<ScopeMarker>());
            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("sagaMessageLayer");
    }
}
