using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

public interface ICompensateActivityScopeProvider<TActivity, TLog> :
    IProbeSite
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    ValueTask<ICompensateScopeContext<TLog>> GetScopeAsync(CompensateContext<TLog> context, CancellationToken cancellationToken = default);

    ValueTask<ICompensateActivityScopeContext<TActivity, TLog>> GetActivityScopeAsync(CompensateContext<TLog> context, CancellationToken cancellationToken = default);
}
