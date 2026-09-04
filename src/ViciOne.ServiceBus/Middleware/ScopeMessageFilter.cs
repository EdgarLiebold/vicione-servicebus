using System.Threading.Tasks;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Middleware;

public class ScopeMessageFilter<T> :
    IFilter<ConsumeContext<T>>
    where T : class
{
    readonly IConsumeScopeProvider _scopeProvider;

    public ScopeMessageFilter(IConsumeScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public async Task SendAsync(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        await using IConsumeScopeContext<T> scope = await _scopeProvider.GetScopeAsync(context).ConfigureAwait(false);

        await next.SendAsync(scope.Context).ConfigureAwait(false);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("scope");
    }
}
