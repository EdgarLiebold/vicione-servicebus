using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Defines the contract for sql transport database migrator.
/// </summary>
public interface ISqlTransportDatabaseMigrator
{
    /// <summary>
    /// Creates database.
    /// </summary>
    /// <param name="options">The options value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task CreateDatabaseAsync(SqlTransportOptions options, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates schema if not exist.
    /// </summary>
    /// <param name="options">The options value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task CreateSchemaIfNotExistAsync(SqlTransportOptions options, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates infrastructure.
    /// </summary>
    /// <param name="options">The options value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task CreateInfrastructureAsync(SqlTransportOptions options, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the delete database operation.
    /// </summary>
    /// <param name="options">The options value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task DeleteDatabaseAsync(SqlTransportOptions options, CancellationToken cancellationToken = default);
}
