using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Configuration;

public sealed class EntityFrameworkPublicArgumentValidationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-CONFIGURATION", "public-entry-points-reject-null-receivers-and-context")]
    public void ConfigurationEntryPoints_RejectNullReceiversAndContext()
    {
        IReceiveEndpointConfigurator endpoint = DispatchProxy.Create<IReceiveEndpointConfigurator, UnusedDispatchProxy>();

        ArgumentNullException defaultRegistration = Assert.Throws<ArgumentNullException>(() =>
            EntityFrameworkOutboxConfigurationExtensions.ConfigureEntityFrameworkTransactionalStore<TestDbContext>(null!));
        ArgumentNullException typedRegistration = Assert.Throws<ArgumentNullException>(() =>
            EntityFrameworkOutboxConfigurationExtensions.ConfigureEntityFrameworkTransactionalStore<ITestBus, TestDbContext>(null!));
        ArgumentNullException endpointReceiver = Assert.Throws<ArgumentNullException>(() =>
            EntityFrameworkOutboxConfigurationExtensions.UseEntityFrameworkOutbox<TestDbContext>(null!, null!));
        ArgumentNullException registrationContext = Assert.Throws<ArgumentNullException>(() =>
            endpoint.UseEntityFrameworkOutbox<TestDbContext>(null!));
        ArgumentNullException sqlServer = Assert.Throws<ArgumentNullException>(() =>
            EntityFrameworkOutboxConfigurationExtensions.UseSqlServer(null!));
        ArgumentNullException postgres = Assert.Throws<ArgumentNullException>(() =>
            EntityFrameworkOutboxConfigurationExtensions.UsePostgres(null!));
        ArgumentNullException sqlite = Assert.Throws<ArgumentNullException>(() =>
            EntityFrameworkOutboxConfigurationExtensions.UseSqlite(null!));

        Assert.Equal("configurator", defaultRegistration.ParamName);
        Assert.Equal("configurator", typedRegistration.ParamName);
        Assert.Equal("configurator", endpointReceiver.ParamName);
        Assert.Equal("context", registrationContext.ParamName);
        Assert.Equal("configurator", sqlServer.ParamName);
        Assert.Equal("configurator", postgres.ParamName);
        Assert.Equal("configurator", sqlite.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-CONFIGURATION", "mapping-entry-points-reject-null-builders")]
    public void MappingEntryPoints_RejectNullBuilders()
    {
        ArgumentNullException allEntities = Assert.Throws<ArgumentNullException>(() =>
            EntityFrameworkOutboxConfigurationExtensions.AddTransactionalOutboxEntities(null!));
        ArgumentNullException inboxEntity = Assert.Throws<ArgumentNullException>(() =>
            EntityFrameworkOutboxConfigurationExtensions.AddInboxStateEntity(null!));
        ArgumentNullException inboxMapping = Assert.Throws<ArgumentNullException>(() =>
            EntityFrameworkOutboxConfigurationExtensions.ConfigureInboxStateEntity(null!));
        ArgumentNullException outboxStateEntity = Assert.Throws<ArgumentNullException>(() =>
            EntityFrameworkOutboxConfigurationExtensions.AddOutboxStateEntity(null!));
        ArgumentNullException outboxStateMapping = Assert.Throws<ArgumentNullException>(() =>
            EntityFrameworkOutboxConfigurationExtensions.ConfigureOutboxStateEntity(null!));
        ArgumentNullException outboxMessageEntity = Assert.Throws<ArgumentNullException>(() =>
            EntityFrameworkOutboxConfigurationExtensions.AddOutboxMessageEntity(null!));
        ArgumentNullException outboxMessageMapping = Assert.Throws<ArgumentNullException>(() =>
            EntityFrameworkOutboxConfigurationExtensions.ConfigureOutboxMessageEntity(null!));
        ArgumentNullException conventionBoundary = Assert.Throws<ArgumentNullException>(() =>
            EntityFrameworkOutboxConfigurationExtensions.OptOutOfEntityFrameworkConventions(null!));

        Assert.Equal("modelBuilder", allEntities.ParamName);
        Assert.Equal("modelBuilder", inboxEntity.ParamName);
        Assert.Equal("inbox", inboxMapping.ParamName);
        Assert.Equal("modelBuilder", outboxStateEntity.ParamName);
        Assert.Equal("outbox", outboxStateMapping.ParamName);
        Assert.Equal("modelBuilder", outboxMessageEntity.ParamName);
        Assert.Equal("outbox", outboxMessageMapping.ParamName);
        Assert.Equal("builder", conventionBoundary.ParamName);
    }

    private interface ITestBus : IBus;

    private sealed class TestDbContext : DbContext;

    private class UnusedDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException("The guarded operation must reject its null argument before invoking the receiver.");
    }
}
