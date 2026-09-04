using System.Threading.Tasks;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Middleware;

public class ScopedCompensateFilter<TActivity, TArguments, TFilter> :
    IFilter<CompensateContext<TArguments>>
    where TActivity : class, ICompensateActivity<TArguments>
    where TArguments : class
    where TFilter : class, IFilter<CompensateContext<TArguments>>
{
    readonly ICompensateActivityScopeProvider<TActivity, TArguments> _scopeProvider;

    public ScopedCompensateFilter(ICompensateActivityScopeProvider<TActivity, TArguments> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public async Task SendAsync(CompensateContext<TArguments> context, IPipe<CompensateContext<TArguments>> next)
    {
        await using ICompensateScopeContext<TArguments> scope = await _scopeProvider.GetScopeAsync(context).ConfigureAwait(false);

        var filter = scope.GetService<TFilter>();

        await filter.SendAsync(scope.Context, next).ConfigureAwait(false);
    }

    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("scopedFilter");
        scope.Add("filter", TypeMetadataCache<TFilter>.ShortName);

        _scopeProvider.Probe(scope);
    }
}
