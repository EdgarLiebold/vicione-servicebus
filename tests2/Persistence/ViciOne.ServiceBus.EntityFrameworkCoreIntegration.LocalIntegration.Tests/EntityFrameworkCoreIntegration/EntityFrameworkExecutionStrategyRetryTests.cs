namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests.EntityFrameworkCoreIntegration;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Testing;
using Xunit;

public sealed class EntityFrameworkExecutionStrategyRetryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-EXECUTION-STRATEGY", "retry-discards-rolled-back-tracked-state")]
    public async Task TransientRetry_DiscardsRolledBackTrackedStateBeforeReexecutingTheConsumer()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        await using PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync(
            "execution-strategy-retry",
            cancellationToken);
        var probe = new RetryProbe();
        var services = new ServiceCollection();
        services.AddSingleton(probe);
        services.AddDbContext<RetryDbContext>(builder => builder
            .UseNpgsql(database.ConnectionString, options => options.EnableRetryOnFailure()));
        services.AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
        {
            configuration.SetTestTimeouts(operationTimeout, operationTimeout);
            configuration.AddEntityFrameworkOutbox<RetryDbContext>(outbox =>
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
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(operationTimeout, cancellationToken);
        Guid messageId = Guid.NewGuid();

        try
        {
            await harness.Bus.Publish(new RetryCommand(messageId), context => context.MessageId = messageId, cancellationToken);
            RetryEffect effect = await probe.EffectDelivered.WaitAsync(operationTimeout, cancellationToken);

            Assert.Equal(messageId, effect.SourceMessageId);
            Assert.Equal(2, probe.ConsumerAttempts);
            Assert.True(probe.StaleTrackedStateInjected);

            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            var verification = scope.ServiceProvider.GetRequiredService<RetryDbContext>();
            InboxState inbox = Assert.Single(await verification.Set<InboxState>()
                .AsNoTracking()
                .ToListAsync(cancellationToken));
            Assert.Equal(messageId, inbox.MessageId);
            Assert.Equal(1, inbox.ReceiveCount);
            Assert.NotNull(inbox.Consumed);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(operationTimeout, CancellationToken.None);
        }
    }

    public sealed record RetryCommand(Guid CorrelationId);
    public sealed record RetryEffect(Guid SourceMessageId);

    public sealed class RetryConsumer(RetryProbe probe, RetryDbContext dbContext) : IConsumer<RetryCommand>
    {
        public Task Consume(ConsumeContext<RetryCommand> context)
        {
            if (probe.RecordConsumerAttempt() == 1)
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

            return context.Publish(new RetryEffect(context.MessageId!.Value), context.CancellationToken);
        }
    }

    public sealed class RetryEffectConsumer(RetryProbe probe) : IConsumer<RetryEffect>
    {
        public Task Consume(ConsumeContext<RetryEffect> context)
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

        public int ConsumerAttempts => Volatile.Read(ref _consumerAttempts);
        public Task<RetryEffect> EffectDelivered => _effectDelivered.Task;
        public bool StaleTrackedStateInjected => Volatile.Read(ref _staleTrackedStateInjected) == 1;

        public int RecordConsumerAttempt() => Interlocked.Increment(ref _consumerAttempts);

        public void RecordEffect(RetryEffect effect)
        {
            if (!_effectDelivered.TrySetResult(effect))
                throw new InvalidOperationException("The retry effect was delivered more than once.");
        }

        public void RecordStaleTrackedStateInjection() => Volatile.Write(ref _staleTrackedStateInjected, 1);
    }
}
