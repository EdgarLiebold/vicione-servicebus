using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Configuration;

public sealed class EntityFrameworkJobServiceConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-JOB-SERVICE", "all-saga-repositories-use-explicit-lock-provider")]
    public void UseEntityFrameworkCoreSagaRepository_AssignsAllRepositoriesWithAnExplicitLockProvider()
    {
        IJobServiceConfigurator configurator = DispatchProxy.Create<IJobServiceConfigurator, RecordingJobServiceConfiguratorProxy>();
        var proxy = (RecordingJobServiceConfiguratorProxy)(object)configurator;
        var lockStatementProvider = new UnexpectedLockStatementProvider();

        configurator.UseEntityFrameworkCoreSagaRepository(
            static () => throw new InvalidOperationException("The DbContext factory must remain deferred during configuration."),
            lockStatementProvider);

        Assert.IsAssignableFrom<ISagaRepository<JobTypeSaga>>(proxy.Repositories[nameof(IJobServiceConfigurator.JobTypeRepository)]);
        Assert.IsAssignableFrom<ISagaRepository<JobSaga>>(proxy.Repositories[nameof(IJobServiceConfigurator.JobRepository)]);
        Assert.IsAssignableFrom<ISagaRepository<JobAttemptSaga>>(proxy.Repositories[nameof(IJobServiceConfigurator.JobAttemptRepository)]);
        Assert.Equal(3, proxy.Repositories.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-JOB-SERVICE", "missing-configuration-dependencies-are-rejected")]
    public void UseEntityFrameworkCoreSagaRepository_RejectsMissingDependencies()
    {
        IJobServiceConfigurator configurator = DispatchProxy.Create<IJobServiceConfigurator, RecordingJobServiceConfiguratorProxy>();
        Func<JobServiceSagaDbContext> contextFactory = static () => throw new NotSupportedException();
        var lockStatementProvider = new UnexpectedLockStatementProvider();

        ArgumentNullException missingConfigurator = Assert.Throws<ArgumentNullException>(() =>
            EntityFrameworkCoreJobServiceConfigurationExtensions.UseEntityFrameworkCoreSagaRepository(
                null!, contextFactory, lockStatementProvider));
        ArgumentNullException missingFactory = Assert.Throws<ArgumentNullException>(() =>
            configurator.UseEntityFrameworkCoreSagaRepository(null!, lockStatementProvider));
        ArgumentNullException missingProvider = Assert.Throws<ArgumentNullException>(() =>
            configurator.UseEntityFrameworkCoreSagaRepository(contextFactory, null!));

        Assert.Equal("configurator", missingConfigurator.ParamName);
        Assert.Equal("contextFactory", missingFactory.ParamName);
        Assert.Equal("lockStatementProvider", missingProvider.ParamName);
    }

    private class RecordingJobServiceConfiguratorProxy : DispatchProxy
    {
        public Dictionary<string, object> Repositories { get; } = new(StringComparer.Ordinal);

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name is { } methodName
                && methodName.StartsWith("set_", StringComparison.Ordinal)
                && methodName.EndsWith("Repository", StringComparison.Ordinal))
            {
                Repositories.Add(methodName[4..], args![0]!);
                return null;
            }

            throw new InvalidOperationException($"Unexpected job-service configurator member: {targetMethod?.Name ?? "<null>"}.");
        }
    }

    private sealed class UnexpectedLockStatementProvider : ILockStatementProvider
    {
        public string GetRowLockStatement<T>(DbContext context)
            where T : class => throw new InvalidOperationException("Row-lock SQL must not be requested during configuration.");

        public string GetRowLockStatement<T>(DbContext context, params string[] propertyNames)
            where T : class => throw new InvalidOperationException("Row-lock SQL must not be requested during configuration.");

        public string GetOutboxStatement(DbContext context) =>
            throw new InvalidOperationException("Outbox SQL must not be requested during job-service configuration.");

        public string GetInboxCleanupLockStatement(DbContext context) =>
            throw new InvalidOperationException("Inbox SQL must not be requested during job-service configuration.");
    }
}
