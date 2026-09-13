using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class ConfigureEndpointExclusionTests
{
    private const string ControlEndpointName = "native-exclusion-control";

    [Theory]
    [InlineData(ExclusionShape.ConsumerExplicit)]
    [InlineData(ExclusionShape.ConsumerAttribute)]
    [InlineData(ExclusionShape.SagaExplicit)]
    [InlineData(ExclusionShape.SagaAttribute)]
    [InlineData(ExclusionShape.StateMachineExplicit)]
    [InlineData(ExclusionShape.StateMachineInstanceAttribute)]
    [RequirementCoverage("REQ-VSB-DI-CONFIGURE-ENDPOINTS", "consumer-saga-and-state-machine-exclusion-matrix")]
    public async Task ExcludedRegistrationNeverCreatesAnEndpointOrConsumesItsMessageAsync(
        ExclusionShape shape)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<ControlConsumer>()
                    .Endpoint(endpoint => endpoint.Name = ControlEndpointName);
                RegisterExcludedOwner(configuration, shape);
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            JsonNode probe = JsonNode.Parse(JsonSerializer.Serialize(
                provider.GetRequiredService<IBusControl>().GetProbeResult(cancellationToken).Results))!;
            Assert.Equal([ControlEndpointName], NonBusEndpointNames(probe));

            Guid correlationId = NewId.NextGuid();
            await harness.Bus.PublishAsync(new ExcludedMessage(correlationId), cancellationToken);
            await harness.Bus.PublishAsync(new ControlMessage(correlationId), cancellationToken);
            IConsumedMessage<ControlMessage> control = await harness.Consumed
                .SelectAsync<ControlMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
            Assert.Equal(correlationId, control.Context.Message.CorrelationId);
            Assert.Null(control.Exception);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(harness.Consumed.Snapshot<ControlMessage>());
        Assert.Empty(harness.Consumed.Snapshot<ExcludedMessage>());
    }

    private static void RegisterExcludedOwner(
        IBusRegistrationConfigurator configuration,
        ExclusionShape shape)
    {
        switch (shape)
        {
            case ExclusionShape.ConsumerExplicit:
                configuration.AddConsumer<ExplicitlyExcludedConsumer>()
                    .ExcludeFromConfigureEndpoints();
                break;
            case ExclusionShape.ConsumerAttribute:
                configuration.AddConsumer<AttributedExcludedConsumer>();
                break;
            case ExclusionShape.SagaExplicit:
                configuration.AddSaga<ExplicitlyExcludedSaga>()
                    .ExcludeFromConfigureEndpoints();
                break;
            case ExclusionShape.SagaAttribute:
                configuration.AddSaga<AttributedExcludedSaga>();
                break;
            case ExclusionShape.StateMachineExplicit:
                configuration.AddSagaStateMachine<ExplicitlyExcludedMachine, ExplicitlyExcludedState>()
                    .ExcludeFromConfigureEndpoints();
                break;
            case ExclusionShape.StateMachineInstanceAttribute:
                configuration.AddSagaStateMachine<AttributedStateMachine, AttributedExcludedState>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
        }
    }

    private static string[] NonBusEndpointNames(JsonNode root) =>
        NodesIn(root)
            .OfType<JsonObject>()
            .Where(node => node["name"] is not null && node["receiveTransport"] is JsonObject)
            .Select(node => node["name"]!.GetValue<string>())
            .Where(name => !name.Contains("_bus_", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    private static IEnumerable<JsonNode> NodesIn(JsonNode node)
    {
        yield return node;
        if (node is JsonObject jsonObject)
        {
            foreach ((_, JsonNode? value) in jsonObject)
            {
                if (value is null)
                    continue;

                foreach (JsonNode nested in NodesIn(value))
                    yield return nested;
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (JsonNode? value in jsonArray)
            {
                if (value is null)
                    continue;

                foreach (JsonNode nested in NodesIn(value))
                    yield return nested;
            }
        }
    }


    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public enum ExclusionShape
    {
        ConsumerExplicit,
        ConsumerAttribute,
        SagaExplicit,
        SagaAttribute,
        StateMachineExplicit,
        StateMachineInstanceAttribute,
    }

    public sealed record ControlMessage(Guid CorrelationId);

    public sealed record ExcludedMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    private sealed class ControlConsumer : IConsumer<ControlMessage>
    {
        public Task ConsumeAsync(ConsumeContext<ControlMessage> context) => Task.CompletedTask;
    }

    private sealed class ExplicitlyExcludedConsumer : IConsumer<ExcludedMessage>
    {
        public Task ConsumeAsync(ConsumeContext<ExcludedMessage> context) => Task.CompletedTask;
    }

    [ExcludeFromConfigureEndpoints]
    private sealed class AttributedExcludedConsumer : IConsumer<ExcludedMessage>
    {
        public Task ConsumeAsync(ConsumeContext<ExcludedMessage> context) => Task.CompletedTask;
    }

    private sealed class ExplicitlyExcludedSaga : ISaga, IInitiatedBy<ExcludedMessage>
    {
        public Guid CorrelationId { get; set; }

        public Task ConsumeAsync(ConsumeContext<ExcludedMessage> context) => Task.CompletedTask;
    }

    [ExcludeFromConfigureEndpoints]
    private sealed class AttributedExcludedSaga : ISaga, IInitiatedBy<ExcludedMessage>
    {
        public Guid CorrelationId { get; set; }

        public Task ConsumeAsync(ConsumeContext<ExcludedMessage> context) => Task.CompletedTask;
    }

    private sealed class ExplicitlyExcludedState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public IState CurrentState { get; set; } = null!;
    }

    [ExcludeFromConfigureEndpoints]
    private sealed class AttributedExcludedState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public IState CurrentState { get; set; } = null!;
    }

    private sealed class ExplicitlyExcludedMachine :
        ViciOneServiceBusStateMachine<ExplicitlyExcludedState>
    {
        public ExplicitlyExcludedMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Initially(When(Started).Finalize());
            SetCompletedWhenFinalized();
        }

        public IEvent<ExcludedMessage> Started { get; private set; } = null!;
    }

    private sealed class AttributedStateMachine :
        ViciOneServiceBusStateMachine<AttributedExcludedState>
    {
        public AttributedStateMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Initially(When(Started).Finalize());
            SetCompletedWhenFinalized();
        }

        public IEvent<ExcludedMessage> Started { get; private set; } = null!;
    }
}
