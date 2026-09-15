using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Saga;

public sealed class SagaConsumeContextFactoryTests
{
    [Theory]
    [InlineData(false, "context")]
    [InlineData(false, "consumeContext")]
    [InlineData(false, "instance")]
    [InlineData(true, "context")]
    [InlineData(true, "consumeContext")]
    [InlineData(true, "instance")]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "consume-context-factories-require-every-input")]
    public async Task RequiredFactoryInput_IsRejectedBeforeAcquisitionOrMutationAsync(bool memory, string input)
    {
        var dictionary = new IndexedSagaDictionary<FactoryState>();
        ConsumeContext<FactoryMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(new FactoryMessage(), TestContext.Current.CancellationToken);
        var state = new FactoryState();
        ISagaConsumeContextFactory<IndexedSagaDictionary<FactoryState>, FactoryState> factory = memory
            ? new InMemorySagaConsumeContextFactory<FactoryState>()
            : new SagaConsumeContextFactory<IndexedSagaDictionary<FactoryState>, FactoryState>();

        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(() => factory.CreateSagaConsumeContextAsync(
            input == "context" ? null! : dictionary,
            input == "consumeContext" ? null! : consumeContext,
            input == "instance" ? null! : state,
            SagaConsumeContextMode.Add));

        Assert.Equal(input, exception.ParamName);
        Assert.Equal(0, dictionary.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "consume-context-factories-reject-invalid-modes")]
    public async Task InvalidMode_IsRejectedBeforeAcquisitionOrMutationAsync(bool memory)
    {
        var dictionary = new IndexedSagaDictionary<FactoryState>();
        ConsumeContext<FactoryMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(new FactoryMessage(), TestContext.Current.CancellationToken);
        ISagaConsumeContextFactory<IndexedSagaDictionary<FactoryState>, FactoryState> factory = memory
            ? new InMemorySagaConsumeContextFactory<FactoryState>()
            : new SagaConsumeContextFactory<IndexedSagaDictionary<FactoryState>, FactoryState>();

        ArgumentOutOfRangeException exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            factory.CreateSagaConsumeContextAsync(dictionary, consumeContext, new FactoryState(), (SagaConsumeContextMode)int.MaxValue));

        Assert.Equal("mode", exception.ParamName);
        Assert.Equal(0, dictionary.Count);
    }

    [Theory]
    [InlineData(SagaConsumeContextMode.Add)]
    [InlineData(SagaConsumeContextMode.Insert)]
    [InlineData(SagaConsumeContextMode.Load)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "default-factory-wraps-state-without-persistence")]
    public async Task DefaultFactory_EachValidModeWrapsTheSameStateWithoutMutatingTheRepositoryAsync(SagaConsumeContextMode mode)
    {
        var dictionary = new IndexedSagaDictionary<FactoryState>();
        ConsumeContext<FactoryMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(new FactoryMessage(), TestContext.Current.CancellationToken);
        var state = new FactoryState();
        var factory = new SagaConsumeContextFactory<IndexedSagaDictionary<FactoryState>, FactoryState>();

        SagaConsumeContext<FactoryState, FactoryMessage> result = await factory.CreateSagaConsumeContextAsync(dictionary, consumeContext, state, mode);

        Assert.Same(state, result.Saga);
        Assert.Equal(consumeContext.CancellationToken, result.CancellationToken);
        Assert.Equal(state.CorrelationId, result.CorrelationId);
        Assert.Equal(0, dictionary.Count);
    }

    private sealed class FactoryState : ISaga
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
    }

    public sealed record FactoryMessage;
}
