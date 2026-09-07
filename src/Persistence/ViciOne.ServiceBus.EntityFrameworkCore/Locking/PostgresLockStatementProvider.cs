namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Resolves EF Core mappings and produces PostgreSQL locking statements.</summary>
public class PostgresLockStatementProvider :
    SqlLockStatementProvider
{
    /// <summary>Initializes a provider that discovers schemas from the EF Core model.</summary>
    public PostgresLockStatementProvider()
        : base(new PostgresLockStatementFormatter())
    {
    }

    /// <summary>Initializes a provider with a fallback schema when the EF Core model omits one.</summary>
    /// <param name="schemaName">The fallback PostgreSQL schema name.</param>
    public PostgresLockStatementProvider(string schemaName)
        : base(schemaName, new PostgresLockStatementFormatter())
    {
    }
}
