// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests.SlowConcurrentSaga.DataAccess
{
    using System.Collections.Generic;
    using Microsoft.EntityFrameworkCore;


    public class SlowConcurrentSagaDbContext : SagaDbContext
    {
        public SlowConcurrentSagaDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override IEnumerable<ISagaClassMap> Configurations
        {
            get { yield return new SlowConcurrentSagaMap(); }
        }
    }
}
