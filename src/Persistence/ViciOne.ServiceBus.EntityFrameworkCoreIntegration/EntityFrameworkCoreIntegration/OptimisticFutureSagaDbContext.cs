using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

public class OptimisticFutureSagaDbContext :
    FutureSagaDbContext
{
    public OptimisticFutureSagaDbContext(DbContextOptions<OptimisticFutureSagaDbContext> options)
        : base(options)
    {
    }

    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield return new FutureStateMap(true); }
    }
}
