namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Resolves EF Core mappings and produces SQLite selection statements used under serializable transactions.</summary>
public class SqliteLockStatementProvider :
    SqlLockStatementProvider
{
    /// <summary>Initializes a provider that discovers table names from the EF Core model.</summary>
    public SqliteLockStatementProvider()
        : base(new SqliteLockStatementFormatter())
    {
    }

    /// <summary>Initializes a provider with a fallback schema value for model compatibility.</summary>
    /// <param name="schemaName">The fallback schema value; SQLite statements do not qualify table names with it.</param>
    public SqliteLockStatementProvider(string schemaName)
        : base(schemaName, new SqliteLockStatementFormatter())
    {
    }
}
