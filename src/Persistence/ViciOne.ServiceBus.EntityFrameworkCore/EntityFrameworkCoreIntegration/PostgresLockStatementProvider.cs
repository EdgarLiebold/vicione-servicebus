namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Provides a postgres lock statement provider implementation.
/// </summary>
public class PostgresLockStatementProvider :
    SqlLockStatementProvider
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public PostgresLockStatementProvider()
        : base(new PostgresLockStatementFormatter())
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="schemaName">The schema name value.</param>
    public PostgresLockStatementProvider(string schemaName)
        : base(schemaName, new PostgresLockStatementFormatter())
    {
    }
}
