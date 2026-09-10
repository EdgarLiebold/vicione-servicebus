namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Resolves EF Core mappings and produces PostgreSQL locking statements.</summary>
public class PostgreSqlLockStatementProvider :
    SqlLockStatementProvider
{
    /// <summary>Initializes a provider that discovers schemas from the EF Core model.</summary>
    public PostgreSqlLockStatementProvider()
        : base(new PostgreSqlLockStatementFormatter())
    {
    }

    /// <summary>Initializes a provider with a fallback schema when the EF Core model omits one.</summary>
    /// <param name="schemaName">The fallback PostgreSQL schema name.</param>
    public PostgreSqlLockStatementProvider(string schemaName)
        : base(schemaName, new PostgreSqlLockStatementFormatter())
    {
    }
}
