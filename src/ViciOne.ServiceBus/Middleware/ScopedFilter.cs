using System.Threading.Tasks;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Middleware;

public class ScopedFilter<TContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
{
    readonly IFilterScopeProvider<TContext> _scopeProvider;

    public ScopedFilter(IFilterScopeProvider<TContext> scopeProvider)
    {
        _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
    }

    public async Task SendAsync(TContext context, IPipe<TContext> next)
    {
        await using IFilterScopeContext<TContext> scope = _scopeProvider.Create(context);

        await scope.Filter.SendAsync(scope.Context, next).ConfigureAwait(false);
    }

    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("scopedFilter");

        _scopeProvider.Probe(scope);
    }
}
