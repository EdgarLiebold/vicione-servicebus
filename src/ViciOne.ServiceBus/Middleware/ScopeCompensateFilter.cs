using System.Threading.Tasks;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Middleware;

public class ScopeCompensateFilter<TActivity, TLog> :
    IFilter<CompensateContext<TLog>>
    where TLog : class
    where TActivity : class, ICompensateActivity<TLog>
{
    readonly ICompensateActivityScopeProvider<TActivity, TLog> _scopeProvider;

    public ScopeCompensateFilter(ICompensateActivityScopeProvider<TActivity, TLog> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public async Task SendAsync(CompensateContext<TLog> context, IPipe<CompensateContext<TLog>> next)
    {
        await using ICompensateScopeContext<TLog> scope = await _scopeProvider.GetScopeAsync(context).ConfigureAwait(false);

        await next.SendAsync(scope.Context).ConfigureAwait(false);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("scope");
    }
}
