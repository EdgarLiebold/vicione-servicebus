namespace ViciOne.ServiceBus.DbTransport.Tests;

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SqlTransport.PostgreSql;


public static class TestConfigurationExtensions
{
    public static IServiceCollection ConfigurePostgresTransport(this IServiceCollection services, bool create = true, bool delete = false,
        string database = RunScopedTransportEndpoint.SharedDatabase)
    {
        services.AddOptions<SqlTransportOptions>().Configure(options =>
        {
            options.UseRunScopedPostgres();
            options.Database = database;
            options.Schema = "transport";
            options.Role = "transport";
            options.Username = "unit_tests";
            options.Password = "H4rd2Gu3ss!";
        });

        services.AddPostgresMigrationHostedService(create, delete);

        return services;
    }

    public static PostgresSqlTransportConnection GetTransportConnection(this IServiceProvider provider)
    {
        return PostgresSqlTransportConnection.GetDatabaseConnection(provider.GetRequiredService<IOptions<SqlTransportOptions>>().Value);
    }
}
