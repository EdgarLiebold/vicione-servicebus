using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Provides a job service saga db context implementation.
/// </summary>
public class JobServiceSagaDbContext :
    SagaDbContext
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="options">The options value.</param>
    public JobServiceSagaDbContext(DbContextOptions<JobServiceSagaDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the configurations value.
    /// </summary>
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
