using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Saga.InMemoryRepository;

public sealed class InMemorySagaRepositoryContextFactoryTests
{
    [Theory]
    [InlineData("message-sagas")]
    [InlineData("message-factory")]
    [InlineData("message-context")]
    [InlineData("load-sagas")]
    [InlineData("factory-sagas")]
    [InlineData("factory-factory")]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "repository-context-and-factory-construction-require-dependencies")]
    public void RequiredConstructorDependency_IsRejectedWithItsExactParameterName(string input)
    {
        var dictionary = new IndexedSagaDictionary<FactoryState>();
        var consumeFactory = new InMemorySagaConsumeContextFactory<FactoryState>();
        ConsumeContext<FactoryMessage> consumeContext = CreateConsumeContext();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
        {
            switch (input)
            {
                case "message-sagas": _ = new InMemorySagaRepositoryContext<FactoryState, FactoryMessage>(null!, consumeFactory, consumeContext); break;
                case "message-factory": _ = new InMemorySagaRepositoryContext<FactoryState, FactoryMessage>(dictionary, null!, consumeContext); break;
                case "message-context": _ = new InMemorySagaRepositoryContext<FactoryState, FactoryMessage>(dictionary, consumeFactory, null!); break;
                case "load-sagas": _ = new InMemorySagaRepositoryContext<FactoryState>(null!, default); break;
                case "factory-sagas": _ = new InMemorySagaRepositoryContextFactory<FactoryState>(null!, consumeFactory); break;
                case "factory-factory": _ = new InMemorySagaRepositoryContextFactory<FactoryState>(dictionary, null!); break;
                default: throw new ArgumentOutOfRangeException(nameof(input));
            }
        });

        Assert.Equal(input[(input.IndexOf('-') + 1)..], exception.ParamName);
        Assert.Equal(0, dictionary.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "execute-requires-callback-and-task")]
    public async Task Execute_RejectsNullCallbacksAndNullTasksForBothCapabilitiesAsync(bool query)
    {
        var factory = new InMemorySagaRepositoryContextFactory<FactoryState>(new IndexedSagaDictionary<FactoryState>(), new InMemorySagaConsumeContextFactory<FactoryState>());
        ArgumentNullException missingCallback = await Assert.ThrowsAsync<ArgumentNullException>(() => query
            ? (Task)factory.ExecuteAsync<FactoryState>((Func<IQuerySagaRepositoryContext<FactoryState>, Task<FactoryState>>)null!, TestContext.Current.CancellationToken)
            : factory.ExecuteAsync<FactoryState>((Func<ILoadSagaRepositoryContext<FactoryState>, Task<FactoryState?>>)null!, TestContext.Current.CancellationToken));
        int calls = 0;
        InvalidOperationException missingTask = await Assert.ThrowsAsync<InvalidOperationException>(() => query
            ? (Task)factory.ExecuteAsync<FactoryState>((IQuerySagaRepositoryContext<FactoryState> _) => { calls++; return null!; }, TestContext.Current.CancellationToken)
            : factory.ExecuteAsync<FactoryState>((ILoadSagaRepositoryContext<FactoryState> _) => { calls++; return null!; }, TestContext.Current.CancellationToken));

        Assert.Equal("asyncMethod", missingCallback.ParamName);
        Assert.Equal("The saga repository callback returned a null task.", missingTask.Message);
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "execute-pre-cancellation-never-invokes-callback")]
    public async Task Execute_PreCancellationDoesNotInvokeTheCallbackAndPreservesTheExactTokenAsync(bool query)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var factory = new InMemorySagaRepositoryContextFactory<FactoryState>(new IndexedSagaDictionary<FactoryState>(), new InMemorySagaConsumeContextFactory<FactoryState>());
        int calls = 0;

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => query
            ? (Task)factory.ExecuteAsync<FactoryState>((IQuerySagaRepositoryContext<FactoryState> _) => { calls++; return Task.FromResult(new FactoryState()); }, cancellation.Token)
            : factory.ExecuteAsync<FactoryState>((ILoadSagaRepositoryContext<FactoryState> _) => { calls++; return Task.FromResult<FactoryState?>(new FactoryState()); }, cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "execute-preserves-result-token-and-fault-identity")]
    public async Task Execute_PreservesTheExactResultTokenAndCallbackFaultAsync(bool query)
    {
        using var cancellation = new CancellationTokenSource();
        var factory = new InMemorySagaRepositoryContextFactory<FactoryState>(new IndexedSagaDictionary<FactoryState>(), new InMemorySagaConsumeContextFactory<FactoryState>());
        var expected = new FactoryState();
        var expectedFailure = new InvalidOperationException("callback failure");
        CancellationToken observed = default;
        FactoryState? result = query
            ? await factory.ExecuteAsync<FactoryState>((IQuerySagaRepositoryContext<FactoryState> context) => { observed = context.CancellationToken; return Task.FromResult(expected); }, cancellation.Token)
            : await factory.ExecuteAsync<FactoryState>((ILoadSagaRepositoryContext<FactoryState> context) => { observed = context.CancellationToken; return Task.FromResult<FactoryState?>(expected); }, cancellation.Token);
        Exception failure = await Assert.ThrowsAsync<InvalidOperationException>(() => query
            ? (Task)factory.ExecuteAsync<FactoryState>((IQuerySagaRepositoryContext<FactoryState> _) => Task.FromException<FactoryState>(expectedFailure), TestContext.Current.CancellationToken)
            : factory.ExecuteAsync<FactoryState>((ILoadSagaRepositoryContext<FactoryState> _) => Task.FromException<FactoryState?>(expectedFailure), TestContext.Current.CancellationToken));

        Assert.Same(expected, result);
        Assert.Equal(cancellation.Token, observed);
        Assert.Same(expectedFailure, failure);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "execute-preserves-synchronously-thrown-callback-fault")]
    public async Task Execute_SynchronousCallbackFaultRetainsItsExactIdentityAsync(bool query)
    {
        var factory = new InMemorySagaRepositoryContextFactory<FactoryState>(new IndexedSagaDictionary<FactoryState>(), new InMemorySagaConsumeContextFactory<FactoryState>());
        var expected = new InvalidOperationException("synchronous callback failure");

        InvalidOperationException observed = await Assert.ThrowsAsync<InvalidOperationException>(() => query
            ? (Task)factory.ExecuteAsync<FactoryState>((IQuerySagaRepositoryContext<FactoryState> _) => throw expected, TestContext.Current.CancellationToken)
            : factory.ExecuteAsync<FactoryState>((ILoadSagaRepositoryContext<FactoryState> _) => throw expected, TestContext.Current.CancellationToken));

        Assert.Same(expected, observed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "execute-allows-null-load-result-from-a-valid-task")]
    public async Task Execute_NullLoadResultIsAllowedWhenItsTaskIsValidAsync()
    {
        var factory = new InMemorySagaRepositoryContextFactory<FactoryState>(new IndexedSagaDictionary<FactoryState>(), new InMemorySagaConsumeContextFactory<FactoryState>());
        int calls = 0;

        FactoryState? result = await factory.ExecuteAsync<FactoryState>((ILoadSagaRepositoryContext<FactoryState> _) =>
        {
            calls++;
            return Task.FromResult<FactoryState?>(null);
        }, TestContext.Current.CancellationToken);

        Assert.Null(result);
        Assert.Equal(1, calls);
    }

    private static ConsumeContext<FactoryMessage> CreateConsumeContext() =>
        InMemoryOutboxTestContextFactory.Create(new FactoryMessage(), TestContext.Current.CancellationToken);

    private sealed class FactoryState : ISaga
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
    }

    public sealed record FactoryMessage;
}
