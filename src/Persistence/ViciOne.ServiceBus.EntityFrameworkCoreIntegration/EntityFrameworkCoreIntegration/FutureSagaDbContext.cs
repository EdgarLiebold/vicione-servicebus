using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

public class FutureSagaDbContext :
    SagaDbContext
{
    public FutureSagaDbContext(DbContextOptions<FutureSagaDbContext> options)
        : base(options)
    {
    }

    protected FutureSagaDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield return new FutureStateMap(false); }
    }
}
