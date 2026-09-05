using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.EntityFrameworkCoreIntegration;

public sealed class EntityFrameworkExecutionStrategyRetryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-EXECUTION-STRATEGY", "retry-discards-rolled-back-tracked-state")]
    public async Task TransientRetry_DiscardsRolledBackTrackedStateBeforeReexecutingTheConsumerAsync()
    {
        RetryScenarioResult result = await RunRetryScenarioAsync();

        Assert.Equal(result.MessageId, result.InboxMessageId);
        Assert.Equal(2, result.ConsumerAttempts);
        Assert.True(result.StaleTrackedStateInjected);
        Assert.Equal(1, result.ReceiveCount);
        Assert.NotNull(result.Consumed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-INBOX-CANCELLATION", "every-inbox-write-receives-the-consume-token")]
    public async Task InboxPersistence_PropagatesTheConsumeTokenToEverySaveAsync()
    {
        RetryScenarioResult result = await RunRetryScenarioAsync();

        Assert.NotEmpty(result.InboxSaveTokens);
        Assert.All(result.InboxSaveTokens, token => Assert.Equal(result.ConsumerCancellationToken, token));
    }

    private static async Task<RetryScenarioResult> RunRetryScenarioAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        await using PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync(
            "execution-strategy-retry",
            cancellationToken);
        var probe = new RetryProbe();
        var saveTokenProbe = new InboxSaveTokenProbe();
        var services = new ServiceCollection();
        services.AddSingleton(probe);
        services.AddSingleton(saveTokenProbe);
        services.AddDbContext<RetryDbContext>((provider, builder) => builder
            .UseNpgsql(database.ConnectionString, options => options.EnableRetryOnFailure())
            .AddInterceptors(provider.GetRequiredService<InboxSaveTokenProbe>()));
        services.AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
        {
            configuration.SetTestTimeouts(operationTimeout, operationTimeout);
            configuration.ConfigureEntityFrameworkTransactionalStore<RetryDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.DisableInboxCleanupService();
            });
            configuration.AddConsumer<RetryConsumer, RetryConsumerDefinition>();
            configuration.AddConsumer<RetryEffectConsumer>();
            configuration.UsingInMemory((context, bus) => bus.ConfigureEndpoints(context));
        });

        await using (var context = new RetryDbContext(
                         new DbContextOptionsBuilder<RetryDbContext>()
                             .UseNpgsql(database.ConnectionString)
                             .Options))
        {
            Assert.True(await context.Database.EnsureCreatedAsync(cancellationToken));
        }

        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        ITestHarness harness = await provider.StartTestHarnessAsync().WaitAsync(operationTimeout, cancellationToken);
        Guid messageId = Guid.NewGuid();

        try
        {
            await harness.Bus.PublishAsync(new RetryCommand(messageId), context => context.MessageId = messageId, cancellationToken);
            RetryEffect effect = await probe.EffectDelivered.WaitAsync(operationTimeout, cancellationToken);

            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            var verification = scope.ServiceProvider.GetRequiredService<RetryDbContext>();
            InboxState inbox = Assert.Single(await verification.Set<InboxState>()
                .AsNoTracking()
                .ToListAsync(cancellationToken));

            Assert.Equal(messageId, effect.SourceMessageId);
            return new RetryScenarioResult(
                messageId,
                inbox.MessageId,
                inbox.ReceiveCount,
                inbox.Consumed,
                probe.ConsumerAttempts,
                probe.StaleTrackedStateInjected,
                probe.ConsumerCancellationToken,
                saveTokenProbe.Snapshot());
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(operationTimeout, CancellationToken.None);
        }
    }

    public sealed record RetryCommand(Guid CorrelationId);
    public sealed record RetryEffect(Guid SourceMessageId);

    public sealed class RetryConsumer(RetryProbe probe, RetryDbContext dbContext) : IConsumer<RetryCommand>
    {
        public Task ConsumeAsync(ConsumeContext<RetryCommand> context)
        {
            if (probe.RecordConsumerAttempt(context.CancellationToken) == 1)
            {
                InboxState inboxState = dbContext.ChangeTracker.Entries<InboxState>()
                    .Select(entry => entry.Entity)
                    .Single();
                inboxState.ReceiveCount = 41;
                probe.RecordStaleTrackedStateInjection();

                throw new PostgresException(
                    "Test-owned serialization failure after a tracked mutation.",
                    "ERROR",
                    "ERROR",
                    PostgresErrorCodes.SerializationFailure);
            }

            return context.Advanced().PublishAsync(new RetryEffect(context.MessageId!.Value), context.CancellationToken);
        }
    }

    public sealed class RetryEffectConsumer(RetryProbe probe) : IConsumer<RetryEffect>
    {
        public Task ConsumeAsync(ConsumeContext<RetryEffect> context)
        {
            probe.RecordEffect(context.Message);
            return Task.CompletedTask;
        }
    }

    private sealed class RetryConsumerDefinition : ConsumerDefinition<RetryConsumer>
    {
        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<RetryConsumer> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.UseEntityFrameworkOutbox<RetryDbContext>(context);
        }
    }

    public sealed class RetryDbContext(DbContextOptions<RetryDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddTransactionalOutboxEntities();
        }
    }

    public sealed class RetryProbe
    {
        private readonly TaskCompletionSource<RetryEffect> _effectDelivered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _consumerAttempts;
        private int _staleTrackedStateInjected;
        private CancellationToken _consumerCancellationToken;

        public int ConsumerAttempts => Volatile.Read(ref _consumerAttempts);
        public CancellationToken ConsumerCancellationToken => _consumerCancellationToken;
        public Task<RetryEffect> EffectDelivered => _effectDelivered.Task;
        public bool StaleTrackedStateInjected => Volatile.Read(ref _staleTrackedStateInjected) == 1;

        public int RecordConsumerAttempt(CancellationToken cancellationToken)
        {
            _consumerCancellationToken = cancellationToken;
            return Interlocked.Increment(ref _consumerAttempts);
        }

        public void RecordEffect(RetryEffect effect)
        {
            if (!_effectDelivered.TrySetResult(effect))
                throw new InvalidOperationException("The retry effect was delivered more than once.");
        }

        public void RecordStaleTrackedStateInjection() => Volatile.Write(ref _staleTrackedStateInjected, 1);
    }

    public sealed class InboxSaveTokenProbe : SaveChangesInterceptor
    {
        private readonly Lock _lock = new();
        private readonly List<CancellationToken> _tokens = [];

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context?.ChangeTracker.Entries<InboxState>()
                    .Any(entry => entry.State is EntityState.Added or EntityState.Modified) == true)
            {
                lock (_lock)
                    _tokens.Add(cancellationToken);
            }

            return ValueTask.FromResult(result);
        }

        public CancellationToken[] Snapshot()
        {
            lock (_lock)
                return [.. _tokens];
        }
    }

    private sealed record RetryScenarioResult(
        Guid MessageId,
        Guid InboxMessageId,
        int ReceiveCount,
        DateTimeOffset? Consumed,
        int ConsumerAttempts,
        bool StaleTrackedStateInjected,
        CancellationToken ConsumerCancellationToken,
        IReadOnlyList<CancellationToken> InboxSaveTokens);
}
