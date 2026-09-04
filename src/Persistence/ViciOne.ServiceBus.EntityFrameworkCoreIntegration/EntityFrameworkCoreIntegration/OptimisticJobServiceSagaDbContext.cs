using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

public class OptimisticJobServiceSagaDbContext :
    SagaDbContext
{
    public OptimisticJobServiceSagaDbContext(DbContextOptions<OptimisticJobServiceSagaDbContext> options)
        : base(options)
    {
    }

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
