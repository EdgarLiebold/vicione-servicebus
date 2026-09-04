using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.ProviderAbstractions;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests.Outbox;

public sealed class EntityFrameworkScopedBusContextProviderTests
{
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
}
