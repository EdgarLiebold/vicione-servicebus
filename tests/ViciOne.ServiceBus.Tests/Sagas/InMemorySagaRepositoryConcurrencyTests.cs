using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas;

public sealed class InMemorySagaRepositoryConcurrencyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "removed-instance-unblocks-and-rejects-waiters")]
    public async Task RemovedInstance_UnblocksAndRejectsEveryWaitingOwnerAsync()
    {
        var instance = new SagaInstance<RepositoryState>(new RepositoryState { CorrelationId = NewId.NextGuid() });
        await instance.MarkInUseAsync(TestContext.Current.CancellationToken);

        Task waitingOwner = instance.MarkInUseAsync(TestContext.Current.CancellationToken);
        Assert.False(waitingOwner.IsCompleted);

        instance.Remove();

        InvalidOperationException waitingException = await Assert.ThrowsAnyAsync<InvalidOperationException>(
            () => waitingOwner);
        InvalidOperationException subsequentException = await Assert.ThrowsAnyAsync<InvalidOperationException>(
            () => instance.MarkInUseAsync(TestContext.Current.CancellationToken));

        Assert.Contains(instance.Instance.CorrelationId.ToString(), waitingException.Message, StringComparison.Ordinal);
        Assert.Contains(instance.Instance.CorrelationId.ToString(), subsequentException.Message, StringComparison.Ordinal);

        instance.Release();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "dictionary-removal-invalidates-old-instance")]
    public async Task DictionaryRemoval_InvalidatesTheRemovedInstanceAndAcceptsAReplacementAsync()
    {
        Guid correlationId = NewId.NextGuid();
        var dictionary = new IndexedSagaDictionary<RepositoryState>();
        var removed = new SagaInstance<RepositoryState>(new RepositoryState { CorrelationId = correlationId });
        dictionary.Add(removed);

        await removed.MarkInUseAsync(TestContext.Current.CancellationToken);
        dictionary.Remove(removed);
        removed.Release();

        Assert.True(removed.IsRemoved);
        Assert.Null(dictionary[correlationId]);
        await Assert.ThrowsAnyAsync<InvalidOperationException>(
            () => removed.MarkInUseAsync(TestContext.Current.CancellationToken));

        var replacement = new SagaInstance<RepositoryState>(new RepositoryState { CorrelationId = correlationId });
        dictionary.Add(replacement);

        Assert.Same(replacement, dictionary[correlationId]);
        Assert.False(replacement.IsRemoved);
    }

    sealed class RepositoryState : ISaga
    {
        public Guid CorrelationId { get; set; }
    }
}
