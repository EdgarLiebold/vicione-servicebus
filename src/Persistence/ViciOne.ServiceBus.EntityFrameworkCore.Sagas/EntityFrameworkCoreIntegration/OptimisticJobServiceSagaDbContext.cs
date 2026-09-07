using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>EF Core context containing row-versioned mappings for all job-service sagas.</summary>
public class OptimisticJobServiceSagaDbContext :
    SagaDbContext
{
    /// <summary>Initializes the row-versioned job-service saga DbContext.</summary>
    /// <param name="options">The options for this context type.</param>
    public OptimisticJobServiceSagaDbContext(DbContextOptions<OptimisticJobServiceSagaDbContext> options)
        : base(options)
    {
    }

    /// <summary>Gets row-versioned mappings for job type, job, and job-attempt saga state.</summary>
    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get
        {
            yield return new JobTypeSagaMap(true);
            yield return new JobSagaMap(true);
            yield return new JobAttemptSagaMap(true);
        }
    }
}
