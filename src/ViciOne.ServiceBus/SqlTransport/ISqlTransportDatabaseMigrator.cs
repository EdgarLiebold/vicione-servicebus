using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SqlTransport;

public interface ISqlTransportDatabaseMigrator
{
    Task CreateDatabaseAsync(SqlTransportOptions options, CancellationToken cancellationToken = default);
    Task CreateSchemaIfNotExistAsync(SqlTransportOptions options, CancellationToken cancellationToken = default);
    Task CreateInfrastructureAsync(SqlTransportOptions options, CancellationToken cancellationToken = default);
    Task DeleteDatabaseAsync(SqlTransportOptions options, CancellationToken cancellationToken = default);
}
