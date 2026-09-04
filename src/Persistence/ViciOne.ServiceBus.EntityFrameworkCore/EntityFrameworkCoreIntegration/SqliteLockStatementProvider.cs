namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Provides a sqlite lock statement provider implementation.
/// </summary>
public class SqliteLockStatementProvider :
    SqlLockStatementProvider
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public SqliteLockStatementProvider()
        : base(new SqliteLockStatementFormatter())
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="schemaName">The schema name value.</param>
    public SqliteLockStatementProvider(string schemaName)
        : base(schemaName, new SqliteLockStatementFormatter())
    {
    }
}
