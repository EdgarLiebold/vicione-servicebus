using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Agents;

/// <summary>
/// Maintains a cached context, which is created upon first use, and recreated whenever a fault is propagated to the usage.
/// </summary>
/// <typeparam name="TContext"></typeparam>
public class PipeContextSupervisor<TContext> :
    Supervisor,
    ISupervisor<TContext>
    where TContext : class, PipeContext
{
    readonly ISupervisor _activeSupervisor;
    readonly IPipeContextFactory<TContext> _contextFactory;
    readonly object _contextLock = new object();
    PipeContextHandle<TContext>? _context;

    /// <summary>
    /// Create the cache
    /// </summary>
    /// <param name="contextFactory">Factory used to create the underlying and active contexts</param>
    public PipeContextSupervisor(IPipeContextFactory<TContext> contextFactory)
    {
        _contextFactory = contextFactory;

        _activeSupervisor = new Supervisor();
    }

    protected bool HasContext
    {
        get
        {
            lock (_contextLock)
                return _context is { IsDisposed: false };
        }
    }

    public async Task SendAsync(IPipe<TContext> pipe, CancellationToken cancellationToken)
    {
        IActivePipeContextAgent<TContext> activeContext = CreateActiveContext(cancellationToken);

        try
        {
            var context = activeContext.Context.Status == TaskStatus.RanToCompletion
                ? activeContext.Context.Result
                : await activeContext.Context.ConfigureAwait(false);

            await pipe.SendAsync(context).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // Fault notification is cleanup. Its failure must never replace the operation failure
            // that determines whether the caller may safely retry.
            try
            {
                await activeContext.FaultedAsync(exception, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception faultException)
            {
                LogContext.Error?.Log(faultException, "Context fault notification failed; the primary operation failure is preserved: {ContextType}",
                    TypeCache<TContext>.ShortName);
            }

            throw;
        }
        finally
        {
            // Cleanup failures remain diagnostic. Reporting them as operation failures after a
            // successful send could trigger a duplicate; replacing a real failure would hide its cause.
            try
            {
                await activeContext.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception stopException)
            {
                LogContext.Error?.Log(stopException, "Context stop failed; the operation result is preserved: {ContextType}", TypeCache<TContext>.ShortName);
            }

            try
            {
                await activeContext.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception disposeException)
            {
                LogContext.Error?.Log(disposeException, "Context disposal failed; the operation result is preserved: {ContextType}",
                    TypeCache<TContext>.ShortName);
            }
        }
    }


    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("source");
        scope.Set(new
        {
            Type = TypeCache<PipeContextSupervisor<TContext>>.ShortName,
            HasContext,
        });
    }

    protected override async Task StopSupervisorAsync(StopSupervisorContext context)
    {
        SetCompleted(ActiveAndActualAgentsCompletedAsync(context));

        await _activeSupervisor.StopAsync(context).ConfigureAwait(false);

        await Task.WhenAll(context.Agents.Select(x => x.StopAsync(context))).OrCanceledAsync(context.CancellationToken).ConfigureAwait(false);

        await Completed.OrCanceledAsync(context.CancellationToken).ConfigureAwait(false);
    }

    async Task ActiveAndActualAgentsCompletedAsync(StopSupervisorContext context)
    {
        await _activeSupervisor.Completed.ConfigureAwait(false);

        await Task.WhenAll(context.Agents.Select(x => x.Completed)).ConfigureAwait(false);
    }

    IActivePipeContextAgent<TContext> CreateActiveContext(CancellationToken cancellationToken)
    {
        PipeContextHandle<TContext> pipeContextHandle = GetContext();

        return _contextFactory.CreateActiveContext(_activeSupervisor, pipeContextHandle, cancellationToken);
    }

    PipeContextHandle<TContext> GetContext()
    {
        lock (_contextLock)
        {
            if (_context is { IsDisposed: false })
                return _context;

            PipeContextHandle<TContext> context = _context = _contextFactory.CreateContext(this);

            void ClearContext(Task task)
            {
                Interlocked.CompareExchange(ref _context, null, context);
            }

            context.Context.ContinueWith(ClearContext, CancellationToken.None, TaskContinuationOptions.NotOnRanToCompletion, TaskScheduler.Default);

            SetReady(context.Context);

            return context;
        }
    }
}
