using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Intercepts the pipe and executes an adjacent pipe prior to executing the next filter in the main pipe
/// </summary>
/// <typeparam name="TContext"></typeparam>
public class InterceptFilter<TContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
{
    readonly IPipe<TContext> _pipe;

    public InterceptFilter(IPipe<TContext> pipe)
    {
        _pipe = pipe;
    }

    async Task IFilter<TContext>.SendAsync(TContext context, IPipe<TContext> next)
    {
        await _pipe.SendAsync(context).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("wiretap");

        _pipe.Probe(scope);
    }
}
