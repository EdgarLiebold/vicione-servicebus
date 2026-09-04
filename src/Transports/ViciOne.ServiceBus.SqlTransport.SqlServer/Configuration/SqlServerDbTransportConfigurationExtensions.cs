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

        services.AddOptions<SqlTransportOptions>();
        services.AddOptions<SqlTransportMigrationOptions>()
            .Configure(options =>
            {
                options.CreateDatabase = true;
                options.CreateSchema = true;
                options.CreateInfrastructure = true;
                options.DeleteDatabase = false;

                configure?.Invoke(options);
            });
        services.AddHostedService<SqlTransportMigrationHostedService>();

        return services;
    }
}
