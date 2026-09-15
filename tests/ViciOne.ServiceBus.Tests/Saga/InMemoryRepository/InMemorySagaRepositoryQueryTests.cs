using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Saga.InMemoryRepository;

public sealed class InMemorySagaRepositoryQueryTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "both-query-entry-points-keep-original-registered-id-after-retirement-and-replacement")]
    public async Task Queries_KeepRegisteredSnapshotIdsWhenACallbackRetiresAndChangesTheStateAsync(bool messageQuery, bool replace)
    {
        var dictionary = new IndexedSagaDictionary<QueryState>();
        var instance = new SagaInstance<QueryState>(new QueryState());
        Guid registeredId = instance.Instance.CorrelationId;
        Guid retiredId = NewId.NextGuid();
        var replacement = new SagaInstance<QueryState>(new QueryState { CorrelationId = registeredId });
        dictionary.Add(instance);
        int predicateCalls = 0;
        Func<QueryState, bool> retire = state =>
        {
            predicateCalls++;
            dictionary.Remove(instance);
            if (replace)
                dictionary.Add(replacement);
            state.CorrelationId = retiredId;
            return true;
        };
        var query = new SagaQuery<QueryState>(state => retire(state));
        Guid[] results;
        if (messageQuery)
        {
            var factory = CreateFactory(dictionary);
            var next = new CapturingQueryPipe();
            await factory.SendQueryAsync(CreateConsumeContext(TestContext.Current.CancellationToken), query, next);
            Assert.Equal(1, next.Calls);
            Assert.Equal(1, next.ResultCount);
            results = next.Results;
        }
        else
        {
            var context = new InMemorySagaRepositoryContext<QueryState>(dictionary, TestContext.Current.CancellationToken);
            ISagaRepositoryQueryContext<QueryState> result = await context.QueryAsync(query, TestContext.Current.CancellationToken);
            Assert.Equal(1, result.Count);
            results = result.ToArray();
        }

        Assert.Equal(registeredId, Assert.Single(results));
        Assert.DoesNotContain(retiredId, results);
        Assert.Equal(1, predicateCalls);
        Assert.True(instance.IsRemoved);
        Assert.Equal(replace ? 1 : 0, dictionary.Count);
        if (replace)
        {
            Assert.Same(replacement, dictionary[registeredId]);
            Assert.False(replacement.IsRemoved);
        }
        else
            Assert.Null(dictionary[registeredId]);
        Assert.Null(dictionary[retiredId]);
        await AssertDictionaryLeaseAvailableAsync(dictionary);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "query-results-materialize-ids-before-pipeline-or-caller-state-mutation")]
    public async Task Queries_MaterializeIdsBeforePipelineOrCallerMutationAsync(bool messageQuery)
    {
        var dictionary = new IndexedSagaDictionary<QueryState>();
        var instance = new SagaInstance<QueryState>(new QueryState());
        Guid registeredId = instance.Instance.CorrelationId;
        Guid retiredId = NewId.NextGuid();
        dictionary.Add(instance);
        var query = new SagaQuery<QueryState>(state => true);
        Guid[] results;
        if (messageQuery)
        {
            var next = new CapturingQueryPipe(() =>
            {
                dictionary.Remove(instance);
                instance.Instance.CorrelationId = retiredId;
            });
            await CreateFactory(dictionary).SendQueryAsync(CreateConsumeContext(TestContext.Current.CancellationToken), query, next);
            Assert.Equal(1, next.Calls);
            results = next.Results;
        }
        else
        {
            var context = new InMemorySagaRepositoryContext<QueryState>(dictionary, TestContext.Current.CancellationToken);
            ISagaRepositoryQueryContext<QueryState> result = await context.QueryAsync(query, TestContext.Current.CancellationToken);
            dictionary.Remove(instance);
            instance.Instance.CorrelationId = retiredId;
            results = result.ToArray();
            Assert.Equal(registeredId, Assert.Single(result));
        }

        Assert.Equal(registeredId, Assert.Single(results));
        Assert.Equal(0, dictionary.Count);
        Assert.True(instance.IsRemoved);
        await AssertDictionaryLeaseAvailableAsync(dictionary);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "required-query-input-is-validated-before-pre-cancellation-for-both-entry-points")]
    public async Task Queries_InvalidInputTakesPrecedenceOverPreCancellationAsync(bool messageQuery)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var dictionary = new IndexedSagaDictionary<QueryState>();
        var instance = new SagaInstance<QueryState>(new QueryState());
        dictionary.Add(instance);
        var next = new CapturingQueryPipe();

        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(() => messageQuery
            ? CreateFactory(dictionary).SendQueryAsync(CreateConsumeContext(cancellation.Token), null!, next)
            : new InMemorySagaRepositoryContext<QueryState>(dictionary, cancellation.Token).QueryAsync(null!, cancellation.Token));

        Assert.Equal("query", exception.ParamName);
        Assert.Equal(0, next.Calls);
        Assert.Equal(1, dictionary.Count);
        Assert.Same(instance, dictionary[instance.Instance.CorrelationId]);
        await AssertDictionaryLeaseAvailableAsync(dictionary);
    }

    [Theory]
    [InlineData("context")]
    [InlineData("query")]
    [InlineData("next")]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "send-query-requires-every-input-before-acquisition-or-pipeline-effects")]
    public async Task SendQuery_MissingRequiredInputFailsBeforeAcquisitionAsync(string input)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var dictionary = new IndexedSagaDictionary<QueryState>();
        var instance = new SagaInstance<QueryState>(new QueryState());
        dictionary.Add(instance);
        var next = new CapturingQueryPipe();
        ConsumeContext<QueryMessage> context = CreateConsumeContext(cancellation.Token);
        var query = new SagaQuery<QueryState>(state => true);

        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(() => CreateFactory(dictionary).SendQueryAsync(
            input == "context" ? null! : context, input == "query" ? null! : query, input == "next" ? null! : next));

        Assert.Equal(input, exception.ParamName);
        Assert.Equal(0, next.Calls);
        Assert.Equal(1, dictionary.Count);
        Assert.Same(instance, dictionary[instance.Instance.CorrelationId]);
        await AssertDictionaryLeaseAvailableAsync(dictionary);
    }

    private static InMemorySagaRepositoryContextFactory<QueryState> CreateFactory(IndexedSagaDictionary<QueryState> dictionary) =>
        new(dictionary, new InMemorySagaConsumeContextFactory<QueryState>());

    private static ConsumeContext<QueryMessage> CreateConsumeContext(CancellationToken cancellationToken) =>
        InMemoryOutboxTestContextFactory.Create(new QueryMessage(), cancellationToken);

    private static async Task AssertDictionaryLeaseAvailableAsync(IndexedSagaDictionary<QueryState> dictionary)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.CancelAfter(TimeSpan.FromSeconds(5));
        await dictionary.MarkInUseAsync(cancellation.Token);
        Assert.Null(Record.Exception(dictionary.Release));
    }

    private sealed class CapturingQueryPipe(Action? beforeEnumeration = null) : IPipe<ISagaRepositoryQueryContext<QueryState, QueryMessage>>
    {
        public int Calls { get; private set; }
        public int ResultCount { get; private set; }
        public Guid[] Results { get; private set; } = [];

        public Task SendAsync(ISagaRepositoryQueryContext<QueryState, QueryMessage> context)
        {
            Calls++;
            beforeEnumeration?.Invoke();
            ResultCount = context.Count;
            Results = context.ToArray();
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context) => context.CreateScope("queryCapture");
    }

    private sealed class QueryState : ISaga
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
    }

    public sealed record QueryMessage;
}
