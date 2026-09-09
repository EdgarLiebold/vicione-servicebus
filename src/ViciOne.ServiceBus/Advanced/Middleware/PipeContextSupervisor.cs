using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Creates a pipe context on demand, caches it while valid, and replaces it after invalidation.</summary>
/// <typeparam name="TContext">The pipe context type.</typeparam>
public class PipeContextSupervisor<TContext> :
    Supervisor,
    ISupervisor<TContext>
    where TContext : class, PipeContext
{
    readonly ISupervisor _activeSupervisor;
    readonly IPipeContextFactory<TContext> _contextFactory;
    readonly object _contextLock = new object();
    IPipeContextHandle<TContext>? _context;

    /// <summary>Initializes a supervisor with the factory that owns context creation and borrowing.</summary>
    /// <param name="contextFactory">Factory used to create the underlying and active contexts.</param>
    public PipeContextSupervisor(IPipeContextFactory<TContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

        _activeSupervisor = new Supervisor();
    }

    /// <summary>Gets whether a reusable owned context is currently available.</summary>
    protected bool HasContext
    {
        get
        {
            lock (_contextLock)
                return _context is { IsDisposed: false };
        }
    }

    /// <summary>Sends the cached context through a pipe within one supervised borrowed use.</summary>
    /// <param name="pipe">The pipe that receives the context.</param>
    /// <param name="cancellationToken">The token that cancels context acquisition or the pipe operation.</param>
    /// <returns>A task that completes with the pipe operation; cleanup failures remain diagnostic.</returns>
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
                LogCleanupFailure(
                    faultException,
                    "Context fault notification failed; the primary operation failure is preserved: {ContextType}");
            }

            throw;
        }
        finally
        {
            // Cleanup failures remain diagnostic. Reporting them as operation failures after a
            // successful send could trigger a duplicate; replacing a real failure would hide its cause.
            try
            {
                await activeContext.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception stopException)
            {
                LogCleanupFailure(stopException, "Context stop failed; the operation result is preserved: {ContextType}");
            }

            try
            {
                await activeContext.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception disposeException)
            {
                LogCleanupFailure(disposeException, "Context disposal failed; the operation result is preserved: {ContextType}");
            }
        }
    }

    /// <summary>Adds the supervisor's context-cache state to a probe.</summary>
    /// <param name="context">The probe to populate.</param>
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

    /// <summary>Stops borrowed uses before stopping the owned contexts captured by the supervisor.</summary>
    /// <param name="context">The stop request and captured owned-context agents.</param>
    /// <returns>A task that completes when borrowed and owned context lifecycles have completed.</returns>
    protected override async Task StopSupervisorAsync(StopSupervisorContext context)
    {
        SetCompleted(ActiveAndActualAgentsCompletedAsync(context));

        Task stopBorrowedContexts = _activeSupervisor.StopAsync(context);
        Task stopOwnedContexts = Task.WhenAll(context.Agents.Select(x => x.StopAsync(context)));
        await Task.WhenAll(stopBorrowedContexts, stopOwnedContexts).ConfigureAwait(false);

        await Completed.ConfigureAwait(false);
    }

    async Task ActiveAndActualAgentsCompletedAsync(StopSupervisorContext context)
    {
        await _activeSupervisor.Completed.ConfigureAwait(false);

        await Task.WhenAll(context.Agents.Select(x => x.Completed)).ConfigureAwait(false);
    }

    IActivePipeContextAgent<TContext> CreateActiveContext(CancellationToken cancellationToken)
    {
        IPipeContextHandle<TContext> pipeContextHandle = GetContext();

        return _contextFactory.CreateActiveContext(_activeSupervisor, pipeContextHandle, cancellationToken)
            ?? throw new InvalidOperationException($"The context factory returned no active {TypeCache<TContext>.ShortName} context.");
    }

    IPipeContextHandle<TContext> GetContext()
    {
        lock (_contextLock)
        {
            if (_context is { IsDisposed: false })
                return _context;

            IPipeContextHandle<TContext> context = _contextFactory.CreateContext(this)
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

    static void LogCleanupFailure(Exception exception, string messageTemplate)
    {
        try
        {
            LogContext.Error?.Log(exception, messageTemplate, TypeCache<TContext>.ShortName);
        }
        catch
        {
            // Diagnostic logging cannot change the operation or cleanup outcome.
        }
    }
}
