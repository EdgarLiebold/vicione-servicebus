using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas;

public sealed class SagaRepositoryCapabilityTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "dispatch-repository-does-not-advertise-unsupported-capabilities")]
    public void DispatchRepository_ExposesOnlyItsImplementedCapability()
    {
        var repository = new SagaRepository<CapabilitySaga>(new UnusedRepositoryContextFactory());

        Assert.IsAssignableFrom<ISagaRepository<CapabilitySaga>>(repository);
        Assert.IsNotAssignableFrom<ILoadSagaRepository<CapabilitySaga>>(repository);
        Assert.IsNotAssignableFrom<IQuerySagaRepository<CapabilitySaga>>(repository);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "load-capability-is-explicit-and-functional")]
    public async Task LoadableRepository_ExposesAndForwardsOnlyTheRequestedAdditionalCapabilityAsync()
    {
        var saga = new CapabilitySaga { CorrelationId = Guid.NewGuid() };
        var loadFactory = new RecordingLoadFactory(saga);
        using var cancellation = new CancellationTokenSource();

        ILoadableSagaRepository<CapabilitySaga> repository = SagaRepository<CapabilitySaga>.CreateLoadable(
            new UnusedRepositoryContextFactory(),
            loadFactory);

        Assert.IsNotAssignableFrom<IQuerySagaRepository<CapabilitySaga>>(repository);
        Assert.Same(saga, await repository.LoadAsync(saga.CorrelationId, cancellation.Token));
        Assert.Equal(saga.CorrelationId, loadFactory.CorrelationId);
        Assert.Equal(cancellation.Token, loadFactory.FactoryCancellationToken);
        Assert.Equal(cancellation.Token, loadFactory.OperationCancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "query-and-load-capabilities-are-explicit-and-functional")]
    public async Task QueryableLoadableRepository_ForwardsBothRequestedCapabilitiesAsync()
    {
        var saga = new CapabilitySaga { CorrelationId = Guid.NewGuid() };
        Guid[] matches = [saga.CorrelationId, Guid.NewGuid()];
        var loadFactory = new RecordingLoadFactory(saga);
        var queryFactory = new RecordingQueryFactory(matches);
        var query = new SagaQuery<CapabilitySaga>(_ => true);
        using var cancellation = new CancellationTokenSource();

        IQueryableSagaRepository<CapabilitySaga> repository = SagaRepository<CapabilitySaga>.CreateQueryable(
            new UnusedRepositoryContextFactory(),
            queryFactory,
            loadFactory);

        Assert.Same(saga, await repository.LoadAsync(saga.CorrelationId, cancellation.Token));
        Assert.Equal(matches, await repository.FindAsync(query, cancellation.Token));
        Assert.Equal(saga.CorrelationId, loadFactory.CorrelationId);
        Assert.Same(query, queryFactory.Query);
        Assert.Equal(cancellation.Token, loadFactory.FactoryCancellationToken);
        Assert.Equal(cancellation.Token, loadFactory.OperationCancellationToken);
        Assert.Equal(cancellation.Token, queryFactory.FactoryCancellationToken);
        Assert.Equal(cancellation.Token, queryFactory.OperationCancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "all-factory-dependencies-are-required")]
    public void RepositoryCreation_RejectsEveryMissingFactory()
    {
        var repositoryFactory = new UnusedRepositoryContextFactory();
        var loadFactory = new RecordingLoadFactory(null);
        var queryFactory = new RecordingQueryFactory([]);

        Assert.Equal("repositoryContextFactory", Assert.Throws<ArgumentNullException>(
            () => new SagaRepository<CapabilitySaga>(null!)).ParamName);
        Assert.Equal("repositoryContextFactory", Assert.Throws<ArgumentNullException>(
            () => SagaRepository<CapabilitySaga>.CreateLoadable(null!, loadFactory)).ParamName);
        Assert.Equal("loadRepositoryContextFactory", Assert.Throws<ArgumentNullException>(
            () => SagaRepository<CapabilitySaga>.CreateLoadable(repositoryFactory, null!)).ParamName);
        Assert.Equal("repositoryContextFactory", Assert.Throws<ArgumentNullException>(
            () => SagaRepository<CapabilitySaga>.CreateQueryable(null!, queryFactory, loadFactory)).ParamName);
        Assert.Equal("queryRepositoryContextFactory", Assert.Throws<ArgumentNullException>(
            () => SagaRepository<CapabilitySaga>.CreateQueryable(repositoryFactory, null!, loadFactory)).ParamName);
        Assert.Equal("loadRepositoryContextFactory", Assert.Throws<ArgumentNullException>(
            () => SagaRepository<CapabilitySaga>.CreateQueryable(repositoryFactory, queryFactory, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "dispatch-boundaries-reject-every-missing-dependency")]
    public async Task DispatchOperations_RejectEveryMissingRequiredArgumentAsync()
    {
        var repository = new SagaRepository<CapabilitySaga>(new UnusedRepositoryContextFactory());
        ConsumeContext<CapabilityMessage> context = InMemoryOutboxTestContextFactory.Create(
            new CapabilityMessage(),
            TestContext.Current.CancellationToken,
            correlationId: Guid.NewGuid());
        var query = new SagaQuery<CapabilitySaga>(_ => true);
        var policy = new UnusedSagaPolicy();

        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.SendAsync<CapabilityMessage>(null!, policy, null!))).ParamName);
        Assert.Equal("policy", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.SendAsync(context, null!, null!))).ParamName);
        Assert.Equal("next", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.SendAsync(context, policy, null!))).ParamName);

        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.SendQueryAsync<CapabilityMessage>(null!, query, policy, null!))).ParamName);
        Assert.Equal("query", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.SendQueryAsync(context, null!, policy, null!))).ParamName);
        Assert.Equal("policy", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.SendQueryAsync(context, query, null!, null!))).ParamName);
        Assert.Equal("next", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.SendQueryAsync(context, query, policy, null!))).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "load-and-query-boundaries-require-their-collaborators")]
    public async Task LoadAndQueryRepositories_RejectEveryMissingRequiredArgumentAsync()
    {
        var queryRepository = new QuerySagaRepository<CapabilitySaga>(new RecordingQueryFactory([]));

        Assert.Equal("repositoryContextFactory", Assert.Throws<ArgumentNullException>(() =>
            new LoadSagaRepository<CapabilitySaga>(null!)).ParamName);
        Assert.Equal("repositoryContextFactory", Assert.Throws<ArgumentNullException>(() =>
            new QuerySagaRepository<CapabilitySaga>(null!)).ParamName);
        Assert.Equal("query", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            queryRepository.FindAsync(null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            queryRepository.Probe(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new LoadSagaRepository<CapabilitySaga>(new RecordingLoadFactory(null)).Probe(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "dependency-injection-exposes-only-supported-capabilities")]
    public void InMemoryRegistration_DoesNotRegisterTheEndpointDispatchRepositoryAsAnApplicationService()
    {
        var services = new ServiceCollection();

        services.RegisterInMemorySagaRepository<CapabilitySaga>();

        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        Assert.Null(provider.GetService<ISagaRepository<CapabilitySaga>>());
        Assert.NotNull(provider.GetService<ILoadSagaRepository<CapabilitySaga>>());
        Assert.NotNull(provider.GetService<IQuerySagaRepository<CapabilitySaga>>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONFIGURATION", "registered-saga-requires-an-explicit-repository")]
    public void SagaRegistration_RejectsAMissingRepository()
    {
        ConfigurationException exception = Assert.Throws<ConfigurationException>(() => new ServiceCollection()
            .AddViciOneServiceBus(configuration => configuration.AddSaga<CapabilitySaga>()));

        Assert.Contains(nameof(CapabilitySaga), exception.Message, StringComparison.Ordinal);
        Assert.Contains("repository", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONFIGURATION", "repository-only-registration-participates-in-completion")]
    public void RepositoryOnlyRegistration_RejectsAMissingRepository()
    {
        ConfigurationException exception = Assert.Throws<ConfigurationException>(() => new ServiceCollection()
            .AddViciOneServiceBus(configuration => configuration.AddSagaRepository<CapabilitySaga>()));

        Assert.Contains(nameof(CapabilitySaga), exception.Message, StringComparison.Ordinal);
        Assert.Contains("repository", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class UnusedRepositoryContextFactory : ISagaRepositoryContextFactory<CapabilitySaga>
    {
        public Task SendAsync<T>(ConsumeContext<T> context, IPipe<SagaRepositoryContext<CapabilitySaga, T>> next)
            where T : class =>
            throw new NotSupportedException();

        public Task SendQueryAsync<T>(ConsumeContext<T> context, ISagaQuery<CapabilitySaga> query,
            IPipe<SagaRepositoryQueryContext<CapabilitySaga, T>> next)
            where T : class =>
            throw new NotSupportedException();

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class RecordingLoadFactory(CapabilitySaga? saga) :
        ILoadSagaRepositoryContextFactory<CapabilitySaga>
    {
        public Guid? CorrelationId { get; private set; }

        public CancellationToken FactoryCancellationToken { get; private set; }

        public CancellationToken OperationCancellationToken { get; private set; }

        public async Task<T?> ExecuteAsync<T>(
            Func<LoadSagaRepositoryContext<CapabilitySaga>, Task<T?>> asyncMethod,
            CancellationToken cancellationToken = default)
            where T : class
        {
            FactoryCancellationToken = cancellationToken;
            var context = new RecordingLoadContext(saga, cancellationToken);
            T? result = await asyncMethod(context);
            CorrelationId = context.CorrelationId;
            OperationCancellationToken = context.OperationCancellationToken;
            return result;
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class RecordingQueryFactory(IEnumerable<Guid> matches) :
        IQuerySagaRepositoryContextFactory<CapabilitySaga>
    {
        public ISagaQuery<CapabilitySaga>? Query { get; private set; }

        public CancellationToken FactoryCancellationToken { get; private set; }

        public CancellationToken OperationCancellationToken { get; private set; }

        public async Task<T> ExecuteAsync<T>(
            Func<QuerySagaRepositoryContext<CapabilitySaga>, Task<T>> asyncMethod,
            CancellationToken cancellationToken = default)
            where T : class
        {
            FactoryCancellationToken = cancellationToken;
            var context = new RecordingQueryContext(matches, cancellationToken);
            T result = await asyncMethod(context);
            Query = context.Query;
            OperationCancellationToken = context.OperationCancellationToken;
            return result;
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class RecordingLoadContext(CapabilitySaga? saga, CancellationToken cancellationToken) :
        BasePipeContext(cancellationToken),
        LoadSagaRepositoryContext<CapabilitySaga>
    {
        public Guid? CorrelationId { get; private set; }

        public CancellationToken OperationCancellationToken { get; private set; }

        public Task<CapabilitySaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
        {
            CorrelationId = correlationId;
            OperationCancellationToken = cancellationToken;
            return Task.FromResult(saga);
        }
    }

    private sealed class RecordingQueryContext(IEnumerable<Guid> matches, CancellationToken cancellationToken) :
        BasePipeContext(cancellationToken),
        SagaRepositoryQueryContext<CapabilitySaga>
    {
        private readonly IReadOnlyList<Guid> _matches = matches.ToArray();

        public int Count => _matches.Count;

        public ISagaQuery<CapabilitySaga>? Query { get; private set; }

        public CancellationToken OperationCancellationToken { get; private set; }

        public Task<SagaRepositoryQueryContext<CapabilitySaga>> QueryAsync(
            ISagaQuery<CapabilitySaga> query,
            CancellationToken cancellationToken = default)
        {
            Query = query;
            OperationCancellationToken = cancellationToken;
            return Task.FromResult<SagaRepositoryQueryContext<CapabilitySaga>>(this);
        }

        public IEnumerator<Guid> GetEnumerator() => _matches.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class UnusedSagaPolicy : ISagaPolicy<CapabilitySaga, CapabilityMessage>
    {
        public bool IsReadOnly => false;

        public bool PreInsertInstance(ConsumeContext<CapabilityMessage> context, [NotNullWhen(true)] out CapabilitySaga? instance)
        {
            instance = null;
            return false;
        }

        public Task ExistingAsync(
            SagaConsumeContext<CapabilitySaga, CapabilityMessage> context,
            IPipe<SagaConsumeContext<CapabilitySaga, CapabilityMessage>> next) =>
            throw new NotSupportedException();

        public Task MissingAsync(
            ConsumeContext<CapabilityMessage> context,
            IPipe<SagaConsumeContext<CapabilitySaga, CapabilityMessage>> next) =>
            throw new NotSupportedException();
    }

    public sealed class CapabilitySaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed record CapabilityMessage;
}
