using System;
using ViciOne.ServiceBus.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Configures the job-service saga repositories to use EF Core.</summary>
public static class EntityFrameworkCoreJobServiceConfigurationExtensions
{
    /// <summary>Uses one DbContext factory and pessimistic lock provider for all three job-service saga repositories.</summary>
    /// <param name="configurator">The job-service configuration that receives the EF Core repositories.</param>
    /// <param name="contextFactory">The delegate that creates a job-service saga DbContext.</param>
    /// <param name="lockStatementProvider">The relational provider that generates row-lock SQL.</param>
    public static void UseEntityFrameworkCoreSagaRepository(this IJobServiceConfigurator configurator, Func<JobServiceSagaDbContext> contextFactory,
        ILockStatementProvider lockStatementProvider)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(contextFactory);
        ArgumentNullException.ThrowIfNull(lockStatementProvider);

        configurator.JobTypeRepository = EntityFrameworkSagaRepository<JobTypeSaga>.CreatePessimistic(contextFactory, lockStatementProvider);

        configurator.JobRepository = EntityFrameworkSagaRepository<JobSaga>.CreatePessimistic(contextFactory, lockStatementProvider);

        configurator.JobAttemptRepository = EntityFrameworkSagaRepository<JobAttemptSaga>.CreatePessimistic(contextFactory, lockStatementProvider);
    }
}
