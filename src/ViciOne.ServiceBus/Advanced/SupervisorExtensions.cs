using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Adds pipe-context agents and asynchronous agent factories to supervisors.</summary>
public static class SupervisorExtensions
{
    /// <summary>Adds an available context whose lifecycle is owned by the supervisor.</summary>
    /// <typeparam name="T">The context type.</typeparam>
    /// <param name="supervisor">The supervisor.</param>
    /// <param name="context">The context.</param>
    /// <returns>A context handle.</returns>
    public static IPipeContextAgent<T> AddContext<T>(this ISupervisor supervisor, T context)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(supervisor);
        ArgumentNullException.ThrowIfNull(context);

        IPipeContextAgent<T> contextAgent = new PipeContextAgent<T>(context);

        supervisor.Add(contextAgent);

        return contextAgent;
    }

    /// <summary>Adds a pending context whose lifecycle is owned by the supervisor.</summary>
    /// <typeparam name="T">The context type.</typeparam>
    /// <param name="supervisor">The supervisor.</param>
    /// <param name="context">The context.</param>
    /// <returns>A context handle.</returns>
    public static IPipeContextAgent<T> AddContext<T>(this ISupervisor supervisor, Task<T> context)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(supervisor);
        ArgumentNullException.ThrowIfNull(context);

        IPipeContextAgent<T> contextAgent = new PipeContextAgent<T>(context);

        supervisor.Add(contextAgent);

        return contextAgent;
    }

    /// <summary>Adds an active pending context whose lifecycle is owned by the supervisor.</summary>
    /// <typeparam name="T">The context type.</typeparam>
    /// <param name="supervisor">The supervisor.</param>
    /// <param name="contextHandle">The actual context handle.</param>
    /// <param name="context">The active context.</param>
    /// <returns>A context handle.</returns>
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

    /// <summary>Adds an active available context whose lifecycle is owned by the supervisor.</summary>
    /// <typeparam name="T">The context type.</typeparam>
    /// <param name="supervisor">The supervisor.</param>
    /// <param name="contextHandle">The actual context handle.</param>
    /// <param name="context">The active context.</param>
    /// <returns>A context handle.</returns>
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

    /// <summary>Adds an asynchronously completed context whose lifecycle is owned by the supervisor.</summary>
    /// <typeparam name="T">The context type.</typeparam>
    /// <param name="supervisor">The supervisor.</param>
    /// <returns>A context handle.</returns>
    public static IAsyncPipeContextAgent<T> AddAsyncContext<T>(this ISupervisor supervisor)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(supervisor);

        IAsyncPipeContextAgent<T> contextAgent = new AsyncPipeContextAgent<T>();

        supervisor.Add(contextAgent);

        return contextAgent;
    }

    /// <summary>Starts agent creation and publishes its completion, cancellation or failure through the supplied async context.</summary>
    /// <typeparam name="T">The supervisor context type.</typeparam>
    /// <typeparam name="TAgent">The agent type.</typeparam>
    /// <param name="supervisor">The supervisor used by the operation.</param>
    /// <param name="asyncContext">The async context used by the operation.</param>
    /// <param name="agentFactory">The agent factory used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
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
                // The async context already carries the creation failure or cancellation.
            }
        });
    }

    /// <summary>Creates an agent from the supervisor context and publishes its lifecycle through the async context.</summary>
    /// <typeparam name="T">The supervisor context type.</typeparam>
    /// <typeparam name="TAgent">The created agent context type.</typeparam>
    /// <param name="supervisor">The supervisor that owns the created agent.</param>
    /// <param name="asyncContext">The context that exposes creation and completion.</param>
    /// <param name="agentFactory">The asynchronous agent factory.</param>
    /// <param name="cancellationToken">The token that cancels agent creation.</param>
    /// <returns>The created agent context.</returns>
    public static async Task<TAgent> CreateAgentAsync<T, TAgent>(this ISupervisor<T> supervisor, IAsyncPipeContextAgent<TAgent> asyncContext,
        Func<T, CancellationToken, Task<TAgent>> agentFactory, CancellationToken cancellationToken)
        where T : class, PipeContext
        where TAgent : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(supervisor);
        ArgumentNullException.ThrowIfNull(asyncContext);
        ArgumentNullException.ThrowIfNull(agentFactory);

        var createAgentPipe = new CreateAgentPipe<T, TAgent>(asyncContext, agentFactory, cancellationToken);

        var supervisorTask = supervisor.SendAsync(createAgentPipe, cancellationToken);

        await Task.WhenAny(supervisorTask, asyncContext.Context).ConfigureAwait(false);

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
            }
            catch (Exception exception)
            {
                await asyncContext.CreateFaultedAsync(exception).ConfigureAwait(false);
            }
        }

        _ = TransferSupervisorOutcomeAsync();

        return await asyncContext.Context.ConfigureAwait(false);
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

            try
            {
                var agent = await _agentFactory(context, _cancellationToken).ConfigureAwait(false);

                await _asyncContext.CreatedAsync(agent).ConfigureAwait(false);

                await _asyncContext.Completed.ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
            {
                CancellationToken canceledToken = exception.CancellationToken.CanBeCanceled
                    ? exception.CancellationToken
                    : _cancellationToken;
                await _asyncContext.CreateCanceledAsync(canceledToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                await _asyncContext.CreateFaultedAsync(exception).ConfigureAwait(false);
            }
        }

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            context.CreateFilterScope("createAgent");
        }
    }
}
