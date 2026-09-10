using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Defines the operations required by sql transport database migrator.</summary>
public interface ISqlTransportDatabaseMigrator
{
    /// <summary>Creates database.</summary>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CreateDatabaseAsync(SqlTransportOptions options, CancellationToken cancellationToken = default);
    /// <summary>Creates schema if not exist.</summary>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CreateSchemaIfNotExistAsync(SqlTransportOptions options, CancellationToken cancellationToken = default);
    /// <summary>Creates infrastructure.</summary>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CreateInfrastructureAsync(SqlTransportOptions options, CancellationToken cancellationToken = default);
    /// <summary>Deletes database.</summary>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteDatabaseAsync(SqlTransportOptions options, CancellationToken cancellationToken = default);
}
