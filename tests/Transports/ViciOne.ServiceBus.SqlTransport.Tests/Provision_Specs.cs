namespace ViciOne.ServiceBus.DbTransport.Tests
{
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Testing;


    /// <summary>
    /// What provisioning has to produce, named once for both engines. Opening and closing a connection
    /// proves that a database exists, not that the transport can work in it; these are the objects every
    /// transport statement addresses.
    /// </summary>
    public static class TransportSchema
    {
        public const string Name = "transport";

        public static readonly string[] Tables =
        {
            "message",
            "messagedelivery",
            "queue",
            "queuemetric",
            "queuemetriccapture",
            "queuesubscription",
            "topic",
            "topicsubscription"
        };

        /// <summary>Normalises the two spellings the migrators use for the same table.</summary>
        public static IReadOnlyList<string> Normalise(IEnumerable<string> names)
        {
            return names.Select(name => name.Replace("_", string.Empty)).OrderBy(name => name).ToArray();
        }
    }


    [TestFixture]
    public class Provisioning_the_transport_database
    {
        [Test]
        [Order(1)]
        public async Task Should_create_the_required_schema_tables_and_indices()
        {
            await using var provider = new ServiceCollection()
                .ConfigurePostgresTransport(database: RunScopedTransportEndpoint.ProvisioningDatabase)
                .AddViciOneServiceBusTestHarness()
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            await using var connection = await provider.OpenTransport(TransportDialect.Postgres);

            IReadOnlyList<string> tables = TransportSchema.Normalise(
                await connection.SchemaTables(TransportSchema.Name));
            IReadOnlyList<string> indices = await connection.SchemaIndices(TransportDialect.Postgres, TransportSchema.Name);

            Assert.Multiple(() =>
            {
                Assert.That(tables, Is.SupersetOf(TransportSchema.Tables),
                    "provisioning did not create every table the transport addresses");
                Assert.That(indices, Does.Contain("message_delivery_fetch_ndx"),
                    "the index every fetch depends on is missing, so provisioning produced tables the transport cannot work on");
                Assert.That(indices, Does.Contain("unique_queue"),
                    "the unique queue index is missing, so a queue name could be created twice");
            });

            await harness.Stop();
        }

        [Test]
        [Order(2)]
        public async Task Should_drop_the_database_on_shutdown()
        {
            // The provider is not held by an await using here: the drop is what its disposal does, so
            // this case disposes it itself and exactly once.
            var provider = new ServiceCollection()
                .ConfigurePostgresTransport(delete: true, database: RunScopedTransportEndpoint.ProvisioningDatabase)
                .AddViciOneServiceBusTestHarness()
                .BuildServiceProvider(true);

            DbConnection server;
            try
            {
                var harness = provider.GetTestHarness();

                await harness.Start();

                // Taken while the provider is alive, used after it is gone.
                server = await provider.OpenServer(TransportDialect.Postgres);

                await harness.Stop();
            }
            finally
            {
                await provider.DisposeAsync();
            }

            await using (server)
            {
                Assert.That(await server.DatabaseExists(TransportDialect.Postgres, RunScopedTransportEndpoint.ProvisioningDatabase),
                    Is.False, "the database is still on the server after the shutdown that was configured to delete it");
            }
        }
    }
}
