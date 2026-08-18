namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests.Shared
{
    using ViciOne.ServiceBus.TestInfrastructure;
using ViciOne.ServiceBus.Tests;
    using System;
    using System.Reflection;
    using Microsoft.EntityFrameworkCore;


    public class PostgresTestDbParameters :
        ITestDbParameters
    {
        public DbContextOptionsBuilder<T> GetDbContextOptions<T>()
            where T : DbContext
        {
            var builder = new DbContextOptionsBuilder<T>();

            Apply(typeof(T), builder);

            return builder;
        }

        public void Apply(Type dbContextType, DbContextOptionsBuilder builder)
        {
            // The endpoint and account come from the fixture the runner started. The literal that stood
            // here addressed the default port with a well known account, so it measured whatever held
            // that port rather than the pinned fixture.
            builder.UseNpgsql(TestDatabase.Postgres(TestDatabase.PostgresPersistence), m =>
            {
                m.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
                m.MigrationsHistoryTable($"__{dbContextType.Name}");
            });
        }

        public ILockStatementProvider RawSqlLockStatements => new PostgresLockStatementProvider(false);
    }
}
