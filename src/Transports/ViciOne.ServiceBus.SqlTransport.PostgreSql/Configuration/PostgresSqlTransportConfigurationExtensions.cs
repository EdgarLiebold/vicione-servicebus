using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.SqlTransport.PostgreSql;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>
/// Provides extension methods for postgres sql transport configuration.
/// </summary>
public static class PostgresSqlTransportConfigurationExtensions
{
    /// <summary>
    /// Adds postgres migration hosted service to the configuration.
    /// </summary>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <param name="create">The create value.</param>
    /// <param name="delete">The delete value.</param>
    /// <returns>The result of the operation.</returns>
    public static IServiceCollection AddPostgresMigrationHostedService(this IServiceCollection services, bool create = true, bool delete = false)
    {
        services.AddPostgresMigrationHostedService(options =>
        {
            options.CreateDatabase = create;
            options.CreateSchema = create;
            options.CreateInfrastructure = create;
            options.DeleteDatabase = delete;
        });

        return services;
    }

    /// <summary>
    /// Adds postgres migration hosted service to the configuration.
    /// </summary>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public static IServiceCollection AddPostgresMigrationHostedService(this IServiceCollection services, Action<SqlTransportMigrationOptions>? configure)
    {
        services.AddTransient<ISqlTransportDatabaseMigrator, PostgresDatabaseMigrator>();

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
