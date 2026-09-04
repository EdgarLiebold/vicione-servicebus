using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Defines the contract for lock statement provider.
/// </summary>
public interface ILockStatementProvider
{
    /// <summary>
    /// Gets row lock statement.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    string GetRowLockStatement<T>(DbContext context)
        where T : class;

    /// <summary>
    /// Returns the lock statement for the specified property (usable for any set)
    /// </summary>
    /// <param name="context"></param>
    /// <param name="propertyNames">One or more property names</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    string GetRowLockStatement<T>(DbContext context, params string[] propertyNames)
        where T : class;

    /// <summary>
    /// Gets outbox statement.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    string GetOutboxStatement(DbContext context);
    /// <summary>
    /// Gets inbox cleanup lock statement.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    string GetInboxCleanupLockStatement(DbContext context);
}
