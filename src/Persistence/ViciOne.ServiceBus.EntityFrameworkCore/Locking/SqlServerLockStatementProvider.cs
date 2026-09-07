namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Resolves EF Core mappings and produces SQL Server locking statements.</summary>
public class SqlServerLockStatementProvider :
    SqlLockStatementProvider
{
    /// <summary>Initializes a provider that discovers schemas from the EF Core model.</summary>
    /// <param name="serializable"><see langword="true"/> to add the SQL Server <c>SERIALIZABLE</c> table hint.</param>
    public SqlServerLockStatementProvider(bool serializable = false)
        : base(new SqlServerLockStatementFormatter(serializable))
    {
    }

    /// <summary>Initializes a provider with a fallback schema when the EF Core model omits one.</summary>
    /// <param name="schemaName">The fallback SQL Server schema name.</param>
    /// <param name="serializable"><see langword="true"/> to add the SQL Server <c>SERIALIZABLE</c> table hint.</param>
    public SqlServerLockStatementProvider(string schemaName, bool serializable = false)
        : base(schemaName, new SqlServerLockStatementFormatter(serializable))
    {
    }
}
