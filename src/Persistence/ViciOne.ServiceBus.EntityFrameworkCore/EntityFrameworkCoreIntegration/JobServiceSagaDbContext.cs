using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>EF Core context containing pessimistic-concurrency mappings for all job-service sagas.</summary>
public class JobServiceSagaDbContext :
    SagaDbContext
{
    /// <summary>Initializes the pessimistic job-service saga DbContext.</summary>
    /// <param name="options">The options for this context type.</param>
    public JobServiceSagaDbContext(DbContextOptions<JobServiceSagaDbContext> options)
        : base(options)
    {
    }

    /// <summary>Gets mappings for job type, job, and job-attempt saga state.</summary>
    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get
        {
            yield return new JobTypeSagaMap(false);
            yield return new JobSagaMap(false);
            yield return new JobAttemptSagaMap(false);
        }
    }
}
