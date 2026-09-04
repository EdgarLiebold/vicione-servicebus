using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DynamoDbIntegration.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.DynamoDbIntegration.Saga;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.DynamoDbIntegration.LocalIntegration.Tests.DynamoDbIntegration.Saga;

public sealed class DynamoDbSagaConcurrencyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-CONCURRENCY", "one-winner-one-exact-conflict-and-version-restored")]
    public async Task OptimisticConflict_PreservesOneWinnerAndOneExactConflictAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DynamoDbTestTable fixture = await DynamoDbTestTable.CreateAsync("OptimisticConflict", cancellationToken);
        var options = new DynamoDbSagaRepositoryOptions<ConcurrentSaga>(fixture.TableName);
        Guid sagaId = Guid.NewGuid();

        using (var seed = new DynamoDbDatabaseContext<ConcurrentSaga>(fixture.CreateContext(), options))
            await seed.InsertAsync(new ConcurrentSaga { CorrelationId = sagaId, Value = "seed" }, cancellationToken);

        ConcurrentSaga first;
        ConcurrentSaga second;
        using (var loader = new DynamoDbDatabaseContext<ConcurrentSaga>(fixture.CreateContext(), options))
        {
            first = Assert.IsType<ConcurrentSaga>(await loader.LoadAsync(sagaId, cancellationToken));
            second = Assert.IsType<ConcurrentSaga>(await loader.LoadAsync(sagaId, cancellationToken));
        }

        first.Value = "first";
        second.Value = "second";
        using var firstWriter = new DynamoDbDatabaseContext<ConcurrentSaga>(fixture.CreateContext(), options);
        using var secondWriter = new DynamoDbDatabaseContext<ConcurrentSaga>(fixture.CreateContext(), options);
        Task<Exception?> firstAttempt = CaptureAsync(() => firstWriter.UpdateAsync(first, cancellationToken));
        Task<Exception?> secondAttempt = CaptureAsync(() => secondWriter.UpdateAsync(second, cancellationToken));
        Exception?[] outcomes = await Task.WhenAll(firstAttempt, secondAttempt);

        Exception conflict = Assert.Single(outcomes, outcome => outcome is not null)!;
        DynamoDbSagaConcurrencyException concurrency = Assert.IsType<DynamoDbSagaConcurrencyException>(conflict);
        Assert.IsType<ConditionalCheckFailedException>(concurrency.InnerException);
        Assert.Single(outcomes, outcome => outcome is null);
        ConcurrentSaga losingInstance = ReferenceEquals(conflict, await firstAttempt) ? first : second;
        ConcurrentSaga winningInstance = ReferenceEquals(losingInstance, first) ? second : first;
        Assert.Equal(0, losingInstance.Version);
        Assert.Equal(1, winningInstance.Version);

        using var verifier = new DynamoDbDatabaseContext<ConcurrentSaga>(fixture.CreateContext(), options);
        ConcurrentSaga persisted = Assert.IsType<ConcurrentSaga>(await verifier.LoadAsync(sagaId, cancellationToken));
        Assert.Equal(1, persisted.Version);
        Assert.Equal(winningInstance.Value, persisted.Value);
        Assert.NotEqual(losingInstance.Value, persisted.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-CONCURRENCY", "conditional-write-failure-maps-to-exact-concurrency-error")]
    public async Task ConditionalFailure_MapsToExactSagaConcurrencyExceptionAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DynamoDbTestTable fixture = await DynamoDbTestTable.CreateAsync("ConditionalFailure", cancellationToken);
        var options = new DynamoDbSagaRepositoryOptions<ConcurrentSaga>(fixture.TableName);
        Guid sagaId = Guid.NewGuid();

        using var context = new DynamoDbDatabaseContext<ConcurrentSaga>(fixture.CreateContext(), options);
        await context.InsertAsync(new ConcurrentSaga { CorrelationId = sagaId, Value = "first" }, cancellationToken);
        DynamoDbSagaConcurrencyException actual = await Assert.ThrowsAsync<DynamoDbSagaConcurrencyException>(
            () => context.InsertAsync(new ConcurrentSaga { CorrelationId = sagaId, Value = "duplicate" }, cancellationToken));

        Assert.Equal(sagaId, actual.CorrelationId);
        Assert.Equal(typeof(ConcurrentSaga), actual.SagaType);
        Assert.IsType<ConditionalCheckFailedException>(actual.InnerException);
        Dictionary<string, AttributeValue> persisted = Assert.Single(await fixture.ScanAsync(cancellationToken));
        Assert.Contains("first", persisted[nameof(DynamoDbSaga.Properties)].S, StringComparison.Ordinal);
        Assert.DoesNotContain("duplicate", persisted[nameof(DynamoDbSaga.Properties)].S, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-CONCURRENCY", "stale-delete-cannot-remove-newer-saga-version")]
    public async Task StaleDelete_PreservesTheNewerSagaVersionAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DynamoDbTestTable fixture = await DynamoDbTestTable.CreateAsync("StaleDelete", cancellationToken);
        var options = new DynamoDbSagaRepositoryOptions<ConcurrentSaga>(fixture.TableName);
        Guid sagaId = Guid.NewGuid();

        using var context = new DynamoDbDatabaseContext<ConcurrentSaga>(fixture.CreateContext(), options);
        await context.InsertAsync(new ConcurrentSaga { CorrelationId = sagaId, Value = "seed" }, cancellationToken);
        ConcurrentSaga stale = Assert.IsType<ConcurrentSaga>(await context.LoadAsync(sagaId, cancellationToken));
        ConcurrentSaga current = Assert.IsType<ConcurrentSaga>(await context.LoadAsync(sagaId, cancellationToken));
        current.Value = "newer";
        await context.UpdateAsync(current, cancellationToken);

        DynamoDbSagaConcurrencyException conflict = await Assert.ThrowsAsync<DynamoDbSagaConcurrencyException>(
            () => context.DeleteAsync(stale, cancellationToken));

        Assert.Equal(sagaId, conflict.CorrelationId);
        Assert.Equal(typeof(ConcurrentSaga), conflict.SagaType);
        Assert.IsType<ConditionalCheckFailedException>(conflict.InnerException);
        ConcurrentSaga persisted = Assert.IsType<ConcurrentSaga>(await context.LoadAsync(sagaId, cancellationToken));
        Assert.Equal(1, persisted.Version);
        Assert.Equal("newer", persisted.Value);

        await context.DeleteAsync(current, cancellationToken);
        Assert.Empty(await fixture.ScanAsync(cancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-CONCURRENCY", "concurrent-choir-updates-persist-every-distinct-voice-once")]
    public async Task ConcurrentChoirUpdates_PersistEveryDistinctVoiceOnceAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DynamoDbTestTable fixture = await DynamoDbTestTable.CreateAsync("ConcurrentChoir", cancellationToken);
        var probe = new ChoirConcurrencyProbe(fixture.OperationTimeout);
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(probe)
            .AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
            {
                configuration.SetTestTimeouts(fixture.OperationTimeout, fixture.OperationTimeout);
                configuration.AddSagaStateMachine<ChoirStateMachine, ChoirSaga, ChoirSagaDefinition>()
                    .DynamoDbRepository(repository =>
                    {
                        repository.TableName = fixture.TableName;
                        repository.ContextFactory(fixture.CreateContext);
                    });
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);

        try
        {
            Guid sagaId = Guid.NewGuid();
            ISendEndpoint endpoint = await harness.GetSagaEndpointAsync<ChoirSaga>(TestContext.Current.CancellationToken);
            await endpoint.SendAsync(new BeginChoir(sagaId), cancellationToken);
            await harness.Published.SelectAsync<ChoirStarted>(
                    observed => observed.Context.Message.CorrelationId == sagaId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            string[] voices = ["Bass", "Baritone", "Tenor", "Countertenor"];
            await Task.WhenAll(voices.Select(voice => endpoint.SendAsync(new AddChoirVoice(sagaId, voice), cancellationToken)));
            await probe.InitialAttemptsEntered.WaitAsync(fixture.OperationTimeout, cancellationToken);
            foreach (string voice in voices)
            {
                await harness.Published.SelectAsync<ChoirVoiceRecorded>(
                        observed => observed.Context.Message.CorrelationId == sagaId && observed.Context.Message.Voice == voice,
                        cancellationToken)
                    .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            }

            var repository = (ILoadSagaRepository<ChoirSaga>)DynamoDbSagaRepository<ChoirSaga>
                .Create(fixture.CreateContext, fixture.TableName);
            ChoirSaga persisted = Assert.IsType<ChoirSaga>(
                await repository.LoadAsync(sagaId, TestContext.Current.CancellationToken));
            using var completed = new CancellationTokenSource();
            completed.Cancel();
            ChoirVoiceRecorded[] published = harness.Published
                .Select<ChoirVoiceRecorded>(completed.Token)
                .Where(observed => observed.Context.Message.CorrelationId == sagaId)
                .Select(observed => observed.Context.Message)
                .ToArray();

            Assert.Equal(voices.Order(StringComparer.Ordinal), persisted.Voices.Order(StringComparer.Ordinal));
            Assert.Equal(voices.Order(StringComparer.Ordinal), published.Select(message => message.Voice).Order(StringComparer.Ordinal));
            Assert.Equal(voices.Length, published.Length);
            Assert.True(probe.InvocationCount >= voices.Length);
            Assert.Equal(voices.Length, persisted.Version);
        }
        finally
        {
            probe.Release();
            await harness.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static async Task<Exception?> CaptureAsync(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    public sealed class ConcurrentSaga : ISagaVersion
    {
        public Guid CorrelationId { get; set; }

        public int Version { get; set; }

        public string Value { get; set; } = string.Empty;
    }

    public sealed record BeginChoir(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record AddChoirVoice(Guid CorrelationId, string Voice) : CorrelatedBy<Guid>;

    public sealed record ChoirStarted(Guid CorrelationId);

    public sealed record ChoirVoiceRecorded(Guid CorrelationId, string Voice);

    public sealed class ChoirSaga : SagaStateMachineInstance, ISagaVersion
    {
        public Guid CorrelationId { get; set; }

        public int Version { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public List<string> Voices { get; set; } = [];
    }

    public sealed class ChoirStateMachine : ViciOneServiceBusStateMachine<ChoirSaga>
    {
        public ChoirStateMachine(ChoirConcurrencyProbe probe)
        {
            ArgumentNullException.ThrowIfNull(probe);

            InstanceState(saga => saga.CurrentState);
            Event(() => Begin, configuration => configuration.CorrelateById(context => context.Message.CorrelationId));
            Event(() => AddVoice, configuration => configuration.CorrelateById(context => context.Message.CorrelationId));

            Initially(
                When(Begin)
                    .Publish(context => new ChoirStarted(context.Saga.CorrelationId))
                    .TransitionTo(Active));
            During(
                Active,
                When(AddVoice)
                    .ThenAsync(context => probe.EnterAsync(context.CancellationToken))
                    .Then(context =>
                    {
                        if (!context.Saga.Voices.Contains(context.Message.Voice, StringComparer.Ordinal))
                            context.Saga.Voices.Add(context.Message.Voice);
                    })
                    .Publish(context => new ChoirVoiceRecorded(context.Saga.CorrelationId, context.Message.Voice)));
        }

        public State Active { get; private set; } = null!;

        public Event<BeginChoir> Begin { get; private set; } = null!;

        public Event<AddChoirVoice> AddVoice { get; private set; } = null!;
    }

    private sealed class ChoirSagaDefinition : SagaDefinition<ChoirSaga>
    {
        public ChoirSagaDefinition()
        {
            ConcurrentMessageLimit = 4;
        }

        protected override void ConfigureSaga(
            IReceiveEndpointConfigurator endpointConfigurator,
            ISagaConfigurator<ChoirSaga> sagaConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.ConcurrentMessageLimit = 4;
            sagaConfigurator.UseMessageRetry(retry => retry.Immediate(8));
            sagaConfigurator.UseInMemoryOutbox(context);
        }
    }

    public sealed class ChoirConcurrencyProbe(TimeSpan timeout)
    {
        private readonly TaskCompletionSource _initialAttemptsEntered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _invocationCount;

        public Task InitialAttemptsEntered => _initialAttemptsEntered.Task;

        public int InvocationCount => Volatile.Read(ref _invocationCount);

        public async Task EnterAsync(CancellationToken cancellationToken)
        {
            int invocation = Interlocked.Increment(ref _invocationCount);
            if (invocation > 4)
                return;

            if (invocation == 4)
            {
                _initialAttemptsEntered.TrySetResult();
                _release.TrySetResult();
            }

            await _release.Task.WaitAsync(timeout, cancellationToken);
        }

        public void Release() => _release.TrySetResult();
    }
}
