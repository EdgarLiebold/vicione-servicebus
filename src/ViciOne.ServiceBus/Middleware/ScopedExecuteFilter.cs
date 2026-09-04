using System.Threading.Tasks;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Middleware;

public class ScopedExecuteFilter<TActivity, TArguments, TFilter> :
    IFilter<ExecuteContext<TArguments>>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
    where TFilter : class, IFilter<ExecuteContext<TArguments>>
{
    readonly IExecuteActivityScopeProvider<TActivity, TArguments> _scopeProvider;

    public ScopedExecuteFilter(IExecuteActivityScopeProvider<TActivity, TArguments> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public async Task SendAsync(ExecuteContext<TArguments> context, IPipe<ExecuteContext<TArguments>> next)
    {
        await using IExecuteScopeContext<TArguments> scope = await _scopeProvider.GetScopeAsync(context).ConfigureAwait(false);

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
