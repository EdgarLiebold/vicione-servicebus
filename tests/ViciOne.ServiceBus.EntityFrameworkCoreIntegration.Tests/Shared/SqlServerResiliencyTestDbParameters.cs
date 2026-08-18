namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests.Shared
{
    using System;
    using System.Reflection;
    using ViciOne.ServiceBus.TestInfrastructure;
using ViciOne.ServiceBus.Tests;
    using Microsoft.EntityFrameworkCore;
    using TestFramework;


    public class SqlServerResiliencyTestDbParameters :
        ITestDbParameters
    {
        /// <summary>
        /// Get DB context options for SQL Server, with resiliency
        /// </summary>
        public DbContextOptionsBuilder<T> GetDbContextOptions<T>()
            where T : DbContext
        {
            var builder = new DbContextOptionsBuilder<T>();

            Apply(typeof(T), builder);

            return builder;
        }

        public void Apply(Type dbContextType, DbContextOptionsBuilder builder)
        {
            builder.UseSqlServer(TestDatabase.SqlServer(), m =>
            {
                m.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
                m.MigrationsHistoryTable($"__{dbContextType.Name}");
                m.EnableRetryOnFailure();
            });
        }

        public ILockStatementProvider RawSqlLockStatements => new SqlServerLockStatementProvider(false);
    }
}
