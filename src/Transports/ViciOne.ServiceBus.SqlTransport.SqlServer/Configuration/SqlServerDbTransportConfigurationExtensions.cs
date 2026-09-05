using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.SqlTransport.SqlServer;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

/// <summary>
/// Provides extension methods for sql server db transport configuration.
/// </summary>
public static class SqlServerDbTransportConfigurationExtensions
{
    /// <summary>
    /// Adds sql server migration hosted service to the configuration.
    /// </summary>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <param name="create">The create value.</param>
    /// <param name="delete">The delete value.</param>
    /// <returns>The result of the operation.</returns>
    public static IServiceCollection AddSqlServerMigrationHostedService(this IServiceCollection services, bool create = true, bool delete = false)
    {
        services.AddSqlServerMigrationHostedService(options =>
        {
            options.CreateDatabase = create;
            options.CreateSchema = create;
            options.CreateInfrastructure = create;
            options.DeleteDatabase = delete;
        });

        return services;
    }

    /// <summary>
    /// Adds sql server migration hosted service to the configuration.
    /// </summary>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public static IServiceCollection AddSqlServerMigrationHostedService(this IServiceCollection services, Action<SqlTransportMigrationOptions>? configure)
    {
        services.AddTransient<ISqlTransportDatabaseMigrator, SqlServerDatabaseMigrator>();

        services.AddOptions<SqlTransportOptions>()
            .Validate(
                static options => !string.IsNullOrWhiteSpace(options.ConnectionString)
                    || (!string.IsNullOrWhiteSpace(options.Host) && !string.IsNullOrWhiteSpace(options.Database)),
                "SQL Server migration for bus 'default': ConnectionString or both Host and Database must be declared. Configure a complete database address.")
            .Validate(
                static options => options.Port is null or > 0 and <= 65535,
                "SQL Server migration for bus 'default': Port must be between 1 and 65535 when specified. Set a valid TCP port or leave it unset.")
            .Validate(
                static options => options.ConnectionLimit is null or > 0,
                "SQL Server migration for bus 'default': ConnectionLimit must be greater than zero when specified. Set a positive limit or leave it unset.")
            .ValidateOnStart();
        services.AddOptions<SqlTransportMigrationOptions>()
            .Configure(options =>
            {
                options.CreateDatabase = true;
                options.CreateSchema = true;
                options.CreateInfrastructure = true;
                options.DeleteDatabase = false;

                configure?.Invoke(options);
            })
            .Validate(
                static options => options.CreateDatabase || options.CreateSchema || options.CreateInfrastructure || options.DeleteDatabase,
                "SQL Server migration for bus 'default': no migration operation is selected. Select at least one create or delete operation, or do not register the migration service.")
            .ValidateOnStart();
        services.AddHostedService<SqlTransportMigrationHostedService>();

        return services;
    }
}
