using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SqlTransport;

public interface ISqlTransportDatabaseMigrator
{
    Task CreateDatabase(SqlTransportOptions options, CancellationToken cancellationToken = default);
    Task CreateSchemaIfNotExist(SqlTransportOptions options, CancellationToken cancellationToken = default);
    Task CreateInfrastructure(SqlTransportOptions options, CancellationToken cancellationToken = default);
    Task DeleteDatabase(SqlTransportOptions options, CancellationToken cancellationToken = default);
}
