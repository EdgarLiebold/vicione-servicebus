using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

internal sealed class ReliableLockStatementProvider : ILockStatementProvider
{
    public string GetRowLockStatement<T>(DbContext context)
        where T : class => Select(context).GetRowLockStatement<T>(context);

    public string GetRowLockStatement<T>(DbContext context, params string[] propertyNames)
        where T : class => Select(context).GetRowLockStatement<T>(context, propertyNames);

    public string GetOutboxStatement(DbContext context) => Select(context).GetOutboxStatement(context);

    public string GetInboxCleanupLockStatement(DbContext context) => Select(context).GetInboxCleanupLockStatement(context);

    static ILockStatementProvider Select(DbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Database.ProviderName switch
        {
            "Microsoft.EntityFrameworkCore.Sqlite" => new SqliteLockStatementProvider(),
            "Npgsql.EntityFrameworkCore.PostgreSQL" => new PostgreSqlLockStatementProvider(),
            "Microsoft.EntityFrameworkCore.SqlServer" => new SqlServerLockStatementProvider(serializable: true),
            string provider => throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "Reliable messaging",
                    "unknown",
                    $"EF provider '{provider}' has no lock-statement adapter.",
                    "Use SQLite, PostgreSQL or SQL Server, or supply a provider adapter")),
            null => throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "Reliable messaging",
                    "unknown",
                    "The EF provider name is unavailable.",
                    "Configure a relational DbContext before starting the host")),
        };
    }
}
