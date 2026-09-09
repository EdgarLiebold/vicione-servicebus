using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Saga;

/// <summary>Loads the saga rows selected by a repository query under the configured concurrency strategy.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal interface SagaLockContext<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Loads and tracks the selected saga rows.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The selected saga entities in repository-defined order.</returns>
    Task<IList<TSaga>> LoadAsync(CancellationToken cancellationToken = default);
}
