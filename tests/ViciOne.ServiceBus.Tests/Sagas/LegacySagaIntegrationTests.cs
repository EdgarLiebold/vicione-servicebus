using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Logging;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas;

public sealed class LegacySagaIntegrationTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTION-EXTENSIONS", "debug-preserves-public-subscription-and-direct-connection")]
    public async Task SagaRegistration_DebugFailureDoesNotPreventPublicMessageDeliveryAsync(bool directConnection, bool loggerThrows)
    {
        TimeSpan timeout = OperationTimeout();
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var repository = new InMemorySagaRepository<FilteredSaga>();
        using var harness = CreateHarness("registration-debug", timeout);
        string template = directConnection ? "Connecting Saga: {SagaType}" : "Subscribing Saga: {SagaType}";
        var diagnosticFailure = new IOException("Saga registration diagnostic failure");
        var logger = new RegistrationDiagnosticLogger(template, loggerThrows ? diagnosticFailure : null);
        Exception? registrationFailure = null;
        ISagaConfigurator<FilteredSaga>? subscriptionConfigurator = null;
        int callbackCalls = 0;
        ConnectHandle? directHandle = null;
        Task? start = null;
        Task? send = null;
        Task<Guid?>? persisted = null;
        if (!directConnection)
        {
            harness.InMemoryReceiveEndpointConfiguring += endpoint =>
            {
                ILogContext? previous = LogContext.Current;
                try
                {
                    LogContext.ConfigureCurrentLogContext(logger);
                    registrationFailure = Record.Exception(() => endpoint.Saga(repository, configurator =>
                    {
                        callbackCalls++;
                        subscriptionConfigurator = configurator;
                    }));
                }
                finally
                {
                    LogContext.Current = previous;
                }
            };
        }
        try
        {
            start = harness.StartAsync(lifetime.Token);
            await start.WaitAsync(timeout, lifetime.Token);
            if (directConnection)
            {
                ILogContext? previous = LogContext.Current;
                try
                {
                    LogContext.ConfigureCurrentLogContext(logger);
                    registrationFailure = Record.Exception(() =>
                    {
                        directHandle = harness.Bus.ConnectSaga(repository);
                    });
                }
                finally
                {
                    LogContext.Current = previous;
                }
            }
            RegistrationDiagnosticLogger.Entry selected = Assert.Single(logger.Entries, entry => entry.Template == template);
            Assert.Equal(LogLevel.Debug, selected.Level);
            Assert.Equal(typeof(FilteredSaga).FullName, selected.SagaType);
            Assert.Equal(loggerThrows ? 1 : 0, logger.ThrowCount);
            if (loggerThrows)
            {
                Assert.Same(diagnosticFailure, logger.ThrownFailure);
                if (registrationFailure is not null)
                    Assert.Same(diagnosticFailure, registrationFailure);
            }
            else
                Assert.Null(logger.ThrownFailure);

            Assert.Null(registrationFailure);
            if (directConnection)
            {
                Assert.NotNull(directHandle);
                Assert.Equal(0, callbackCalls);
                Assert.Null(subscriptionConfigurator);
            }
            else
            {
                Assert.Equal(1, callbackCalls);
                Assert.IsType<SagaConfigurator<FilteredSaga>>(subscriptionConfigurator);
                Assert.Null(directHandle);
            }
            Guid correlationId = Guid.Parse("bdc03cd6-527d-46df-bba5-d2c4c0f0b707");
            send = directConnection
                ? harness.BusSendEndpoint.SendAsync(new FilteredStart(correlationId), lifetime.Token)
                : harness.InputQueueSendEndpoint.SendAsync(new FilteredStart(correlationId), lifetime.Token);
            await send.WaitAsync(timeout, lifetime.Token);
            persisted = ((ISagaRepository<FilteredSaga>)repository).WaitForSagaAsync(correlationId, timeout,
                cancellationToken: lifetime.Token);
            Assert.Equal(correlationId, await persisted.WaitAsync(timeout, lifetime.Token));
        }
        finally
        {
            try
            {
                lifetime.Cancel();
                if (send is not null)
                    await ObserveSagaRegistrationDiagnosticTaskAsync(send);
            }
            finally
            {
                try
                {
                    if (persisted is not null)
                        await ObserveSagaRegistrationDiagnosticTaskAsync(persisted);
                }
                finally
                {
                    try
                    {
                        if (start is not null)
                            await ObserveSagaRegistrationDiagnosticTaskAsync(start);
                    }
                    finally
                    {
                        try
                        {
                            directHandle?.Dispose();
                        }
                        finally
                        {
                            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
                        }
                    }
                }
            }
        }
        IConsumedMessage<FilteredStart> delivery = Assert.Single(harness.Consumed.Snapshot<FilteredStart>());
        Assert.Equal(Guid.Parse("bdc03cd6-527d-46df-bba5-d2c4c0f0b707"), delivery.Context.Message.CorrelationId);
        Assert.Null(delivery.Exception);
    }

    private static async Task ObserveSagaRegistrationDiagnosticTaskAsync(Task task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
        }
        catch (Exception failure) when (failure is not TimeoutException && (task.IsFaulted || task.IsCanceled))
        {
        }
    }

    private sealed class RegistrationDiagnosticLogger(string templateToThrow, Exception? failure) : ILogger
    {
        public sealed record Entry(LogLevel Level, string? Template, string? SagaType);
        public List<Entry> Entries { get; } = [];
        public int ThrowCount { get; private set; }
        public Exception? ThrownFailure { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => level == LogLevel.Debug;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            IEnumerable<KeyValuePair<string, object?>> fields = state as IEnumerable<KeyValuePair<string, object?>>
                ?? Array.Empty<KeyValuePair<string, object?>>();
            string? template = fields.FirstOrDefault(field => field.Key == "{OriginalFormat}").Value as string;
            string? sagaType = fields.FirstOrDefault(field => field.Key == "SagaType").Value as string;
            Entries.Add(new Entry(level, template, sagaType));
            if (template == templateToThrow && failure is not null)
            {
                ThrowCount++;
                ThrownFailure = failure;
                throw failure;
            }
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "saga-message-and-combined-filters-share-one-delivery")]
    public async Task SagaConfiguration_InvokesAllThreePipeLayersWithTheSameSagaAndMessageAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var repository = new InMemorySagaRepository<FilteredSaga>();
        var sagaLayer = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        var messageLayer = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        var combinedLayer = new TaskCompletionSource<(Guid SagaId, Guid MessageId)>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness("pipe-layers", timeout);
        harness.InMemoryReceiveEndpointConfiguring += endpoint => endpoint.Saga(repository, saga =>
        {
            saga.UseExecute(context => sagaLayer.TrySetResult(context.Saga.CorrelationId));
            saga.Message<FilteredStart>(message =>
                message.UseExecute(context => messageLayer.TrySetResult(context.Message.CorrelationId)));
            saga.SagaMessage<FilteredStart>(message => message.UseExecute(context =>
                combinedLayer.TrySetResult((context.Saga.CorrelationId, context.Message.CorrelationId))));
        });

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        Guid correlationId = NewId.NextGuid();
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new FilteredStart(correlationId), cancellationToken);

            Assert.Equal(correlationId, await sagaLayer.Task.WaitAsync(timeout, cancellationToken));
            Assert.Equal(correlationId, await messageLayer.Task.WaitAsync(timeout, cancellationToken));
            Assert.Equal((correlationId, correlationId), await combinedLayer.Task.WaitAsync(timeout, cancellationToken));
            Assert.Equal(correlationId, await ((ISagaRepository<FilteredSaga>)repository).WaitForSagaAsync(correlationId, timeout, cancellationToken: TestContext.Current.CancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(harness.Consumed.Snapshot<FilteredStart>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INITIATE-OR-ORCHESTRATE", "missing-and-existing-instance-matrix")]
    public async Task InitiatedByOrOrchestrates_CreatesAMissingInstanceAndReusesAnExistingInstanceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var repository = new InMemorySagaRepository<NewOrExistingSaga>();
        using var harness = CreateHarness("new-or-existing", timeout);
        harness.InMemoryReceiveEndpointConfiguring += endpoint => endpoint.Saga(repository);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        Guid initiatedByEvent = NewId.NextGuid();
        Guid createdThenOrchestrated = NewId.NextGuid();
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new EitherMessage(initiatedByEvent), cancellationToken);
            Assert.Equal(initiatedByEvent, await ((ISagaRepository<NewOrExistingSaga>)repository).WaitForSagaAsync(saga => saga.CorrelationId == initiatedByEvent && saga.EventCount == 1 && saga.CreateCount == 0, timeout, cancellationToken: TestContext.Current.CancellationToken));

            await harness.InputQueueSendEndpoint.SendAsync(new CreateMessage(createdThenOrchestrated), cancellationToken);
            Assert.Equal(createdThenOrchestrated, await ((ISagaRepository<NewOrExistingSaga>)repository).WaitForSagaAsync(saga => saga.CorrelationId == createdThenOrchestrated && saga.CreateCount == 1, timeout, cancellationToken: TestContext.Current.CancellationToken));
            await harness.InputQueueSendEndpoint.SendAsync(new EitherMessage(createdThenOrchestrated), cancellationToken);
            Assert.Equal(createdThenOrchestrated, await ((ISagaRepository<NewOrExistingSaga>)repository).WaitForSagaAsync(saga => saga.CorrelationId == createdThenOrchestrated && saga.CreateCount == 1 && saga.EventCount == 1, timeout, cancellationToken: TestContext.Current.CancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(2, harness.Consumed.Snapshot<EitherMessage>().Count());
        Assert.Single(harness.Consumed.Snapshot<CreateMessage>());
        Assert.Empty(harness.Published.Snapshot<Fault<EitherMessage>>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-INJECTION", "dependency-assigned-before-consume")]
    public async Task SagaExecuteFilter_AssignsTheExactDependencyBeforeTheInitiatingConsumeMethodRunsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var dependency = new SagaDependency(NewId.NextGuid());
        var repository = new InMemorySagaRepository<InjectedSaga>();
        using var harness = CreateHarness("injection", timeout);
        harness.InMemoryReceiveEndpointConfiguring += endpoint => endpoint.Saga(
            repository,
            saga => saga.UseExecute(context => context.Saga.Dependency = dependency));

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        Guid correlationId = NewId.NextGuid();
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new InjectedStart(correlationId), cancellationToken);
            Assert.Equal(correlationId, await ((ISagaRepository<InjectedSaga>)repository).WaitForSagaAsync(saga => saga.CorrelationId == correlationId && saga.DependencyObserved == dependency.Id, timeout, cancellationToken: TestContext.Current.CancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(harness.Consumed.Snapshot<InjectedStart>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-FILTER-EXPRESSION", "property-and-correlation-id-substitution")]
    public void SagaFilterExpressionConverter_SubstitutesTheMessageAndExcludesTheOtherInstance(bool useCorrelationId)
    {
        Guid matchingId = NewId.NextGuid();
        var message = new QueryMessage(matchingId, "matching");
        var matching = new QuerySaga { CorrelationId = matchingId, Name = "matching" };
        var other = new QuerySaga { CorrelationId = NewId.NextGuid(), Name = "other" };
        Expression<Func<QuerySaga, QueryMessage, bool>> selector = useCorrelationId
            ? (saga, current) => saga.CorrelationId == current.CorrelationId
            : (saga, current) => saga.Name == current.Name;

        Expression<Func<QuerySaga, bool>> converted =
            new SagaFilterExpressionConverter<QuerySaga, QueryMessage>(message).Convert(selector);
        Func<QuerySaga, bool> predicate = converted.Compile();

        Assert.True(predicate(matching));
        Assert.False(predicate(other));
        Assert.Equal(1, new[] { matching, other }.Count(predicate));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PROPERTY-QUERY", "non-generic-and-generic-value-extraction")]
    public void PropertyExpressionSagaQueryFactory_ExtractsTheExactValueThroughBothPublicOverloads()
    {
        var selector = new SagaQueryPropertySelector<QueryMessage, string>(context => context.Message.Name);
        var factory = new PropertyExpressionSagaQueryFactory<QuerySaga, QueryMessage, string>(
            saga => saga.Name,
            selector);
        ConsumeContext<QueryMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(
            new QueryMessage(NewId.NextGuid(), "Joe"),
            TestContext.Current.CancellationToken);

        Assert.True(factory.TryCreateQuery(consumeContext, out ISagaQuery<QuerySaga>? query));
        Assert.NotNull(query);
        Assert.True(query.TryGetPropertyValue(out object? untyped));
        Assert.Equal("Joe", untyped);
        Assert.True(query.TryGetPropertyValue<QuerySaga, string>(out string? typed));
        Assert.Equal("Joe", typed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "manual-create-destroy-finalize-lifecycle")]
    public async Task HandAssembledRepository_CreatesRoutesAndRemovesTheStateMachineInstanceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var machine = new RepositoryLifecycleMachine();
        ISagaRepository<RepositoryState> repository = CreateRepository<RepositoryState>();
        using var harness = CreateHarness("repository", timeout);
        harness.InMemoryReceiveEndpointConfiguring += endpoint => endpoint.StateMachineSaga(machine, repository);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        Guid correlationId = NewId.NextGuid();
        Task<IPublishedMessage<RepositoryCreated>> created = harness.Published
            .SelectAsync<RepositoryCreated>(message => message.Context.Message.CorrelationId == correlationId, cancellationToken)
            .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
        Task<IPublishedMessage<RepositoryDestroyed>> destroyed = harness.Published
            .SelectAsync<RepositoryDestroyed>(message => message.Context.Message.CorrelationId == correlationId, cancellationToken)
            .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new RepositoryCreate(correlationId), cancellationToken);
            Assert.Equal(correlationId, (await created.WaitAsync(timeout, cancellationToken)).Context.Message.CorrelationId);
            await harness.InputQueueSendEndpoint.SendAsync(new RepositoryDestroy(correlationId), cancellationToken);
            Assert.Equal(correlationId, (await destroyed.WaitAsync(timeout, cancellationToken)).Context.Message.CorrelationId);
            Assert.Equal(correlationId, await repository.WaitForSagaRemovalAsync(correlationId, timeout, cancellationToken: TestContext.Current.CancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(harness.Published.Snapshot<RepositoryCreated>());
        Assert.Single(harness.Published.Snapshot<RepositoryDestroyed>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "insert-on-initial-factory-finally-repeat")]
    public async Task HandAssembledInsertOnInitialRepository_FinalizesAndPublishesFinallyForEveryInstanceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var machine = new InsertOnInitialMachine();
        ISagaRepository<RepositoryState> repository = CreateRepository<RepositoryState>();
        using var harness = CreateHarness("insert-on-initial", timeout);
        harness.InMemoryReceiveEndpointConfiguring += endpoint => endpoint.StateMachineSaga(machine, repository);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        Guid[] ids = [NewId.NextGuid(), NewId.NextGuid()];
        try
        {
            foreach (Guid correlationId in ids)
            {
                Task<IPublishedMessage<RepositoryCreated>> created = harness.Published
                    .SelectAsync<RepositoryCreated>(message => message.Context.Message.CorrelationId == correlationId, cancellationToken)
                    .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
                Task<IPublishedMessage<RepositoryFinally>> finalized = harness.Published
                    .SelectAsync<RepositoryFinally>(message => message.Context.Message.CorrelationId == correlationId, cancellationToken)
                    .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

                await harness.InputQueueSendEndpoint.SendAsync(new RepositoryCreate(correlationId), cancellationToken);

                Assert.Equal(correlationId, (await created.WaitAsync(timeout, cancellationToken)).Context.Message.CorrelationId);
                Assert.Equal(correlationId, (await finalized.WaitAsync(timeout, cancellationToken)).Context.Message.CorrelationId);
                Assert.Equal(correlationId, await repository.WaitForSagaRemovalAsync(correlationId, timeout, cancellationToken: TestContext.Current.CancellationToken));
            }
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(2, harness.Published.Snapshot<RepositoryCreated>().Count());
        Assert.Equal(2, harness.Published.Snapshot<RepositoryFinally>().Count());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-NEW-POLICY", "duplicate-initiating-message-publishes-one-fault")]
    public async Task DuplicateInitiatingMessage_PreservesTheInstanceAndPublishesOneTypedFaultAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var repository = new InMemorySagaRepository<DuplicateSaga>();
        using var harness = CreateHarness("duplicate", timeout);
        harness.InMemoryReceiveEndpointConfiguring += endpoint => endpoint.Saga(repository);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        Guid correlationId = NewId.NextGuid();
        Task<IPublishedMessage<Fault<DuplicateStart>>> fault = harness.Published
            .SelectAsync<Fault<DuplicateStart>>(
                message => message.Context.Message.Message.CorrelationId == correlationId,
                cancellationToken)
            .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new DuplicateStart(correlationId), cancellationToken);
            Assert.Equal(correlationId, await ((ISagaRepository<DuplicateSaga>)repository).WaitForSagaAsync(correlationId, timeout, cancellationToken: TestContext.Current.CancellationToken));
            await harness.InputQueueSendEndpoint.SendAsync(new DuplicateStart(correlationId), cancellationToken);

            IPublishedMessage<Fault<DuplicateStart>> faultContext = await fault.WaitAsync(timeout, cancellationToken);
            Assert.Equal(correlationId, faultContext.Context.Message.Message.CorrelationId);
            Assert.Equal(correlationId, await ((ISagaRepository<DuplicateSaga>)repository).WaitForSagaAsync(correlationId, timeout, cancellationToken: TestContext.Current.CancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(harness.Published.Snapshot<Fault<DuplicateStart>>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "duplicate-initiate-reports-typed-saga-exception")]
    public async Task DirectBusSagaConnection_ReportsTheTypedSagaExceptionForAnExistingInstanceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var repository = new InMemorySagaRepository<DuplicateSaga>();
        using var harness = CreateHarness("direct-connect", timeout);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        using ConnectHandle handle = harness.Bus.ConnectSaga(repository);
        Guid correlationId = NewId.NextGuid();
        try
        {
            await harness.BusSendEndpoint.SendAsync(new DuplicateStart(correlationId), cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            Assert.Equal(correlationId, await ((ISagaRepository<DuplicateSaga>)repository).WaitForSagaAsync(correlationId, timeout, cancellationToken: TestContext.Current.CancellationToken));

            await harness.BusSendEndpoint.SendAsync(new DuplicateStart(correlationId), cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            int deliveryCount = await harness.Consumed.SelectAsync<DuplicateStart>(cancellationToken)
                .Take(2)
                .CountObservedAsync(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, cancellationToken);
            Assert.Equal(2, deliveryCount);
            Assert.Equal(correlationId, await ((ISagaRepository<DuplicateSaga>)repository).WaitForSagaAsync(correlationId, timeout, cancellationToken: TestContext.Current.CancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        IConsumedMessage<DuplicateStart>[] deliveries =
            harness.Consumed.Snapshot<DuplicateStart>().ToArray();
        Assert.Equal(2, deliveries.Length);
        Assert.Single(deliveries, delivery => delivery.Exception is null);
        SagaException exception = Assert.IsType<SagaException>(
            Assert.Single(deliveries, delivery => delivery.Exception is not null).Exception);
        Assert.Equal(typeof(DuplicateStart), exception.MessageType);
    }

    private static ISagaRepository<TSaga> CreateRepository<TSaga>()
        where TSaga : class, ISaga
    {
        var dictionary = new IndexedSagaDictionary<TSaga>();
        ISagaConsumeContextFactory<IndexedSagaDictionary<TSaga>, TSaga> consumeContextFactory =
            new InMemorySagaConsumeContextFactory<TSaga>();
        var contextFactory = new InMemorySagaRepositoryContextFactory<TSaga>(dictionary, consumeContextFactory);
        return SagaRepository<TSaga>.CreateQueryable(contextFactory, contextFactory, contextFactory);
    }

    private static InMemoryTestHarness CreateHarness(string suffix, TimeSpan timeout) =>
        new($"legacy-saga-{suffix}-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions().OperationTimeout!.Value;


    public sealed record FilteredStart(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed class FilteredSaga : ISaga, IInitiatedBy<FilteredStart>
    {
        public Guid CorrelationId { get; set; }

        public Task ConsumeAsync(ConsumeContext<FilteredStart> context) => Task.CompletedTask;
    }

    public sealed record CreateMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record EitherMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed class NewOrExistingSaga :
        ISaga,
        IInitiatedBy<CreateMessage>,
        IInitiatedByOrOrchestrates<EitherMessage>
    {
        public NewOrExistingSaga(Guid correlationId) => CorrelationId = correlationId;

        public Guid CorrelationId { get; set; }

        public int CreateCount { get; private set; }

        public int EventCount { get; private set; }

        public Task ConsumeAsync(ConsumeContext<CreateMessage> context)
        {
            CreateCount++;
            return Task.CompletedTask;
        }

        public Task ConsumeAsync(ConsumeContext<EitherMessage> context)
        {
            EventCount++;
            return Task.CompletedTask;
        }
    }

    public sealed record SagaDependency(Guid Id);

    public sealed record InjectedStart(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed class InjectedSaga : ISaga, IInitiatedBy<InjectedStart>
    {
        public Guid CorrelationId { get; set; }

        public SagaDependency? Dependency { get; set; }

        public Guid? DependencyObserved { get; private set; }

        public Task ConsumeAsync(ConsumeContext<InjectedStart> context)
        {
            DependencyObserved = Dependency?.Id;
            return Task.CompletedTask;
        }
    }

    public sealed record QueryMessage(Guid CorrelationId, string Name);

    public sealed class QuerySaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public sealed record RepositoryCreate(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record RepositoryDestroy(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record RepositoryCreated(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record RepositoryDestroyed(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record RepositoryFinally(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed class RepositoryState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;
    }

    public sealed class RepositoryLifecycleMachine : ViciOneServiceBusStateMachine<RepositoryState>
    {
        public RepositoryLifecycleMachine()
        {
            InstanceState(instance => instance.CurrentState);
            SetCompletedWhenFinalized();
            Initially(When(Created)
                .Publish(context => new RepositoryCreated(context.Saga.CorrelationId))
                .TransitionTo(Active));
            During(Active, When(Destroyed)
                .Publish(context => new RepositoryDestroyed(context.Saga.CorrelationId))
                .Finalize());
        }

        public IState Active { get; private set; } = null!;

        public IEvent<RepositoryCreate> Created { get; private set; } = null!;

        public IEvent<RepositoryDestroy> Destroyed { get; private set; } = null!;
    }

    public sealed class InsertOnInitialMachine : ViciOneServiceBusStateMachine<RepositoryState>
    {
        public InsertOnInitialMachine()
        {
            InstanceState(instance => instance.CurrentState);
            SetCompletedWhenFinalized();
            Event(() => Created, configuration =>
            {
                configuration.InsertOnInitial = true;
                configuration.SetSagaFactory(context => new RepositoryState
                {
                    CorrelationId = context.Message.CorrelationId,
                });
            });
            Initially(When(Created)
                .Publish(context => new RepositoryCreated(context.Saga.CorrelationId))
                .TransitionTo(Active)
                .ThenAwaited(context => context.RaiseAsync(Destroyed)));
            During(Active, When(Destroyed).Finalize());
            Finally(binder => binder.Publish(context => new RepositoryFinally(context.Saga.CorrelationId)));
        }

        public IState Active { get; private set; } = null!;

        public IEvent<RepositoryCreate> Created { get; private set; } = null!;

        public IEvent Destroyed { get; private set; } = null!;
    }

    public sealed record DuplicateStart(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed class DuplicateSaga : ISaga, IInitiatedBy<DuplicateStart>
    {
        public Guid CorrelationId { get; set; }

        public Task ConsumeAsync(ConsumeContext<DuplicateStart> context) => Task.CompletedTask;
    }
}
