using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.EntityFrameworkCoreIntegration;

public sealed class ScopedOutboxFilterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-SCOPED-FILTER", "consume-outbox-publish-filter-retains-originating-scope")]
    public async Task ConsumeOutboxPublishFilter_UsesTheExactConsumerScopeAsync()
    {
        await using ScopedOutboxFixture fixture = await ScopedOutboxFixture.CreateAsync();
        Guid correlationId = Guid.NewGuid();

        await fixture.Harness.Bus.PublishAsync(
            new PublishFromConsumeScope(correlationId),
            context => context.MessageId = Guid.NewGuid(),
            fixture.CancellationToken);

        FilterObservation filter = await fixture.Filters.ReadAsync(
            fixture.OperationTimeout,
            fixture.CancellationToken);
        ScopedPublishedEvent delivered = await fixture.Deliveries.ReadPublishedAsync(
            fixture.OperationTimeout,
            fixture.CancellationToken);

        Assert.Equal(FilterKind.Publish, filter.Kind);
        Assert.Equal(correlationId, delivered.CorrelationId);
        Assert.Equal(delivered.ScopeId, filter.MessageScopeId);
        Assert.Equal(delivered.ScopeId, filter.FilterScopeId);
        Assert.True(filter.HasConsumeContext);
        Assert.Equal(1, fixture.Deliveries.PublishedCount);
        Assert.Equal(1, fixture.Filters.PublishCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-SCOPED-FILTER", "bus-outbox-send-filter-retains-originating-scope")]
    public async Task BusOutboxSendFilter_UsesTheExactApplicationScopeAsync()
    {
        await using ScopedOutboxFixture fixture = await ScopedOutboxFixture.CreateAsync();
        Guid correlationId = Guid.NewGuid();
        Guid expectedScopeId;

        await using (AsyncServiceScope scope = fixture.Services.CreateAsyncScope())
        {
            expectedScopeId = scope.ServiceProvider.GetRequiredService<ScopeIdentity>().Value;
            var dbContext = scope.ServiceProvider.GetRequiredService<ScopedOutboxDbContext>();
            var endpointProvider = scope.ServiceProvider.GetRequiredService<ISendEndpointProvider>();
            Uri destination = fixture.Harness.GetConsumerAddress<ScopedSendConsumer>();
            ISendEndpoint endpoint = await endpointProvider.GetSendEndpointAsync(destination, TestContext.Current.CancellationToken);

            await endpoint.SendAsync(
                new ScopedSendCommand(correlationId, expectedScopeId),
                context => context.MessageId = Guid.NewGuid(),
                fixture.CancellationToken);
            await dbContext.SaveChangesAsync(fixture.CancellationToken);
        }

        FilterObservation filter = await fixture.Filters.ReadAsync(
            fixture.OperationTimeout,
            fixture.CancellationToken);
        ScopedSendCommand delivered = await fixture.Deliveries.ReadSentAsync(
            fixture.OperationTimeout,
            fixture.CancellationToken);

        Assert.Equal(FilterKind.Send, filter.Kind);
        Assert.Equal(correlationId, delivered.CorrelationId);
        Assert.Equal(expectedScopeId, delivered.ScopeId);
        Assert.Equal(expectedScopeId, filter.MessageScopeId);
        Assert.Equal(expectedScopeId, filter.FilterScopeId);
        Assert.False(filter.HasConsumeContext);
        Assert.Equal(1, fixture.Deliveries.SentCount);
        Assert.Equal(1, fixture.Filters.SendCount);
    }

    public sealed record PublishFromConsumeScope(Guid CorrelationId);

    public sealed record ScopedPublishedEvent(Guid CorrelationId, Guid ScopeId);

    public sealed record ScopedSendCommand(Guid CorrelationId, Guid ScopeId);

    public sealed record ScopeIdentity(Guid Value);

    public sealed class PublishFromOutboxConsumer(ScopeIdentity scopeIdentity) : IConsumer<PublishFromConsumeScope>
    {
        public Task ConsumeAsync(ConsumeContext<PublishFromConsumeScope> context) => context.Advanced().PublishAsync(
            new ScopedPublishedEvent(context.Message.CorrelationId, scopeIdentity.Value),
            context.CancellationToken);
    }

    public sealed class ScopedPublishedEventConsumer(ScopedDeliveryProbe deliveries) : IConsumer<ScopedPublishedEvent>
    {
        public Task ConsumeAsync(ConsumeContext<ScopedPublishedEvent> context)
        {
            deliveries.RecordPublished(context.Message);
            return Task.CompletedTask;
        }
    }

    public sealed class ScopedSendConsumer(ScopedDeliveryProbe deliveries) : IConsumer<ScopedSendCommand>
    {
        public Task ConsumeAsync(ConsumeContext<ScopedSendCommand> context)
        {
            deliveries.RecordSent(context.Message);
            return Task.CompletedTask;
        }
    }

    private sealed class PublishFromOutboxConsumerDefinition : ConsumerDefinition<PublishFromOutboxConsumer>
    {
        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<PublishFromOutboxConsumer> consumerConfigurator,
            IRegistrationContext context) => endpointConfigurator.UseEntityFrameworkOutbox<ScopedOutboxDbContext>(context);
    }

    public sealed class ScopedPublishFilter<T>(
        ScopeIdentity scopeIdentity,
        IScopedConsumeContextProvider consumeContextProvider,
        ScopedFilterProbe filters) : IFilter<PublishContext<T>>
        where T : class
    {
        public Task SendAsync(PublishContext<T> context, IPipe<PublishContext<T>> next)
        {
            if (context.Message is ScopedPublishedEvent message)
            {
                filters.Record(new FilterObservation(
                    FilterKind.Publish,
                    message.ScopeId,
                    scopeIdentity.Value,
                    consumeContextProvider.HasContext));
            }

            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("scopedOutboxPublishFilter");
    }

    public sealed class ScopedSendFilter<T>(
        ScopeIdentity scopeIdentity,
        IScopedConsumeContextProvider consumeContextProvider,
        ScopedFilterProbe filters) : IFilter<SendContext<T>>
        where T : class
    {
        public Task SendAsync(SendContext<T> context, IPipe<SendContext<T>> next)
        {
            if (context.Message is ScopedSendCommand message)
            {
                filters.Record(new FilterObservation(
                    FilterKind.Send,
                    message.ScopeId,
                    scopeIdentity.Value,
                    consumeContextProvider.HasContext));
            }

            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("scopedOutboxSendFilter");
    }

    public sealed class ScopedOutboxDbContext(DbContextOptions<ScopedOutboxDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddTransactionalOutboxEntities();
    }

    public enum FilterKind
    {
        Publish,
        Send,
    }

    public sealed record FilterObservation(
        FilterKind Kind,
        Guid MessageScopeId,
        Guid FilterScopeId,
        bool HasConsumeContext);

    public sealed class ScopedFilterProbe
    {
        private readonly Channel<FilterObservation> _observations = Channel.CreateUnbounded<FilterObservation>();
        private int _publishCount;
        private int _sendCount;

        public int PublishCount => Volatile.Read(ref _publishCount);

        public int SendCount => Volatile.Read(ref _sendCount);

        public void Record(FilterObservation observation)
        {
            if (observation.Kind == FilterKind.Publish)
                Interlocked.Increment(ref _publishCount);
            else
                Interlocked.Increment(ref _sendCount);

            if (!_observations.Writer.TryWrite(observation))
                throw new InvalidOperationException("The scoped-filter observation channel rejected an event.");
        }

        public async Task<FilterObservation> ReadAsync(
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            FilterObservation observation = await _observations.Reader.ReadAsync(cancellationToken)
                .AsTask()
                .WaitAsync(timeout, cancellationToken);
            return observation;
        }
    }

    public sealed class ScopedDeliveryProbe
    {
        private readonly Channel<ScopedPublishedEvent> _published = Channel.CreateUnbounded<ScopedPublishedEvent>();
        private readonly Channel<ScopedSendCommand> _sent = Channel.CreateUnbounded<ScopedSendCommand>();
        private int _publishedCount;
        private int _sentCount;

        public int PublishedCount => Volatile.Read(ref _publishedCount);

        public int SentCount => Volatile.Read(ref _sentCount);

        public void RecordPublished(ScopedPublishedEvent message)
        {
            Interlocked.Increment(ref _publishedCount);
            if (!_published.Writer.TryWrite(message))
                throw new InvalidOperationException("The scoped publish-delivery channel rejected an event.");
        }

        public void RecordSent(ScopedSendCommand message)
        {
            Interlocked.Increment(ref _sentCount);
            if (!_sent.Writer.TryWrite(message))
                throw new InvalidOperationException("The scoped send-delivery channel rejected an event.");
        }

        public Task<ScopedPublishedEvent> ReadPublishedAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
            _published.Reader.ReadAsync(cancellationToken).AsTask().WaitAsync(timeout, cancellationToken);

        public Task<ScopedSendCommand> ReadSentAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
            _sent.Reader.ReadAsync(cancellationToken).AsTask().WaitAsync(timeout, cancellationToken);
    }

    private sealed class ScopedOutboxFixture : IAsyncDisposable
    {
        private readonly PostgreSqlTestDatabase _database;

        private ScopedOutboxFixture(
            PostgreSqlTestDatabase database,
            ServiceProvider services,
            ITestHarness harness,
            ScopedFilterProbe filters,
            ScopedDeliveryProbe deliveries,
            TimeSpan operationTimeout,
            CancellationToken cancellationToken)
        {
            _database = database;
            Services = services;
            Harness = harness;
            Filters = filters;
            Deliveries = deliveries;
            OperationTimeout = operationTimeout;
            CancellationToken = cancellationToken;
        }

        public CancellationToken CancellationToken { get; }

        public ScopedDeliveryProbe Deliveries { get; }

        public ScopedFilterProbe Filters { get; }

        public ITestHarness Harness { get; }

        public TimeSpan OperationTimeout { get; }

        public ServiceProvider Services { get; }

        public static async Task<ScopedOutboxFixture> CreateAsync()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
                .GetValidatedOptions()
                .OperationTimeout!.Value;
            PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync(
                "scoped-outbox-filters",
                cancellationToken);
            var filters = new ScopedFilterProbe();
            var deliveries = new ScopedDeliveryProbe();
            var services = new ServiceCollection();
            services.AddScoped(_ => new ScopeIdentity(Guid.NewGuid()));
            services.AddSingleton(filters);
            services.AddSingleton(deliveries);
            services.AddDbContext<ScopedOutboxDbContext>(builder => builder
                .UseNpgsql(database.ConnectionString, options => options.EnableRetryOnFailure()));
            services.AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
            {
                configuration.SetTestTimeouts(operationTimeout, operationTimeout);
                configuration.Contracts(contracts => contracts
                    .Register<PublishFromConsumeScope>("vicione.tests.ef.publish-from-scope")
                    .Register<ScopedPublishedEvent>("vicione.tests.ef.scoped-published-event")
                    .Register<ScopedSendCommand>("vicione.tests.ef.scoped-send-command"));
                configuration.ConfigureEntityFrameworkTransactionalStore<ScopedOutboxDbContext>(outbox =>
                {
                    outbox.UsePostgreSql();
                    outbox.DisableInboxCleanupService();
                    outbox.QueryDelay = TimeSpan.FromHours(1);
                    outbox.EnableTransactionalOutbox();
                });
                configuration.AddConsumer<PublishFromOutboxConsumer, PublishFromOutboxConsumerDefinition>();
                configuration.AddConsumer<ScopedPublishedEventConsumer>();
                configuration.AddConsumer<ScopedSendConsumer>();
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.UsePublishFilter(typeof(ScopedPublishFilter<>), context);
                    bus.UseSendFilter(typeof(ScopedSendFilter<>), context);
                    bus.ConfigureEndpoints(context);
                });
            });

            ServiceProvider? provider = null;
            try
            {
                await using (var context = new ScopedOutboxDbContext(
                                 new DbContextOptionsBuilder<ScopedOutboxDbContext>()
                                     .UseNpgsql(database.ConnectionString)
                                     .Options))
                {
                    if (!await context.Database.EnsureCreatedAsync(cancellationToken))
                        throw new InvalidOperationException("The scoped-outbox test database was not created.");
                }

                provider = services.BuildServiceProvider(new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true,
                });
                ITestHarness harness = await provider.StartTestHarnessAsync().WaitAsync(operationTimeout, cancellationToken);
                return new ScopedOutboxFixture(
                    database,
                    provider,
                    harness,
                    filters,
                    deliveries,
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
