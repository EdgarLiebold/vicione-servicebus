using System.Threading.Tasks;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Middleware;

public class ScopedConsumeFilter<T, TFilter> :
    IFilter<ConsumeContext<T>>
    where T : class
    where TFilter : class, IFilter<ConsumeContext<T>>
{
    readonly IConsumeScopeProvider _scopeProvider;

    public ScopedConsumeFilter(IConsumeScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public async Task SendAsync(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        await using IConsumeScopeContext<T> scope = await _scopeProvider.GetScopeAsync(context).ConfigureAwait(false);

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
