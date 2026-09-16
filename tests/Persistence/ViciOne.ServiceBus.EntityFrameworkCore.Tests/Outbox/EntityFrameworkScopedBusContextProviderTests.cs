using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Outbox;

public sealed class EntityFrameworkScopedBusContextProviderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-IDENTITY", "selector-rejects-missing-inputs-and-registration")]
    public void Provider_RejectsMissingInputsAndAnEmptyRegistrationSet()
    {
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();

        Assert.Throws<ArgumentNullException>(() =>
            new EntityFrameworkScopedBusContextProvider<IBus>(null!, services));
        Assert.Throws<ArgumentNullException>(() =>
            new EntityFrameworkScopedBusContextProvider<IBus>([], null!));
        ConfigurationException failure = Assert.Throws<ConfigurationException>(() =>
            new EntityFrameworkScopedBusContextProvider<IBus>([], services));

        Assert.Contains("No Entity Framework bus outbox", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-IDENTITY", "factory-metadata-and-provider-boundary")]
    public void Factories_ExposeExactContextIdentityAndRejectAMissingProvider()
    {
        var reliable = new EntityFrameworkScopedBusContextFactory<IBus, FirstDbContext>(isDefault: true);
        var transactional = new EntityFrameworkTransactionalScopedBusContextFactory<IBus, SecondDbContext>(isDefault: false);

        Assert.Equal(typeof(FirstDbContext), reliable.DbContextType);
        Assert.True(reliable.IsDefault);
        Assert.Throws<ArgumentNullException>(() => reliable.Create(null!));
        Assert.Equal(typeof(SecondDbContext), transactional.DbContextType);
        Assert.False(transactional.IsDefault);
        Assert.Throws<ArgumentNullException>(() => transactional.Create(null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-IDENTITY", "transactional-session-is-reused-per-scope")]
    public void TransactionalFactory_ReusesTheRegistrySessionWithinOneScope()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => new FirstDbContext());
        services.AddViciOneServiceBus(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.ConfigureEntityFrameworkTransactionalStore<FirstDbContext>(outbox =>
            {
                outbox.UseSqlite();
                outbox.DisableInboxCleanupService();
                outbox.EnableTransactionalOutbox(delivery => delivery.DisableDeliveryService());
            });
            bus.UsingInMemory();
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        EntityFrameworkBusOutboxSessionRegistry<IBus> registry = scope.ServiceProvider
            .GetRequiredService<EntityFrameworkBusOutboxSessionRegistry<IBus>>();
        EntityFrameworkTransactionalScopedBusContext<IBus, FirstDbContext> first = registry
            .GetOrCreateTransactional<FirstDbContext>(scope.ServiceProvider);
        EntityFrameworkTransactionalScopedBusContext<IBus, FirstDbContext> second = registry
            .GetOrCreateTransactional<FirstDbContext>(scope.ServiceProvider);
        IEntityFrameworkScopedBusContextFactory<IBus> factory = Assert.Single(
            provider.GetServices<IEntityFrameworkScopedBusContextFactory<IBus>>());

        Assert.Same(first, second);
        Assert.Same(first, factory.Create(scope.ServiceProvider));
        Assert.Same(first, scope.ServiceProvider
            .GetRequiredService<IEntityFrameworkTransactionalOutbox<IBus, FirstDbContext>>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-IDENTITY", "concurrent-session-resolution-has-one-winner")]
    public async Task TransactionalRegistry_ConcurrentResolutionReturnsOneSessionAsync()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => new FirstDbContext());
        services.AddViciOneServiceBus(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.ConfigureEntityFrameworkTransactionalStore<FirstDbContext>(outbox =>
            {
                outbox.UseSqlite();
                outbox.DisableInboxCleanupService();
                outbox.EnableTransactionalOutbox(delivery => delivery.DisableDeliveryService());
            });
            bus.UsingInMemory();
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        EntityFrameworkBusOutboxSessionRegistry<IBus> registry = scope.ServiceProvider
            .GetRequiredService<EntityFrameworkBusOutboxSessionRegistry<IBus>>();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<EntityFrameworkTransactionalScopedBusContext<IBus, FirstDbContext>>[] resolutions = Enumerable
            .Range(0, 16)
            .Select(async _ =>
            {
                await release.Task;
                return registry.GetOrCreateTransactional<FirstDbContext>(scope.ServiceProvider);
            })
            .ToArray();

        release.SetResult();
        EntityFrameworkTransactionalScopedBusContext<IBus, FirstDbContext>[] contexts =
            await Task.WhenAll(resolutions);

        EntityFrameworkTransactionalScopedBusContext<IBus, FirstDbContext> expected = Assert.Single(
            contexts.Distinct());
        Assert.All(contexts, context => Assert.Same(expected, context));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-IDENTITY", "transactional-factory-preserves-ambient-consume-context")]
    public void TransactionalFactory_PreservesAnAmbientConsumeContext()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => new FirstDbContext());
        services.AddViciOneServiceBus(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.ConfigureEntityFrameworkTransactionalStore<FirstDbContext>(outbox =>
            {
                outbox.UseSqlite();
                outbox.DisableInboxCleanupService();
                outbox.EnableTransactionalOutbox(delivery => delivery.DisableDeliveryService());
            });
            bus.UsingInMemory();
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IEntityFrameworkScopedBusContextFactory<IBus> factory = Assert.Single(
            provider.GetServices<IEntityFrameworkScopedBusContextFactory<IBus>>());
        IScopedConsumeContextProvider contextProvider = scope.ServiceProvider
            .GetRequiredService<Bind<IBus, IScopedConsumeContextProvider>>()
            .Value;
        ConsumeContext ambient = DispatchProxy.Create<ConsumeContext, PassiveConsumeContextProxy>();

        using (contextProvider.PushContext(ambient))
        {
            Assert.IsType<ConsumeContextScopedBusContext>(factory.Create(scope.ServiceProvider));
            using EntityFrameworkTransactionalScopedBusContext<IBus, FirstDbContext> explicitContext =
                EntityFrameworkTransactionalScopedBusContextFactory<IBus, FirstDbContext>
                    .CreateTransactionalContext(scope.ServiceProvider);
            Assert.IsType<EntityFrameworkTransactionalConsumeContextScopedBusContext<IBus, FirstDbContext>>(
                explicitContext);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-IDENTITY", "single-dbcontext-is-implicit-default")]
    public void SingleRegistration_IsSelectedWithoutAnExplicitDefault()
    {
        var expected = new RecordingScopedBusContext();
        var factory = new RecordingFactory(typeof(FirstDbContext), false, expected);
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();

        var provider = new EntityFrameworkScopedBusContextProvider<IBus>([factory], services);

        Assert.Same(expected, provider.Context);
        Assert.Equal(1, factory.CreateCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-IDENTITY", "multiple-dbcontexts-require-default")]
    public void MultipleRegistrations_WithoutADefaultFailBeforeCreatingASession()
    {
        var first = new RecordingFactory(typeof(FirstDbContext), false, new RecordingScopedBusContext());
        var second = new RecordingFactory(typeof(SecondDbContext), false, new RecordingScopedBusContext());
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();

        ConfigurationException failure = Assert.Throws<ConfigurationException>(() =>
            new EntityFrameworkScopedBusContextProvider<IBus>([first, second], services));

        Assert.Contains("explicit default DbContext", failure.Message, StringComparison.Ordinal);
        Assert.Equal(0, first.CreateCount);
        Assert.Equal(0, second.CreateCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-IDENTITY", "explicit-default-is-order-independent")]
    public void ExactlyOneDefault_IsSelectedIndependentOfRegistrationOrder(bool reverse)
    {
        var regular = new RecordingFactory(typeof(FirstDbContext), false, new RecordingScopedBusContext());
        var expected = new RecordingScopedBusContext();
        var selected = new RecordingFactory(typeof(SecondDbContext), true, expected);
        IEntityFrameworkScopedBusContextFactory<IBus>[] factories = reverse ? [selected, regular] : [regular, selected];
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();

        var provider = new EntityFrameworkScopedBusContextProvider<IBus>(factories, services);

        Assert.Same(expected, provider.Context);
        Assert.Equal(0, regular.CreateCount);
        Assert.Equal(1, selected.CreateCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-IDENTITY", "multiple-defaults-fail-closed")]
    public void MultipleDefaults_FailBeforeCreatingASession()
    {
        var first = new RecordingFactory(typeof(FirstDbContext), true, new RecordingScopedBusContext());
        var second = new RecordingFactory(typeof(SecondDbContext), true, new RecordingScopedBusContext());
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();

        ConfigurationException failure = Assert.Throws<ConfigurationException>(() =>
            new EntityFrameworkScopedBusContextProvider<IBus>([first, second], services));

        Assert.Contains("Exactly one default", failure.Message, StringComparison.Ordinal);
        Assert.Equal(0, first.CreateCount);
        Assert.Equal(0, second.CreateCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-IDENTITY", "bus-discriminator-is-explicit-stable-bounded-and-distinct")]
    public void BusIdentity_IsExplicitStableBoundedAndDistinctFromClrTypeNames()
    {
        BusPersistenceIdentity<IFirstBus> first = BusPersistenceIdentity<IFirstBus>.Create("orders-v1");
        BusPersistenceIdentity<ISecondBus> second = BusPersistenceIdentity<ISecondBus>.Create("billing-v1");

        Assert.Equal("orders-v1", first.Require("test"));
        Assert.Equal("orders-v1", BusPersistenceIdentity<IFirstBus>.Create("orders-v1").Require("test"));
        Assert.Equal("billing-v1", second.Require("test"));
        Assert.DoesNotContain(nameof(IFirstBus), first.Require("test"), StringComparison.Ordinal);
        Assert.InRange(first.Require("test").Length, 1, BusPersistenceIdentity<IFirstBus>.MaximumLength);
        Assert.InRange(second.Require("test").Length, 1, BusPersistenceIdentity<ISecondBus>.MaximumLength);
    }

    private interface IFirstBus : IBus;
    private interface ISecondBus : IBus;

    private sealed class FirstDbContext : DbContext;
    private sealed class SecondDbContext : DbContext;

    private sealed class RecordingFactory(
        Type dbContextType,
        bool isDefault,
        ScopedBusContext context) : IEntityFrameworkScopedBusContextFactory<IBus>
    {
        private int _createCount;

        public Type DbContextType { get; } = dbContextType;
        public bool IsDefault { get; } = isDefault;
        public int CreateCount => Volatile.Read(ref _createCount);

        public ScopedBusContext Create(IServiceProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);
            Interlocked.Increment(ref _createCount);
            return context;
        }
    }

    private sealed class RecordingScopedBusContext : ScopedBusContext
    {
        public ISendEndpointProvider SendEndpointProvider => null!;
        public IPublishEndpoint PublishEndpoint => null!;
        public IScopedClientFactory ClientFactory => null!;
    }

    private class PassiveConsumeContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.ReturnType.IsValueType == true
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
    }
}
