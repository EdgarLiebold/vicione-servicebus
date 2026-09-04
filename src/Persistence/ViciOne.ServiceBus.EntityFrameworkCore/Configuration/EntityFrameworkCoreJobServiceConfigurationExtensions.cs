using System;
using ViciOne.ServiceBus.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Provides extension methods for entity framework core job service configuration.
/// </summary>
public static class EntityFrameworkCoreJobServiceConfigurationExtensions
{
    /// <summary>
    /// Configures entity framework core saga repository for the current pipeline.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="contextFactory">The context factory value.</param>
    /// <param name="lockStatementProvider">The lock statement provider value.</param>
    public static void UseEntityFrameworkCoreSagaRepository(this IJobServiceConfigurator configurator, Func<JobServiceSagaDbContext> contextFactory,
        ILockStatementProvider? lockStatementProvider = default)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(contextFactory);

        lockStatementProvider ??= new SqlServerLockStatementProvider();

        configurator.Repository = EntityFrameworkSagaRepository<JobTypeSaga>.CreatePessimistic(contextFactory, lockStatementProvider);

        configurator.JobRepository = EntityFrameworkSagaRepository<JobSaga>.CreatePessimistic(contextFactory, lockStatementProvider);

        configurator.JobAttemptRepository = EntityFrameworkSagaRepository<JobAttemptSaga>.CreatePessimistic(contextFactory, lockStatementProvider);
    }
}
