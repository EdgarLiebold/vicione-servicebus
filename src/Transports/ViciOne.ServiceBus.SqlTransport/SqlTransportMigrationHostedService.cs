using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Runs the configured SQL database provisioning stages at host start and optional cleanup at host stop.</summary>
public class SqlTransportMigrationHostedService :
    IHostedService
{
    readonly ILogger<SqlTransportMigrationHostedService> _logger;
    readonly ISqlTransportDatabaseMigrator _migrator;
    readonly SqlTransportMigrationOptions _options;
    readonly SqlTransportOptions _transportOptions;

    /// <summary>Creates the hosted lifecycle for a provider-specific SQL database migrator.</summary>
    /// <param name="migrator">The provider-specific database migrator.</param>
    /// <param name="logger">The diagnostic logger.</param>
    /// <param name="options">The enabled provisioning stages.</param>
    /// <param name="transportOptions">The target database connection and identity options.</param>
    public SqlTransportMigrationHostedService(ISqlTransportDatabaseMigrator migrator, ILogger<SqlTransportMigrationHostedService> logger,
        IOptions<SqlTransportMigrationOptions> options, IOptions<SqlTransportOptions> transportOptions)
    {
        _migrator = migrator ?? throw new ArgumentNullException(nameof(migrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _transportOptions = transportOptions?.Value ?? throw new ArgumentNullException(nameof(transportOptions));
    }

    /// <summary>Creates the enabled database, schema, and infrastructure stages in dependency order.</summary>
    /// <param name="cancellationToken">The token used to cancel provisioning.</param>
    /// <returns>A task that completes after every enabled startup stage.</returns>
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

    /// <summary>Deletes the transport database during shutdown when ephemeral cleanup is enabled.</summary>
    /// <param name="cancellationToken">The token used to cancel cleanup.</param>
    /// <returns>A task that completes after optional database deletion.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_options.DeleteDatabase)
        {
            _logger.LogInformation("Deleting Database {Database}", _transportOptions.Database);

            await _migrator.DeleteDatabaseAsync(_transportOptions, cancellationToken).ConfigureAwait(false);
        }
    }
}
