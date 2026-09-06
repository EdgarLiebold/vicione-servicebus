using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Rescue catches an exception, and if the exception matches the exception filter,
/// passes control to the rescue pipe.
/// </summary>
/// <typeparam name="TContext">The context type.</typeparam>
/// <typeparam name="TRescueContext">The rescue context type.</typeparam>
public class RescueFilter<TContext, TRescueContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
    where TRescueContext : class, PipeContext
{
    readonly IExceptionFilter _exceptionFilter;
    readonly RescueContextFactory<TContext, TRescueContext> _rescueContextFactory;
    readonly IPipe<TRescueContext> _rescuePipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="rescuePipe">The rescue pipe.</param>
    /// <param name="exceptionFilter">The exception filter.</param>
    /// <param name="rescueContextFactory">The rescue context factory.</param>
    public RescueFilter(IPipe<TRescueContext> rescuePipe, IExceptionFilter exceptionFilter,
        RescueContextFactory<TContext, TRescueContext> rescueContextFactory)
    {
        _rescuePipe = rescuePipe ?? throw new ArgumentNullException(nameof(rescuePipe));
        _exceptionFilter = exceptionFilter ?? throw new ArgumentNullException(nameof(exceptionFilter));
        _rescueContextFactory = rescueContextFactory ?? throw new ArgumentNullException(nameof(rescueContextFactory));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("rescue");

        _rescuePipe.Probe(scope);
    }

    [DebuggerNonUserCode]
    async Task IFilter<TContext>.SendAsync(TContext context, IPipe<TContext> next)
    {
        try
        {
            await next.SendAsync(context).ConfigureAwait(false);
        }
        catch (AggregateException ex)
        {
            if (!_exceptionFilter.Match(ex.GetBaseException()))
                throw;

            var rescueContext = _rescueContextFactory(context, ex)
                ?? throw new InvalidOperationException("The rescue context factory returned null.");

            await _rescuePipe.SendAsync(rescueContext).ConfigureAwait(false);
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
