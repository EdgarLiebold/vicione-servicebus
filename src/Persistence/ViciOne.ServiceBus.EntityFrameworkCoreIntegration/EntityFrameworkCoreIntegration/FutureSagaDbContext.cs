// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration
{
    using System.Collections.Generic;
    using Microsoft.EntityFrameworkCore;


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
}
