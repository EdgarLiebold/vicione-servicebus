using System.Threading.Tasks;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Middleware;

public class ScopeConsumeFilter :
    IFilter<ConsumeContext>
{
    readonly IConsumeScopeProvider _scopeProvider;

    public ScopeConsumeFilter(IConsumeScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public async Task SendAsync(ConsumeContext context, IPipe<ConsumeContext> next)
    {
        await using var scope = await _scopeProvider.GetScopeAsync(context).ConfigureAwait(false);

        await next.SendAsync(scope.Context).ConfigureAwait(false);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("scope");
    }
}
