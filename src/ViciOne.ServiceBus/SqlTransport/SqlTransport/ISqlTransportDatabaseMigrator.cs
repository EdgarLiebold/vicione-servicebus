// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SqlTransport
{
    using System.Threading;
    using System.Threading.Tasks;


    public interface ISqlTransportDatabaseMigrator
    {
        Task CreateDatabase(SqlTransportOptions options, CancellationToken cancellationToken = default);
        Task CreateSchemaIfNotExist(SqlTransportOptions options, CancellationToken cancellationToken = default);
        Task CreateInfrastructure(SqlTransportOptions options, CancellationToken cancellationToken = default);
        Task DeleteDatabase(SqlTransportOptions options, CancellationToken cancellationToken = default);
    }
}
