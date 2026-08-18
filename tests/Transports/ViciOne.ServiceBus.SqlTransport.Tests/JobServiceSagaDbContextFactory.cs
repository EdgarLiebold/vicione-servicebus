namespace ViciOne.ServiceBus.SqlTransport.Tests
{
    using System.Reflection;
    using EntityFrameworkCoreIntegration;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Design;


    public class JobServiceSagaDbContextFactory :
        IDesignTimeDbContextFactory<JobServiceSagaDbContext>
    {
        public JobServiceSagaDbContext CreateDbContext(params string[] args)
        {
            var builder = new DbContextOptionsBuilder<JobServiceSagaDbContext>();

            Apply(builder);

            return new JobServiceSagaDbContext(builder.Options);
        }

        public static void Apply(DbContextOptionsBuilder builder)
        {
            builder.UseNpgsql(RunScopedTransportEndpoint.PostgresConnectionString("ViciOneServiceBus_transport_tests"), options =>
            {
                options.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
                options.MigrationsHistoryTable("job_service_db_context_ef");
            });
        }

        public JobServiceSagaDbContext CreateDbContext(DbContextOptionsBuilder<JobServiceSagaDbContext> optionsBuilder)
        {
            return new JobServiceSagaDbContext(optionsBuilder.Options);
        }
    }
}