using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Saga;

public interface SagaLockContext<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>
    /// Execute the callback on each saga instance, and return a Task that waits on the results
    /// </summary>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<IList<TSaga>> LoadAsync(CancellationToken cancellationToken = default);
}
