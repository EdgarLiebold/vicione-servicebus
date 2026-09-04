using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Provides an optimistic job service saga db context implementation.
/// </summary>
public class OptimisticJobServiceSagaDbContext :
    SagaDbContext
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="options">The options value.</param>
    public OptimisticJobServiceSagaDbContext(DbContextOptions<OptimisticJobServiceSagaDbContext> options)
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
            yield return new JobTypeSagaMap(true);
            yield return new JobSagaMap(true);
            yield return new JobAttemptSagaMap(true);
        }
    }
}
