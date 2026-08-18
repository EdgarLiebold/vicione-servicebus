namespace ViciOne.ServiceBus.DbTransport.Tests.SqlServer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using NUnit.Framework;
    using SqlTransport.SqlServer;
    using Testing;


    [TestFixture]
    public class Provisioning_the_transport_database
    {
        [Test]
        [Order(1)]
        public async Task Should_create_the_required_schema_tables_and_indices()
        {
            await using var provider = new ServiceCollection()
                .ConfigureSqlServerTransport(database: RunScopedTransportEndpoint.ProvisioningDatabase)
                .AddViciOneServiceBusTestHarness()
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            await using var connection = await provider.OpenTransport(TransportDialect.SqlServer);

            IReadOnlyList<string> tables = TransportSchema.Normalise(
                await connection.SchemaTables(TransportSchema.Name));
            IReadOnlyList<string> indices = await connection.SchemaIndices(TransportDialect.SqlServer, TransportSchema.Name);

            Assert.Multiple(() =>
            {
                Assert.That(tables, Is.SupersetOf(TransportSchema.Tables),
                    "provisioning did not create every table the transport addresses");
                Assert.That(indices, Is.Not.Empty,
                    "provisioning created tables without a single index, so the transport would scan them");
                Assert.That(indices.Any(name => name.Contains("messagedelivery")), Is.True,
                    "the message delivery table carries no index of its own, which every fetch depends on");
            });

            await harness.Stop();
        }

        [Test]
        [Order(2)]
        public async Task Should_drop_the_database_on_shutdown()
        {
            await using var provider = new ServiceCollection()
                .ConfigureSqlServerTransport(delete: true, database: RunScopedTransportEndpoint.ProvisioningDatabase)
                .AddViciOneServiceBusTestHarness()
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            // The server connection is taken while the provider is alive; the drop happens on its disposal.
            var server = await provider.OpenServer(TransportDialect.SqlServer);

            await harness.Stop();
            await provider.DisposeAsync();

            await using (server)
            {
                Assert.That(await server.DatabaseExists(TransportDialect.SqlServer, RunScopedTransportEndpoint.ProvisioningDatabase),
                    Is.False, "the database is still on the server after the shutdown that was configured to delete it");
            }
        }
    }


    public static class TestConfigurationExtensions
    {
        public static IServiceCollection ConfigureSqlServerTransport(this IServiceCollection services, bool create = true, bool delete = false,
            string database = RunScopedTransportEndpoint.SharedDatabase)
        {
            services.AddOptions<SqlTransportOptions>().Configure(options =>
            {
                options.UseRunScopedSqlServer();
                options.Database = database;
                options.Schema = "transport";
                options.Role = "transport";
                options.Username = "unit_tests";
                options.Password = "H4rd2Gu3ss!";
            });

            services.AddSqlServerMigrationHostedService(create, delete);

            return services;
        }

        public static SqlServerSqlTransportConnection GetTransportConnection(this IServiceProvider provider)
        {
            return SqlServerSqlTransportConnection.GetDatabaseConnection(provider.GetRequiredService<IOptions<SqlTransportOptions>>().Value);
        }
    }
}
