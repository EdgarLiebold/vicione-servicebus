using System;
using ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

namespace ViciOne.ServiceBus;

public static class EntityFrameworkCoreJobServiceConfigurationExtensions
{
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
