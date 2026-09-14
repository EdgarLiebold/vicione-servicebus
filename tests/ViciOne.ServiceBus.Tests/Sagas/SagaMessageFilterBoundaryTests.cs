using System.Linq.Expressions;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas;

public sealed class SagaMessageFilterBoundaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "message-filters-require-context-and-next-pipe")]
    public async Task MessageFilters_RejectEveryMissingPipelineArgumentBeforeConsumingAsync()
    {
        ISagaMessageFilter<BoundarySaga, BoundaryMessage>[] filters =
        [
            new InitiatedBySagaMessageFilter<BoundarySaga, BoundaryMessage>(),
            new InitiatedByOrOrchestratesSagaMessageFilter<BoundarySaga, BoundaryMessage>(),
            new ObservesSagaMessageFilter<BoundarySaga, BoundaryMessage>(),
            new OrchestratesSagaMessageFilter<BoundarySaga, BoundaryMessage>(),
        ];
        IPipe<SagaConsumeContext<BoundarySaga, BoundaryMessage>> next =
            Pipe.Empty<SagaConsumeContext<BoundarySaga, BoundaryMessage>>();
        var saga = new BoundarySaga { CorrelationId = Guid.NewGuid() };
        using InMemorySagaConsumeContext<BoundarySaga, BoundaryMessage> context = await CreateContextAsync(saga);

        foreach (ISagaMessageFilter<BoundarySaga, BoundaryMessage> filter in filters)
        {
            ArgumentNullException missingContext = await Assert.ThrowsAsync<ArgumentNullException>(
                () => filter.SendAsync(null!, next));
            ArgumentNullException missingNext = await Assert.ThrowsAsync<ArgumentNullException>(
                () => filter.SendAsync(context, null!));

            Assert.Equal("context", missingContext.ParamName);
            Assert.Equal("next", missingNext.ParamName);
        }

        Assert.Equal(0, saga.ConsumeCount);
    }

    private static async Task<InMemorySagaConsumeContext<BoundarySaga, BoundaryMessage>> CreateContextAsync(
        BoundarySaga saga)
    {
        ConsumeContext<BoundaryMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(
            new BoundaryMessage(saga.CorrelationId),
            TestContext.Current.CancellationToken);
        var instance = new SagaInstance<BoundarySaga>(saga);
        await instance.MarkInUseAsync(TestContext.Current.CancellationToken);
        return new InMemorySagaConsumeContext<BoundarySaga, BoundaryMessage>(consumeContext, instance);
    }

    public sealed record BoundaryMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed class BoundarySaga :
        ISaga,
        IInitiatedBy<BoundaryMessage>,
        IInitiatedByOrOrchestrates<BoundaryMessage>,
        IObserves<BoundaryMessage, BoundarySaga>,
        IOrchestrates<BoundaryMessage>
    {
        public Guid CorrelationId { get; set; }

        public int ConsumeCount { get; private set; }

        public Expression<Func<BoundarySaga, BoundaryMessage, bool>> CorrelationExpression =>
            (saga, message) => saga.CorrelationId == message.CorrelationId;

        public Task ConsumeAsync(ConsumeContext<BoundaryMessage> context)
        {
            ConsumeCount++;
            return Task.CompletedTask;
        }
    }
}
