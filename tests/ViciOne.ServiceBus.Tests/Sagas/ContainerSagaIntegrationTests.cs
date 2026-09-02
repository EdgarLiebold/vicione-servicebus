using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas;

public sealed class ContainerSagaIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-SAGA", "three-message-container-repository-lifecycle")]
    public async Task ContainerSaga_ConsumesTheInitiatingOrchestratedAndObservedMessagesExactlyOnce()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddSaga<ContainerLifecycleSaga>().InMemoryRepository();
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);
        bool started = true;

        try
        {
            Guid correlationId = NewId.NextGuid();
            var first = new SagaFirst(correlationId, "first");
            var second = new SagaSecond(correlationId, "second");
            var third = new SagaThird(correlationId, "third");
            ISagaTestHarness<ContainerLifecycleSaga> sagaHarness =
                harness.GetSagaHarness<ContainerLifecycleSaga>();

            await harness.Bus.Publish(first, cancellationToken);
            await sagaHarness.Consumed.SelectAsync<SagaFirst>(cancellationToken)
                .First().WaitAsync(timeout, cancellationToken);
            await harness.Bus.Publish(second, cancellationToken);
            await sagaHarness.Consumed.SelectAsync<SagaSecond>(cancellationToken)
                .First().WaitAsync(timeout, cancellationToken);
            await harness.Bus.Publish(third, cancellationToken);
            await sagaHarness.Consumed.SelectAsync<SagaThird>(cancellationToken)
                .First().WaitAsync(timeout, cancellationToken);

            ContainerLifecycleSaga instance = sagaHarness.Sagas.Contains(correlationId);
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            started = false;

            Assert.NotNull(instance);
            Assert.Equal(correlationId, instance.CorrelationId);
            Assert.Equal(new[] { "first", "second", "third" }, instance.Values);
            Assert.Single(sagaHarness.Consumed.Select<SagaFirst>(SnapshotOnlyToken()));
            Assert.Single(sagaHarness.Consumed.Select<SagaSecond>(SnapshotOnlyToken()));
            Assert.Single(sagaHarness.Consumed.Select<SagaThird>(SnapshotOnlyToken()));
        }
        finally
        {
            if (started)
                await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-SAGA", "inline-name-and-definition-name-precedence")]
    public async Task SagaEndpointOverrides_RouteToTheInlineAndDefinitionOwnedAddresses()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddSaga<InlineEndpointSaga>()
                    .Endpoint(endpoint => endpoint.Name = "custom-container-saga")
                    .InMemoryRepository();
                configuration.AddSaga<DefinitionEndpointSaga, DefinitionEndpointSagaDefinition>()
                    .Endpoint(endpoint => endpoint.Temporary = true)
                    .InMemoryRepository();
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);

        try
        {
            Guid inlineId = NewId.NextGuid();
            Guid definitionId = NewId.NextGuid();
            ISendEndpoint inline = await harness.Bus.GetSendEndpoint(new Uri("queue:custom-container-saga"))
                .WaitAsync(timeout, cancellationToken);
            ISendEndpoint definition = await harness.Bus.GetSendEndpoint(new Uri("queue:custom-definition-saga"))
                .WaitAsync(timeout, cancellationToken);

            await inline.Send(new InlineSagaStart(inlineId), cancellationToken);
            await definition.Send(new DefinitionSagaStart(definitionId), cancellationToken);

            ISagaTestHarness<InlineEndpointSaga> inlineHarness = harness.GetSagaHarness<InlineEndpointSaga>();
            ISagaTestHarness<DefinitionEndpointSaga> definitionHarness =
                harness.GetSagaHarness<DefinitionEndpointSaga>();
            IReceivedMessage<InlineSagaStart> inlineReceived = await inlineHarness.Consumed
                .SelectAsync<InlineSagaStart>(cancellationToken).First().WaitAsync(timeout, cancellationToken);
            IReceivedMessage<DefinitionSagaStart> definitionReceived = await definitionHarness.Consumed
                .SelectAsync<DefinitionSagaStart>(cancellationToken).First().WaitAsync(timeout, cancellationToken);

            Assert.Equal(inlineId, inlineHarness.Sagas.Contains(inlineId).CorrelationId);
            Assert.Equal(definitionId, definitionHarness.Sagas.Contains(definitionId).CorrelationId);
            Assert.Equal("custom-container-saga", inlineReceived.Context.ReceiveContext.InputAddress.AbsolutePath.Trim('/'));
            Assert.Equal("custom-definition-saga", definitionReceived.Context.ReceiveContext.InputAddress.AbsolutePath.Trim('/'));
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions().OperationTimeout!.Value;

    private static CancellationToken SnapshotOnlyToken() => new(canceled: true);

    public sealed record SagaFirst(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;
    public sealed record SagaSecond(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;
    public sealed record SagaThird(Guid CorrelationId, string Value);

    public sealed class ContainerLifecycleSaga :
        ISaga,
        InitiatedBy<SagaFirst>,
        Orchestrates<SagaSecond>,
        Observes<SagaThird, ContainerLifecycleSaga>
    {
        public ContainerLifecycleSaga(Guid correlationId) => CorrelationId = correlationId;

        private ContainerLifecycleSaga()
        {
        }

        public Guid CorrelationId { get; set; }

        public List<string> Values { get; private set; } = [];

        public Task Consume(ConsumeContext<SagaFirst> context)
        {
            Values.Add(context.Message.Value);
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<SagaSecond> context)
        {
            Values.Add(context.Message.Value);
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<SagaThird> context)
        {
            Values.Add(context.Message.Value);
            return Task.CompletedTask;
        }

        Expression<Func<ContainerLifecycleSaga, SagaThird, bool>>
            Observes<SagaThird, ContainerLifecycleSaga>.CorrelationExpression =>
            (saga, message) => saga.CorrelationId == message.CorrelationId;
    }

    public sealed record InlineSagaStart(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class InlineEndpointSaga(Guid correlationId) : ISaga, InitiatedBy<InlineSagaStart>
    {
        public Guid CorrelationId { get; set; } = correlationId;

        public Task Consume(ConsumeContext<InlineSagaStart> context) => Task.CompletedTask;
    }

    public sealed record DefinitionSagaStart(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class DefinitionEndpointSaga(Guid correlationId) : ISaga, InitiatedBy<DefinitionSagaStart>
    {
        public Guid CorrelationId { get; set; } = correlationId;

        public Task Consume(ConsumeContext<DefinitionSagaStart> context) => Task.CompletedTask;
    }

    public sealed class DefinitionEndpointSagaDefinition : SagaDefinition<DefinitionEndpointSaga>
    {
        public DefinitionEndpointSagaDefinition() => EndpointName = "custom-definition-saga";
    }
}
