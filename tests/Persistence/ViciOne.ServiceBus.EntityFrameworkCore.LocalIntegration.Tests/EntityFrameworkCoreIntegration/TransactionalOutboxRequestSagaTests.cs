using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.InMemoryTransport;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.EntityFrameworkCoreIntegration;

[Collection(EntityFrameworkOpenTelemetryCollection.Name)]
public sealed class TransactionalOutboxRequestSagaTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-SAGA-REQUEST", "failed-request-surfaces-original-saga-fault")]
    public async Task FailedSagaRequest_ReturnsARequestFaultWithTheOriginalExceptionAsync()
    {
        await using RequestSagaFixture fixture = await RequestSagaFixture.CreateAsync();
        var request = new StartRequestSaga(Guid.NewGuid(), Fail: true);

        RequestFaultException exception = await Assert.ThrowsAsync<RequestFaultException>(() =>
            fixture.Client.GetResponseAsync<RequestSagaStarted>(request, fixture.CancellationToken));
        Fault<StartRequestSaga> fault = Assert.IsAssignableFrom<Fault<StartRequestSaga>>(exception.Fault);

        ExceptionInfo outer = Assert.Single(fault.Exceptions);
        ExceptionInfo original = Assert.Single(Flatten(outer), item =>
            item.ExceptionType == TypeCache<ExpectedSagaFailure>.ShortName);
        Assert.Equal(TypeCache<StartRequestSaga>.ShortName, exception.RequestType);
        Assert.Equal(TypeCache<ExpectedSagaFailure>.ShortName, original.ExceptionType);
        Assert.Equal(ExpectedSagaFailure.FailureMessage, original.Message);
        Assert.Equal(request.CorrelationId, fault.Message.CorrelationId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-SAGA-FAULT", "retries-emit-one-terminal-fault")]
    public async Task RetriedSagaFailure_EmitsOneTerminalFaultAfterAllAttemptsAsync()
    {
        await using RequestSagaFixture fixture = await RequestSagaFixture.CreateAsync();
        var request = new StartRequestSaga(Guid.NewGuid(), Fail: true);

        await Assert.ThrowsAsync<RequestFaultException>(() =>
            fixture.Client.GetResponseAsync<RequestSagaStarted>(request, fixture.CancellationToken));
        using var snapshot = new CancellationTokenSource();
        snapshot.Cancel();
        ISentMessage<Fault<StartRequestSaga>>[] faults = fixture.Harness.Sent
            .Select<Fault<StartRequestSaga>>(snapshot.Token)
            .Where(message => message.Context.Message.Message.CorrelationId == request.CorrelationId)
            .ToArray();

        Assert.Equal(3, fixture.Attempts.For(request.CorrelationId));
        Assert.Single(faults);
        Assert.Single(faults[0].Context.Message.Exceptions);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-SAGA-OBSERVABILITY", "successful-request-emits-saga-activity")]
    public async Task SuccessfulSagaRequest_RespondsUnderTheExactOpenTelemetrySagaActivityAsync()
    {
        var activities = new ConcurrentQueue<ActivitySnapshot>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => activities.Enqueue(new ActivitySnapshot(
                activity.Source.Name,
                activity.Kind,
                activity.TagObjects.ToArray())),
        };
        ActivitySource.AddActivityListener(listener);
        await using RequestSagaFixture fixture = await RequestSagaFixture.CreateAsync();
        var request = new StartRequestSaga(Guid.NewGuid(), Fail: false);

        Response<RequestSagaStarted> response = await fixture.Client.GetResponseAsync<RequestSagaStarted>(
            request,
            fixture.CancellationToken);
        ActivitySnapshot sagaActivity = Assert.Single(activities, activity =>
            Equals(activity.Tag(DiagnosticHeaders.SagaId), request.CorrelationId.ToString("D")));

        Assert.Equal(request.CorrelationId, response.Message.CorrelationId);
        Assert.Equal(ServiceBusTelemetry.ActivitySourceName, sagaActivity.SourceName);
        Assert.Equal(ActivityKind.Consumer, sagaActivity.Kind);
        Assert.Equal("process", sagaActivity.Tag(DiagnosticHeaders.Messaging.Operation));
        Assert.Equal(request.CorrelationId.ToString("D"), sagaActivity.Tag(DiagnosticHeaders.CorrelationId));
        Assert.False(string.IsNullOrWhiteSpace(Assert.IsType<string>(sagaActivity.Tag(DiagnosticHeaders.RequestId))));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-SAGA-SCOPE", "state-machine-activity-receives-correlation-proxies")]
    public async Task SuccessfulSagaRequest_ProvidesCorrelationScopedEndpointsToTheStateMachineActivityAsync()
    {
        await using RequestSagaFixture fixture = await RequestSagaFixture.CreateAsync();
        var request = new StartRequestSaga(Guid.NewGuid(), Fail: false);

        Response<RequestSagaStarted> response = await fixture.Client.GetResponseAsync<RequestSagaStarted>(
            request,
            fixture.CancellationToken);
        ScopeProxyObservation observation = await fixture.ScopeProxies.ReadAsync(
            fixture.OperationTimeout,
            fixture.CancellationToken);

        Assert.Equal(request.CorrelationId, response.Message.CorrelationId);
        Assert.Equal(request.CorrelationId, observation.CorrelationId);
        Assert.Equal(typeof(CorrelationIdConsumeContextProxy<StartRequestSaga>), observation.PublishEndpointType);
        Assert.Equal(typeof(CorrelationIdConsumeContextProxy<StartRequestSaga>), observation.SendEndpointProviderType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-SAGA-QUERY", "query-correlation-reuses-the-outbox-transaction")]
    public async Task QueryCorrelatedSaga_ReusesTheEntityFrameworkOutboxTransactionAsync()
    {
        await using RequestSagaFixture fixture = await RequestSagaFixture.CreateAsync();
        Guid sagaId = Guid.NewGuid();
        string lookupKey = $"request-{sagaId:N}";

        Response<RequestSagaStarted> started = await fixture.Client.GetResponseAsync<RequestSagaStarted>(
            new StartRequestSaga(sagaId, Fail: false, LookupKey: lookupKey),
            fixture.CancellationToken);
        IRequestClient<QueryRequestSaga> queryClient = fixture.Harness.GetRequestClient<QueryRequestSaga>();
        Response<RequestSagaFound> found = await queryClient.GetResponseAsync<RequestSagaFound>(
            new QueryRequestSaga(lookupKey),
            fixture.CancellationToken);

        Assert.Equal(sagaId, started.Message.CorrelationId);
        Assert.Equal(sagaId, found.Message.CorrelationId);
        Assert.Equal(lookupKey, found.Message.LookupKey);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-SAGA-SCHEDULE", "delayed-response-preserves-response-address-and-request-id")]
    public async Task DelayedSagaResponse_PreservesTheRequestIdentityAcrossTheOutboxScheduleAsync()
    {
        await using RequestSagaFixture fixture = await RequestSagaFixture.CreateAsync();
        TimeSpan delay = TimeSpan.FromHours(2);
        var request = new StartRequestSaga(Guid.NewGuid(), Fail: false, Delay: delay);
        Task<Response<RequestSagaStarted>> responseTask = fixture.Client.GetResponseAsync<RequestSagaStarted>(
            request,
            fixture.CancellationToken);
        IReceivedMessage<StartRequestSaga> consumed = await fixture.Harness.Consumed
            .SelectAsync<StartRequestSaga>(
                context => context.Context.Message.CorrelationId == request.CorrelationId,
                fixture.CancellationToken)
            .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, fixture.CancellationToken);
        IInMemoryDelayProvider delayProvider = fixture.Services.GetRequiredService<IInMemoryDelayProvider>();

        Assert.False(responseTask.IsCompleted);
        delayProvider.Advance(delay - TimeSpan.FromTicks(1));
        Assert.False(responseTask.IsCompleted);
        delayProvider.Advance(TimeSpan.FromTicks(1));
        Response<RequestSagaStarted> response = await responseTask.WaitAsync(
            fixture.OperationTimeout,
            fixture.CancellationToken);

        await using AsyncServiceScope verificationScope = fixture.Services.CreateAsyncScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<RequestSagaDbContext>();
        RequestSagaState state = await dbContext.States.AsNoTracking()
            .SingleAsync(item => item.CorrelationId == request.CorrelationId, fixture.CancellationToken);

        Assert.Null(consumed.Exception);
        Assert.Equal(request.CorrelationId, consumed.Context.Message.CorrelationId);
        Assert.Equal(request.CorrelationId, response.Message.CorrelationId);
        Assert.NotNull(response.RequestId);
        Assert.Equal(response.RequestId, state.RequestId);
        Assert.Equal(response.DestinationAddress, state.ResponseAddress);
        Assert.Equal(RequestSagaStateMachine.RunningStateName, state.CurrentState);
    }

    public sealed record StartRequestSaga(
        Guid CorrelationId,
        bool Fail,
        TimeSpan? Delay = null,
        string? LookupKey = null) : CorrelatedBy<Guid>;

    public sealed record RequestSagaStarted(Guid CorrelationId);

    public sealed record QueryRequestSaga(string LookupKey);

    public sealed record RequestSagaFound(Guid CorrelationId, string LookupKey);

    public sealed record ResumeRequestSaga(Guid CorrelationId) : CorrelatedBy<Guid>;

    private static IEnumerable<ExceptionInfo> Flatten(ExceptionInfo exception)
    {
        for (ExceptionInfo? current = exception; current is not null; current = current.InnerException)
            yield return current;
    }

    public sealed class ExpectedSagaFailure : Exception
    {
        public const string FailureMessage = "The request saga failed as requested.";

        public ExpectedSagaFailure() : base(FailureMessage)
        {
        }
    }

    public sealed class RequestSagaState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public Guid? RequestId { get; set; }

        public Uri? ResponseAddress { get; set; }

        public Guid? ResumeTokenId { get; set; }

        public string LookupKey { get; set; } = string.Empty;
    }

    public sealed class RequestSagaStateMachine : ViciOneServiceBusStateMachine<RequestSagaState>
    {
        public const string RunningStateName = "Running";

        public RequestSagaStateMachine(RequestAttemptProbe attempts)
        {
            InstanceState(state => state.CurrentState);
            Event(() => Started, configuration => configuration.CorrelateById(context => context.Message.CorrelationId));
            Event(() => Queried, configuration =>
                configuration.CorrelateBy((instance, context) => instance.LookupKey == context.Message.LookupKey));
            Schedule(
                () => Resume,
                state => state.ResumeTokenId,
                configuration => configuration.Received = eventConfiguration =>
                    eventConfiguration.CorrelateById(context => context.Message.CorrelationId));
            Initially(
                When(Started, context => context.Message.Fail)
                    .Then(context =>
                    {
                        attempts.Increment(context.Message.CorrelationId);
                        throw new ExpectedSagaFailure();
                    }),
                When(Started, context => !context.Message.Fail && !context.Message.Delay.HasValue)
                    .Then(context =>
                    {
                        attempts.Increment(context.Message.CorrelationId);
                        context.Saga.LookupKey = context.Message.LookupKey ?? $"request-{context.Message.CorrelationId:N}";
                    })
                    .Activity(activity => activity.OfType<ScopeProxyActivity>())
                    .Respond(context => new RequestSagaStarted(context.Saga.CorrelationId))
                    .TransitionTo(Running),
                When(Started, context => !context.Message.Fail && context.Message.Delay.HasValue)
                    .Then(context =>
                    {
                        attempts.Increment(context.Message.CorrelationId);
                        context.Saga.RequestId = context.RequestId;
                        context.Saga.ResponseAddress = context.ResponseAddress;
                    })
                    .Schedule(
                        Resume,
                        context => new ResumeRequestSaga(context.Saga.CorrelationId),
                        context => context.Message.Delay!.Value)
                    .TransitionTo(Delayed));
            During(
                Running,
                When(Queried)
                    .Respond(context => new RequestSagaFound(context.Saga.CorrelationId, context.Saga.LookupKey)));
            During(
                Delayed,
                When(Resume.Received)
                    .Send(
                        context => context.Saga.ResponseAddress!,
                        context => new RequestSagaStarted(context.Saga.CorrelationId),
                        (context, send) => send.RequestId = context.Saga.RequestId)
                    .TransitionTo(Running));
        }

        public State Running { get; private set; } = null!;

        public State Delayed { get; private set; } = null!;

        public Event<StartRequestSaga> Started { get; private set; } = null!;

        public Event<QueryRequestSaga> Queried { get; private set; } = null!;

        public Schedule<RequestSagaState, ResumeRequestSaga> Resume { get; private set; } = null!;
    }

    public sealed class ScopeProxyActivity(
        IPublishEndpoint publishEndpoint,
        ISendEndpointProvider sendEndpointProvider,
        ScopeProxyProbe observations) : IStateMachineActivity<RequestSagaState, StartRequestSaga>
    {
        public Task ExecuteAsync(
            BehaviorContext<RequestSagaState, StartRequestSaga> context,
            IBehavior<RequestSagaState, StartRequestSaga> next)
        {
            observations.Record(new ScopeProxyObservation(
                context.Message.CorrelationId,
                publishEndpoint.GetType(),
                sendEndpointProvider.GetType()));
            return next.ExecuteAsync(context);
        }

        public Task FaultedAsync<TException>(
            BehaviorExceptionContext<RequestSagaState, StartRequestSaga, TException> context,
            IBehavior<RequestSagaState, StartRequestSaga> next)
            where TException : Exception => next.FaultedAsync(context);

        public void Probe(ProbeContext context) => context.CreateScope("scopeProxyActivity");

        public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
    }

    private sealed class RequestSagaStateDefinition : SagaDefinition<RequestSagaState>
    {
        protected override void ConfigureSaga(
            IReceiveEndpointConfigurator endpointConfigurator,
            ISagaConfigurator<RequestSagaState> sagaConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.UseMessageRetry(retry => retry.Immediate(2));
            endpointConfigurator.UseEntityFrameworkOutbox<RequestSagaDbContext>(context);
        }
    }

    public sealed class RequestSagaMap : SagaClassMap<RequestSagaState>
    {
        protected override void Configure(EntityTypeBuilder<RequestSagaState> entity, ModelBuilder model)
        {
            entity.ToTable("RequestSagaStates");
            entity.Property(state => state.CurrentState).HasMaxLength(64);
            entity.Property(state => state.RequestId);
            entity.Property(state => state.ResponseAddress);
            entity.Property(state => state.ResumeTokenId);
            entity.Property(state => state.LookupKey).HasMaxLength(128);
        }
    }

    public sealed class RequestSagaDbContext(DbContextOptions<RequestSagaDbContext> options) : SagaDbContext(options)
    {
        public DbSet<RequestSagaState> States => Set<RequestSagaState>();

        protected override IEnumerable<ISagaClassMap> Configurations
        {
            get { yield return new RequestSagaMap(); }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.AddTransactionalOutboxEntities();
        }
    }

    public sealed class RequestAttemptProbe
    {
        private readonly Dictionary<Guid, int> _attempts = [];
        private readonly Lock _lock = new();

        public int For(Guid correlationId)
        {
            lock (_lock)
                return _attempts.GetValueOrDefault(correlationId);
        }

        public void Increment(Guid correlationId)
        {
            lock (_lock)
                _attempts[correlationId] = _attempts.GetValueOrDefault(correlationId) + 1;
        }
    }

    public sealed record ScopeProxyObservation(
        Guid CorrelationId,
        Type PublishEndpointType,
        Type SendEndpointProviderType);

    public sealed class ScopeProxyProbe
    {
        private readonly TaskCompletionSource<ScopeProxyObservation> _observation =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Record(ScopeProxyObservation observation)
        {
            if (!_observation.TrySetResult(observation))
                throw new InvalidOperationException("The scope-proxy observation was recorded more than once.");
        }

        public Task<ScopeProxyObservation> ReadAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
            _observation.Task.WaitAsync(timeout, cancellationToken);
    }

    private sealed record ActivitySnapshot(
        string SourceName,
        ActivityKind Kind,
        KeyValuePair<string, object?>[] Tags)
    {
        public object? Tag(string key) => Tags.SingleOrDefault(tag => tag.Key == key).Value;
    }

    private sealed class RequestSagaFixture : IAsyncDisposable
    {
        private readonly PostgreSqlTestDatabase _database;

        private RequestSagaFixture(
            PostgreSqlTestDatabase database,
            ServiceProvider services,
            ITestHarness harness,
            RequestAttemptProbe attempts,
            ScopeProxyProbe scopeProxies,
            TimeSpan operationTimeout,
            CancellationToken cancellationToken)
        {
            _database = database;
            Services = services;
            Harness = harness;
            Attempts = attempts;
            ScopeProxies = scopeProxies;
            OperationTimeout = operationTimeout;
            CancellationToken = cancellationToken;
            Client = harness.GetRequestClient<StartRequestSaga>();
        }

        public RequestAttemptProbe Attempts { get; }

        public CancellationToken CancellationToken { get; }

        public IRequestClient<StartRequestSaga> Client { get; }

        public ITestHarness Harness { get; }

        public TimeSpan OperationTimeout { get; }

        public ScopeProxyProbe ScopeProxies { get; }

        public ServiceProvider Services { get; }

        public static async Task<RequestSagaFixture> CreateAsync()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
                .GetValidatedOptions()
                .OperationTimeout!.Value;
            PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync(
                "transactional-outbox-request-saga",
                cancellationToken);
            await using (var setup = new RequestSagaDbContext(
                             new DbContextOptionsBuilder<RequestSagaDbContext>()
                                 .UseNpgsql(database.ConnectionString)
                                 .Options))
            {
                if (!await setup.Database.EnsureCreatedAsync(cancellationToken))
                    throw new InvalidOperationException("The request-saga test database was not created.");
            }

            var attempts = new RequestAttemptProbe();
            var scopeProxies = new ScopeProxyProbe();
            var services = new ServiceCollection();
            services.AddSingleton(attempts);
            services.AddSingleton(scopeProxies);
            services.AddDbContext<RequestSagaDbContext>(builder => builder
                .UseNpgsql(database.ConnectionString, options => options.EnableRetryOnFailure()));
            services.AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
            {
                configuration.SetTestTimeouts(operationTimeout, operationTimeout);
                configuration.ConfigureEntityFrameworkTransactionalStore<RequestSagaDbContext>(outbox =>
                {
                    outbox.UsePostgres();
                    outbox.DisableInboxCleanupService();
                });
                configuration.AddSagaStateMachine<RequestSagaStateMachine, RequestSagaState, RequestSagaStateDefinition>()
                    .EntityFrameworkRepository(repository =>
                    {
                        repository.UsePostgres();
                        repository.ExistingDbContext<RequestSagaDbContext>();
                    });
            });

            ServiceProvider? provider = null;
            try
            {
                provider = services.BuildServiceProvider(new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true,
                });
                ITestHarness harness = await provider.StartTestHarnessAsync().WaitAsync(operationTimeout, cancellationToken);
                return new RequestSagaFixture(
                    database,
                    provider,
                    harness,
                    attempts,
                    scopeProxies,
                    operationTimeout,
                    cancellationToken);
            }
            catch
            {
                if (provider is not null)
                    await provider.DisposeAsync();
                await database.DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await Harness.StopAsync(CancellationToken.None).WaitAsync(OperationTimeout, CancellationToken.None);
            await Services.DisposeAsync();
            await _database.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class EntityFrameworkOpenTelemetryCollection
{
    public const string Name = "Entity Framework OpenTelemetry listeners";
}
