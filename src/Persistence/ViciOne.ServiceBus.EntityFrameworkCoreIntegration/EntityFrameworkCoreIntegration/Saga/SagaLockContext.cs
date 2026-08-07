// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Saga
{
    using System.Collections.Generic;
    using System.Threading.Tasks;


    public interface SagaLockContext<TSaga>
        where TSaga : class, ISaga
    {
        /// <summary>
        /// Execute the callback on each saga instance, and return a Task that waits on the results
        /// </summary>
        /// <returns></returns>
        Task<IList<TSaga>> Load();
    }
}
