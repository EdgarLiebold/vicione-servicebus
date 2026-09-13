namespace ViciOne.ServiceBus.Tests.DependencyInjection.ContainerDiscovery;

public sealed class DiscoveryMarker;

public sealed record DiscoveryPing(Guid CorrelationId) : ICorrelatedBy<Guid>;
public sealed record DiscoveryPong(Guid CorrelationId) : ICorrelatedBy<Guid>;
public sealed record PingReceived(Guid CorrelationId) : ICorrelatedBy<Guid>;
public sealed record PingAcknowledged(Guid CorrelationId) : ICorrelatedBy<Guid>;
public sealed record PingCompleted(Guid CorrelationId) : ICorrelatedBy<Guid>;
public sealed record PingArguments(Guid CorrelationId);
public sealed record PingLog(Guid CorrelationId);

public sealed class DiscoveryPingConsumer : IConsumer<DiscoveryPing>
{
    public async Task ConsumeAsync(ConsumeContext<DiscoveryPing> context)
    {
        await context.Advanced().PublishAsync(new PingReceived(context.Message.CorrelationId), context.CancellationToken);
        await context.RespondAsync(new DiscoveryPong(context.Message.CorrelationId));
    }
}

public sealed class DiscoveryPingConsumerDefinition : ConsumerDefinition<DiscoveryPingConsumer>
{
    public DiscoveryPingConsumerDefinition()
    {
        EndpointName = "ping-queue";
        ConcurrentMessageLimit = 1;
    }
}

public sealed class DiscoveryExcludedConsumer : IConsumer<DiscoveryPong>
{
    public Task ConsumeAsync(ConsumeContext<DiscoveryPong> context) => Task.CompletedTask;
}

public sealed class DiscoveryPingSaga :
    ISaga,
    IInitiatedBy<PingReceived>,
    IOrchestrates<PingAcknowledged>
{
    public Guid CorrelationId { get; set; }

    public Task ConsumeAsync(ConsumeContext<PingReceived> context) => Task.CompletedTask;

    public Task ConsumeAsync(ConsumeContext<PingAcknowledged> context) => Task.CompletedTask;
}

public sealed class DiscoveryPingState : ISagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
}

public sealed class DiscoveryPingStateMachine : ViciOneServiceBusStateMachine<DiscoveryPingState>
{
    public DiscoveryPingStateMachine()
    {
        InstanceState(instance => instance.CurrentState);
        Event(() => Received, configuration =>
        {
            configuration.CorrelateById(context => context.Message.CorrelationId);
            configuration.SelectId(context => context.Message.CorrelationId);
            configuration.InsertOnInitial = true;
        });
        Event(() => Acknowledged, configuration =>
            configuration.CorrelateById(context => context.Message.CorrelationId));

        Initially(When(Received).TransitionTo(Pinged));
        During(Pinged,
            When(Acknowledged)
                .Publish(context => new PingCompleted(context.Message.CorrelationId))
                .TransitionTo(Ponged));
    }

    public IState Pinged { get; } = null!;
    public IState Ponged { get; } = null!;
    public IEvent<PingReceived> Received { get; } = null!;
    public IEvent<PingAcknowledged> Acknowledged { get; } = null!;
}

public sealed class DiscoveryPingStateDefinition : SagaDefinition<DiscoveryPingState>
{
    public DiscoveryPingStateDefinition() => EndpointName = "discovery-ping-state";
}

public sealed class PingActivity : IActivity<PingArguments, PingLog>
{
    public Task<ExecutionResult> ExecuteAsync(ExecuteContext<PingArguments> context) =>
        Task.FromResult(context.Completed<PingLog>(new PingLog(context.Arguments.CorrelationId)));

    public Task<CompensationResult> CompensateAsync(CompensateContext<PingLog> context) =>
        Task.FromResult(context.Compensated());
}

public sealed class PingSecondActivity : IExecuteActivity<PingArguments>
{
    public Task<ExecutionResult> ExecuteAsync(ExecuteContext<PingArguments> context) =>
        Task.FromResult(context.Completed());
}
