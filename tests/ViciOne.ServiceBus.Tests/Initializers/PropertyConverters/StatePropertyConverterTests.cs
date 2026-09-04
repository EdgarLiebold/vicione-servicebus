using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyConverters;

public sealed class StatePropertyConverterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-STATE-PROPERTY", "integer-state-to-name-and-header")]
    public async Task PublishInitializer_MapsTheCurrentIntegerStateToItsNameAsync()
    {
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        using var harness = new InMemoryTestHarness($"initializer-state-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
        };
        harness.BeginTestScope();
        var machine = new IntegerStateMachine();
        var repository = new InMemorySagaRepository<IntegerStateInstance>();
        var received = new TaskCompletionSource<ConsumeContext<StateTransitionPublished>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
        {
            configurator.StateMachineSaga(machine, repository);
            configurator.Handler<StateTransitionPublished>(context =>
            {
                received.TrySetResult(context);
                return Task.CompletedTask;
            });
        };
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var sagaId = new Guid("93ce1b99-a905-450d-9eb6-6c3057a791f0");

        try
        {
            await harness.StartAsync(cancellationToken);

            await harness.Bus.PublishAsync(new StateTransitionStarted { CorrelationId = sagaId }, cancellationToken);

            ConsumeContext<StateTransitionPublished> context = await received.Task.WaitAsync(
                operationTimeout,
                cancellationToken);

            Assert.Equal(sagaId, context.Message.CorrelationId);
            Assert.Equal(machine.Running.Name, context.Message.CurrentState);
            Assert.True(context.Headers.TryGetHeader("Custom-Header-Value", out object? header));
            Assert.Equal("Frankie Say Relax", header);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private sealed class IntegerStateInstance : SagaStateMachineInstance
    {
        public int CurrentState { get; set; }

        public Guid CorrelationId { get; set; }
    }

    private sealed class IntegerStateMachine : ViciOneServiceBusStateMachine<IntegerStateInstance>
    {
        public IntegerStateMachine()
        {
            InstanceState(instance => instance.CurrentState, Running);
            Event(() => Started);

            Initially(
                When(Started)
                    .TransitionTo(Running)
                    .PublishAsync(context => context.InitAsync<StateTransitionPublished>(new
                    {
                        context.Saga.CorrelationId,
                        CurrentState = this.GetStateAsync(context),
                        __Header_Custom_Header_Value = "Frankie Say Relax",
                    })));
        }

        public State Running { get; } = null!;

        public Event<StateTransitionStarted> Started { get; } = null!;
    }

    public sealed class StateTransitionStarted : CorrelatedBy<Guid>
    {
        public Guid CorrelationId { get; init; }
    }

    public sealed class StateTransitionPublished
    {
        public Guid CorrelationId { get; init; }

        public string CurrentState { get; init; } = null!;
    }
}
