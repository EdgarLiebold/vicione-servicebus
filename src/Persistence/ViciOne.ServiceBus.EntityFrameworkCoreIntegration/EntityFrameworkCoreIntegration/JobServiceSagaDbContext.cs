// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration
{
    using System.Collections.Generic;
    using Microsoft.EntityFrameworkCore;


    public class JobServiceSagaDbContext :
        SagaDbContext
    {
        public JobServiceSagaDbContext(DbContextOptions<JobServiceSagaDbContext> options)
            : base(options)
        {
        }

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
}
