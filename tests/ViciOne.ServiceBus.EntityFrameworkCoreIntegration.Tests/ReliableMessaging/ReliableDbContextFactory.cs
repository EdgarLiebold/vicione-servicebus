// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests.ReliableMessaging
{
    using System.Reflection;
    using ViciOne.ServiceBus.Tests;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Design;
    using TestFramework;


    public class ReliableDbContextFactory :
        IDesignTimeDbContextFactory<ReliableDbContext>
    {
        public ReliableDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<ReliableDbContext>();

            Apply(builder);

            return new ReliableDbContext(builder.Options);
        }

        public static void Apply(DbContextOptionsBuilder builder)
        {
            builder.UseSqlServer(LocalDbConnectionStringProvider.GetLocalDbConnectionString(), options =>
            {
                options.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
                options.MigrationsHistoryTable($"__{nameof(ReliableDbContext)}");

                options.EnableRetryOnFailure(5);
                options.MinBatchSize(1);
            });

            builder.EnableSensitiveDataLogging();
        }

        public ReliableDbContext CreateDbContext(DbContextOptionsBuilder<ReliableDbContext> optionsBuilder)
        {
            return new ReliableDbContext(optionsBuilder.Options);
        }
    }
}
