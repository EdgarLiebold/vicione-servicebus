using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Resolves EF Core model mappings and returns provider-specific locking SQL.</summary>
public interface ILockStatementProvider
{
    /// <summary>Builds a row-lock query using the mapped <c>CorrelationId</c> property.</summary>
    /// <typeparam name="T">The mapped entity type.</typeparam>
    /// <param name="context">The DbContext whose model supplies table and column names.</param>
    /// <returns>Parameterized provider-specific SQL.</returns>
    string GetRowLockStatement<T>(DbContext context)
        where T : class;

    /// <summary>Returns the lock statement for the specified property (usable for any set).</summary>
    /// <typeparam name="T">The mapped entity type.</typeparam>
    /// <param name="context">The DbContext whose model supplies table and column names.</param>
    /// <param name="propertyNames">One or more mapped properties used as equality predicates.</param>
    /// <returns>Parameterized provider-specific SQL.</returns>
    string GetRowLockStatement<T>(DbContext context, params string[] propertyNames)
        where T : class;

    /// <summary>Builds a query that locks the next due outbox row for one bus.</summary>
    /// <param name="context">The DbContext whose model supplies outbox mappings.</param>
    /// <returns>Parameterized provider-specific SQL.</returns>
    string GetOutboxStatement(DbContext context);
    /// <summary>Builds a transaction-scoped, non-blocking ownership query for inbox cleanup.</summary>
    /// <param name="context">The DbContext whose model supplies inbox mappings.</param>
    /// <returns>Provider-specific SQL that returns one when ownership is acquired.</returns>
    string GetInboxCleanupLockStatement(DbContext context);
}
