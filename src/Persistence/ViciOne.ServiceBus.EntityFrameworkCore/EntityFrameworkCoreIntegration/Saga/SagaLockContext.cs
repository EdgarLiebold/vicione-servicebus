using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Saga;

/// <summary>
/// Defines the contract for saga lock context.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
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
