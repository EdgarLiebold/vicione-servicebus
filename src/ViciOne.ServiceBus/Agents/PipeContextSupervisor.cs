using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Agents;

/// <summary>Creates a pipe context on demand, caches it while valid, and replaces it after invalidation.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class PipeContextSupervisor<TContext> :
    Supervisor,
    ISupervisor<TContext>
    where TContext : class, PipeContext
{
    readonly ISupervisor _activeSupervisor;
    readonly IPipeContextFactory<TContext> _contextFactory;
    readonly object _contextLock = new object();
    PipeContextHandle<TContext>? _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="contextFactory">Factory used to create the underlying and active contexts.</param>
    public PipeContextSupervisor(IPipeContextFactory<TContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

        _activeSupervisor = new Supervisor();
    }

    /// <summary>Gets a value indicating whether this instance has context.</summary>
    protected bool HasContext
    {
        get
        {
            lock (_contextLock)
                return _context is { IsDisposed: false };
        }
    }

    /// <summary>Executes a pipe using an active handle for the cached context.</summary>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(IPipe<TContext> pipe, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pipe);
        cancellationToken.ThrowIfCancellationRequested();

        IActivePipeContextAgent<TContext> activeContext = CreateActiveContext(cancellationToken);

        try
        {
            TContext context = activeContext.Context.Status == TaskStatus.RanToCompletion
                ? activeContext.Context.Result
                : await activeContext.Context.ConfigureAwait(false);
            if (context is null)
                throw new InvalidOperationException($"The active context handle completed without a {TypeCache<TContext>.ShortName} context.");

            await pipe.SendAsync(context).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // Fault notification is cleanup. Its failure must never replace the operation failure
            // that determines whether the caller may safely retry.
            try
            {
                await activeContext.FaultedAsync(exception).ConfigureAwait(false);
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

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateScope("source");
        scope.Set(new
        {
            Type = TypeCache<PipeContextSupervisor<TContext>>.ShortName,
            HasContext,
        });
    }

    /// <summary>Stops supervisor.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

        return _contextFactory.CreateActiveContext(_activeSupervisor, pipeContextHandle, cancellationToken)
            ?? throw new InvalidOperationException($"The context factory returned no active {TypeCache<TContext>.ShortName} context.");
    }

    PipeContextHandle<TContext> GetContext()
    {
        lock (_contextLock)
        {
            if (_context is { IsDisposed: false })
                return _context;

            PipeContextHandle<TContext> context = _contextFactory.CreateContext(this)
                ?? throw new InvalidOperationException($"The context factory returned no {TypeCache<TContext>.ShortName} context.");
            _context = context;

            void ClearContext()
            {
                Interlocked.CompareExchange(ref _context, null, context);
            }

            context.Context.ContinueWith(_ => ClearContext(), CancellationToken.None, TaskContinuationOptions.NotOnRanToCompletion, TaskScheduler.Default);

            SetReady(context.Context);

            return context;
        }
    }
}
