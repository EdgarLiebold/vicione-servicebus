using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

#nullable enable
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

public interface ILoadQueryExecutor<TSaga>
    where TSaga : class, ISaga
{
    Task<TSaga?> LoadAsync(DbContext dbContext, Guid correlationId, CancellationToken cancellationToken);
}
