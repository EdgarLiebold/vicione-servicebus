using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Routes selected pipeline failures through a projected rescue context.</summary>
/// <typeparam name="TContext">The original pipeline context type.</typeparam>
/// <typeparam name="TRescueContext">The context type supplied to the rescue pipe.</typeparam>
internal sealed class RescueFilter<TContext, TRescueContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
    where TRescueContext : class, PipeContext
{
    readonly IExceptionFilter _exceptionFilter;
    readonly RescueContextFactory<TContext, TRescueContext> _rescueContextFactory;
    readonly IPipe<TRescueContext> _rescuePipe;

    /// <summary>Creates a filter that projects selected failures into a rescue pipeline.</summary>
    /// <param name="rescuePipe">The pipeline that handles projected failures.</param>
    /// <param name="exceptionFilter">The filter that selects failures for rescue.</param>
    /// <param name="rescueContextFactory">Creates a rescue context from the failed context and exception.</param>
    public RescueFilter(IPipe<TRescueContext> rescuePipe, IExceptionFilter exceptionFilter,
        RescueContextFactory<TContext, TRescueContext> rescueContextFactory)
    {
        _rescuePipe = rescuePipe ?? throw new ArgumentNullException(nameof(rescuePipe));
        _exceptionFilter = exceptionFilter ?? throw new ArgumentNullException(nameof(exceptionFilter));
        _rescueContextFactory = rescueContextFactory ?? throw new ArgumentNullException(nameof(rescueContextFactory));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scope = context.CreateFilterScope("rescue");

        _rescuePipe.Probe(scope);
    }

    [DebuggerNonUserCode]
    async Task IFilter<TContext>.SendAsync(TContext context, IPipe<TContext> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        try
        {
            await next.SendAsync(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (!_exceptionFilter.Match(ex))
                throw;

            var rescueContext = _rescueContextFactory(context, ex)
                ?? throw new InvalidOperationException("The rescue context factory returned null.");

            await _rescuePipe.SendAsync(rescueContext).ConfigureAwait(false);
        }
    }
}
