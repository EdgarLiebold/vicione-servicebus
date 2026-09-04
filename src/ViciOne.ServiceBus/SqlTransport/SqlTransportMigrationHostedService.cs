using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Provides a sql transport migration hosted service implementation.
/// </summary>
public class SqlTransportMigrationHostedService :
    IHostedService
{
    readonly ILogger<SqlTransportMigrationHostedService> _logger;
    readonly ISqlTransportDatabaseMigrator _migrator;
    readonly SqlTransportMigrationOptions _options;
    readonly SqlTransportOptions _transportOptions;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="migrator">The migrator value.</param>
    /// <param name="logger">The logger value.</param>
    /// <param name="options">The options value.</param>
    /// <param name="dbOptions">The db options value.</param>
    public SqlTransportMigrationHostedService(ISqlTransportDatabaseMigrator migrator, ILogger<SqlTransportMigrationHostedService> logger,
        IOptions<SqlTransportMigrationOptions> options, IOptions<SqlTransportOptions> dbOptions)
    {
        _migrator = migrator;
        _logger = logger;
        _options = options.Value;
        _transportOptions = dbOptions.Value;
    }

    /// <summary>
    /// Starts the configured component.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_options.CreateDatabase)
        {
            _logger.LogInformation("ViciOne.ServiceBus SQL Transport creating database {Database}", _transportOptions.Database);

            await _migrator.CreateDatabaseAsync(_transportOptions, cancellationToken).ConfigureAwait(false);
        }

        if (_options.CreateSchema)
        {
            _logger.LogInformation("ViciOne.ServiceBus SQL Transport creating schema for database {Database}", _transportOptions.Database);

            await _migrator.CreateSchemaIfNotExistAsync(_transportOptions, cancellationToken).ConfigureAwait(false);
        }

        if (_options.CreateInfrastructure)
        {
            _logger.LogInformation("ViciOne.ServiceBus SQL Transport creating infrastructure for database {Database}", _transportOptions.Database);

            await _migrator.CreateInfrastructureAsync(_transportOptions, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Stops the configured component.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_options.DeleteDatabase)
        {
            _logger.LogInformation("Deleting Database {Database}", _transportOptions.Database);

            await _migrator.DeleteDatabaseAsync(_transportOptions, cancellationToken).ConfigureAwait(false);
        }
    }
}
