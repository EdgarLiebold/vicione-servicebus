using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Connects pipe-context ownership and asynchronous creation to supervisor lifecycles.</summary>
public static class SupervisorExtensions
{
    /// <summary>Adds an already available context whose lifecycle is owned by the supervisor.</summary>
    /// <typeparam name="T">The pipe context type.</typeparam>
    /// <param name="supervisor">The supervisor that owns the context.</param>
    /// <param name="context">The context to own.</param>
    /// <returns>The supervised owning handle.</returns>
    public static IPipeContextAgent<T> AddContext<T>(this ISupervisor supervisor, T context)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(supervisor);
        ArgumentNullException.ThrowIfNull(context);

        IPipeContextAgent<T> contextAgent = new PipeContextAgent<T>(context);

        supervisor.Add(contextAgent);

        return contextAgent;
    }

    /// <summary>Adds an asynchronously supplied context whose lifecycle is owned by the supervisor.</summary>
    /// <typeparam name="T">The pipe context type.</typeparam>
    /// <param name="supervisor">The supervisor that owns the context.</param>
    /// <param name="context">The task that supplies the context to own.</param>
    /// <returns>The supervised owning handle.</returns>
    public static IPipeContextAgent<T> AddContext<T>(this ISupervisor supervisor, Task<T> context)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(supervisor);
        ArgumentNullException.ThrowIfNull(context);

        IPipeContextAgent<T> contextAgent = new PipeContextAgent<T>(context);

        supervisor.Add(contextAgent);

        return contextAgent;
    }

    /// <summary>Adds one asynchronously supplied borrowed use of an owned context.</summary>
    /// <typeparam name="T">The pipe context type.</typeparam>
    /// <param name="supervisor">The supervisor that tracks the borrowed use.</param>
    /// <param name="contextHandle">The handle that owns the shared context.</param>
    /// <param name="context">The task that supplies the context for this use.</param>
    /// <returns>The supervised borrowed handle.</returns>
    public static IActivePipeContextAgent<T> AddActiveContext<T>(this ISupervisor supervisor, IPipeContextHandle<T> contextHandle, Task<T> context)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(supervisor);
        ArgumentNullException.ThrowIfNull(contextHandle);
        ArgumentNullException.ThrowIfNull(context);

        var activeContext = new ActivePipeContext<T>(contextHandle, context);

        var contextAgent = new ActivePipeContextAgent<T>(activeContext);

        supervisor.Add(contextAgent);

        return contextAgent;
    }

    /// <summary>Adds one already available borrowed use of an owned context.</summary>
    /// <typeparam name="T">The pipe context type.</typeparam>
    /// <param name="supervisor">The supervisor that tracks the borrowed use.</param>
    /// <param name="contextHandle">The handle that owns the shared context.</param>
    /// <param name="context">The context used by this borrower.</param>
    /// <returns>The supervised borrowed handle.</returns>
    public static IActivePipeContextAgent<T> AddActiveContext<T>(this ISupervisor supervisor, IPipeContextHandle<T> contextHandle, T context)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(supervisor);
        ArgumentNullException.ThrowIfNull(contextHandle);
        ArgumentNullException.ThrowIfNull(context);

        var activeContext = new ActivePipeContext<T>(contextHandle, context);

        var contextAgent = new ActivePipeContextAgent<T>(activeContext);

        supervisor.Add(contextAgent);

        return contextAgent;
    }

    /// <summary>Adds a supervised context agent whose creation outcome will be reported later.</summary>
    /// <typeparam name="T">The pipe context type.</typeparam>
    /// <param name="supervisor">The supervisor that owns the pending agent.</param>
    /// <returns>The handle used to report creation and observe lifecycle completion.</returns>
    public static IAsyncPipeContextAgent<T> AddAsyncContext<T>(this ISupervisor supervisor)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(supervisor);

        IAsyncPipeContextAgent<T> contextAgent = new AsyncPipeContextAgent<T>();

        supervisor.Add(contextAgent);

        return contextAgent;
    }

    /// <summary>Starts owned agent creation and observes its outcome through the supplied asynchronous context.</summary>
    /// <typeparam name="T">The supervisor context type.</typeparam>
    /// <typeparam name="TAgent">The agent type.</typeparam>
    /// <param name="supervisor">The supervisor that supplies the owner context.</param>
    /// <param name="asyncContext">The owned agent that receives the creation outcome.</param>
    /// <param name="agentFactory">The asynchronous factory for the agent context.</param>
    /// <param name="cancellationToken">The token that cancels creation.</param>
    public static void StartAgent<T, TAgent>(this ISupervisor<T> supervisor, IAsyncPipeContextAgent<TAgent> asyncContext,
        Func<T, CancellationToken, Task<TAgent>> agentFactory, CancellationToken cancellationToken)
        where T : class, PipeContext
        where TAgent : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(supervisor);
        ArgumentNullException.ThrowIfNull(asyncContext);
        ArgumentNullException.ThrowIfNull(agentFactory);

        Task<TAgent> creationTask = supervisor.CreateAgentAsync(asyncContext, agentFactory, cancellationToken);

        creationTask.GetAwaiter().OnCompleted(() =>
        {
            try
            {
                creationTask.GetAwaiter().GetResult();
            }
            catch
            {
                // The completion callback observes failures from the background creation bridge.
            }
        });
    }

    /// <summary>Creates an agent context from the supervisor context and publishes its lifecycle.</summary>
    /// <typeparam name="T">The supervisor context type.</typeparam>
    /// <typeparam name="TAgent">The created agent context type.</typeparam>
    /// <param name="supervisor">The supervisor that owns the created agent.</param>
    /// <param name="asyncContext">The owned agent that receives the creation outcome.</param>
    /// <param name="agentFactory">The asynchronous agent factory.</param>
    /// <param name="cancellationToken">The token that cancels agent creation.</param>
    /// <returns>The created context after its outcome has been published.</returns>
    public static async Task<TAgent> CreateAgentAsync<T, TAgent>(this ISupervisor<T> supervisor, IAsyncPipeContextAgent<TAgent> asyncContext,
        Func<T, CancellationToken, Task<TAgent>> agentFactory, CancellationToken cancellationToken)
        where T : class, PipeContext
        where TAgent : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(supervisor);
        ArgumentNullException.ThrowIfNull(asyncContext);
        ArgumentNullException.ThrowIfNull(agentFactory);

        var createAgentPipe = new CreateAgentPipe<T, TAgent>(asyncContext, agentFactory, cancellationToken);

        Task supervisorTask = supervisor.SendAsync(createAgentPipe, cancellationToken);
        Task<TAgent> contextTask = asyncContext.Context;

        async Task TransferSupervisorOutcomeAsync()
        {
            try
            {
                await supervisorTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
            {
                CancellationToken canceledToken = exception.CancellationToken.CanBeCanceled
                    ? exception.CancellationToken
                    : cancellationToken;
                await asyncContext.CreateCanceledAsync(canceledToken).ConfigureAwait(false);
                throw new OperationCanceledException(exception.Message, exception, canceledToken);
            }
            catch (Exception exception)
            {
                await asyncContext.CreateFaultedAsync(exception).ConfigureAwait(false);
                throw;
            }
        }

        Task transferTask = TransferSupervisorOutcomeAsync();
        Task firstCompleted = await Task.WhenAny(transferTask, contextTask).ConfigureAwait(false);
        if (firstCompleted == transferTask)
        {
            await transferTask.ConfigureAwait(false);

            if (!contextTask.IsCompleted)
                throw new InvalidOperationException("The supervisor completed agent creation without publishing an outcome.");
        }
        else
            transferTask.IgnoreUnobservedExceptions();

        return await contextTask.ConfigureAwait(false);
    }

    sealed class CreateAgentPipe<T, TAgent> :
        IPipe<T>
        where T : class, PipeContext
        where TAgent : class, PipeContext
    {
        readonly Func<T, CancellationToken, Task<TAgent>> _agentFactory;
        readonly IAsyncPipeContextAgent<TAgent> _asyncContext;
        readonly CancellationToken _cancellationToken;

        public CreateAgentPipe(IAsyncPipeContextAgent<TAgent> asyncContext, Func<T, CancellationToken, Task<TAgent>> agentFactory,
            CancellationToken cancellationToken)
        {
            _asyncContext = asyncContext ?? throw new ArgumentNullException(nameof(asyncContext));
            _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
            _cancellationToken = cancellationToken;
        }

        public async Task SendAsync(T context)
        {
            ArgumentNullException.ThrowIfNull(context);

            TAgent agent;
            try
            {
                agent = await _agentFactory(context, _cancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException($"The agent factory returned no {typeof(TAgent).Name} context.");
            }
            catch (OperationCanceledException exception)
            {
                CancellationToken canceledToken = exception.CancellationToken.CanBeCanceled
                    ? exception.CancellationToken
                    : _cancellationToken;
                await _asyncContext.CreateCanceledAsync(canceledToken).ConfigureAwait(false);
                return;
            }
            catch (Exception exception)
            {
                await _asyncContext.CreateFaultedAsync(exception).ConfigureAwait(false);
                return;
            }

            await _asyncContext.CreatedAsync(agent).ConfigureAwait(false);

            await _asyncContext.Completed.ConfigureAwait(false);
        }

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            context.CreateFilterScope("createAgent");
        }
    }
}
