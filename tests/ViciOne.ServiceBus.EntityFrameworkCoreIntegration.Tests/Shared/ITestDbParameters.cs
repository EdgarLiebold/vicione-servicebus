// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests.Shared
{
    using System;
    using Microsoft.EntityFrameworkCore;


    public interface ITestDbParameters
    {
        ILockStatementProvider RawSqlLockStatements { get; }

        DbContextOptionsBuilder<T> GetDbContextOptions<T>()
            where T : DbContext;

        void Apply(Type dbContextType, DbContextOptionsBuilder builder);
    }
}
