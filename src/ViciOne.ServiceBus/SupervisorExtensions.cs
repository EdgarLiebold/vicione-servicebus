using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides extension methods for supervisor.</summary>
public static class SupervisorExtensions
{
    /// <summary>Adds a context to the supervisor as an agent, which can be stopped by the supervisor.</summary>
    /// <typeparam name="T">The context type.</typeparam>
    /// <param name="supervisor">The supervisor.</param>
    /// <param name="context">The context.</param>
    /// <returns>A context handle.</returns>
    public static IPipeContextAgent<T> AddContext<T>(this ISupervisor supervisor, T context)
        where T : class, PipeContext
    {
        if (supervisor == null)
            throw new ArgumentNullException(nameof(supervisor));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        IPipeContextAgent<T> contextAgent = new PipeContextAgent<T>(context);

        supervisor.Add(contextAgent);

        return contextAgent;
    }

    /// <summary>Adds a context to the supervisor as an agent, which can be stopped by the supervisor.</summary>
    /// <typeparam name="T">The context type.</typeparam>
    /// <param name="supervisor">The supervisor.</param>
    /// <param name="context">The context.</param>
    /// <returns>A context handle.</returns>
    public static IPipeContextAgent<T> AddContext<T>(this ISupervisor supervisor, Task<T> context)
        where T : class, PipeContext
    {
        if (supervisor == null)
            throw new ArgumentNullException(nameof(supervisor));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        IPipeContextAgent<T> contextAgent = new PipeContextAgent<T>(context);

        supervisor.Add(contextAgent);

        return contextAgent;
    }

    /// <summary>Adds a context to the supervisor as an agent, which can be stopped by the supervisor.</summary>
    /// <typeparam name="T">The context type.</typeparam>
    /// <param name="supervisor">The supervisor.</param>
    /// <param name="contextHandle">The actual context handle.</param>
    /// <param name="context">The active context.</param>
    /// <returns>A context handle.</returns>
    public static IActivePipeContextAgent<T> AddActiveContext<T>(this ISupervisor supervisor, PipeContextHandle<T> contextHandle, Task<T> context)
        where T : class, PipeContext
    {
        if (supervisor == null)
            throw new ArgumentNullException(nameof(supervisor));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var activeContext = new ActivePipeContext<T>(contextHandle, context);

        var contextAgent = new ActivePipeContextAgent<T>(activeContext);

        supervisor.Add(contextAgent);

        return contextAgent;
    }

    /// <summary>Adds a context to the supervisor as an agent, which can be stopped by the supervisor.</summary>
    /// <typeparam name="T">The context type.</typeparam>
    /// <param name="supervisor">The supervisor.</param>
    /// <param name="contextHandle">The actual context handle.</param>
    /// <param name="context">The active context.</param>
    /// <returns>A context handle.</returns>
    public static IActivePipeContextAgent<T> AddActiveContext<T>(this ISupervisor supervisor, PipeContextHandle<T> contextHandle, T context)
        where T : class, PipeContext
    {
        if (supervisor == null)
            throw new ArgumentNullException(nameof(supervisor));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var activeContext = new ActivePipeContext<T>(contextHandle, context);

        var contextAgent = new ActivePipeContextAgent<T>(activeContext);

        supervisor.Add(contextAgent);

        return contextAgent;
    }

    /// <summary>Adds a context to the supervisor as an agent, which can be stopped by the supervisor.</summary>
    /// <typeparam name="T">The context type.</typeparam>
    /// <param name="supervisor">The supervisor.</param>
    /// <returns>A context handle.</returns>
    public static IAsyncPipeContextAgent<T> AddAsyncContext<T>(this ISupervisor supervisor)
        where T : class, PipeContext
    {
        if (supervisor == null)
            throw new ArgumentNullException(nameof(supervisor));

        IAsyncPipeContextAgent<T> contextAgent = new AsyncPipeContextAgent<T>();

        supervisor.Add(contextAgent);

        return contextAgent;
    }

    /// <summary>
    /// Starts asynchronous agent creation for a caller that owns the supplied async context rather
    /// than the mirror task returned by <see cref="CreateAgentAsync{T,TAgent}"/>. The mirror outcome is
    /// observed here; creation cancellation and failure are transferred to <paramref name="asyncContext"/>.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
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
        Task<TAgent> creationTask = supervisor.CreateAgentAsync(asyncContext, agentFactory, cancellationToken);

        creationTask.GetAwaiter().OnCompleted(() =>
        {
            try
            {
                creationTask.GetAwaiter().GetResult();
            }
            catch
            {
                // The creation task transfers cancellation or failure to asyncContext. This observer exists
                // solely so the mirror task cannot become an unobserved exception.
            }
        });
    }

    /// <summary>Creates agent.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TAgent">The agent type.</typeparam>
    /// <param name="supervisor">The supervisor.</param>
    /// <param name="asyncContext">The async context.</param>
    /// <param name="agentFactory">The agent factory.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    public static async Task<TAgent> CreateAgentAsync<T, TAgent>(this ISupervisor<T> supervisor, IAsyncPipeContextAgent<TAgent> asyncContext,
        Func<T, CancellationToken, Task<TAgent>> agentFactory, CancellationToken cancellationToken)
        where T : class, PipeContext
        where TAgent : class, PipeContext
    {
        var createAgentPipe = new CreateAgentPipe<T, TAgent>(asyncContext, agentFactory, cancellationToken);

        var supervisorTask = supervisor.SendAsync(createAgentPipe, cancellationToken);

        await Task.WhenAny(supervisorTask, asyncContext.Context).ConfigureAwait(false);

        async Task HandleSupervisorTaskAsync()
        {
            try
            {
                await supervisorTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await asyncContext.CreateCanceledAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                await asyncContext.CreateFaultedAsync(exception, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        // The bridge catches every supervisor outcome and transfers it to asyncContext. It may
        // outlive context creation because supervisor.Send remains active until the agent stops.
        _ = HandleSupervisorTaskAsync();

        return await asyncContext.Context.ConfigureAwait(false);
    }


    class CreateAgentPipe<T, TAgent> :
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
            _asyncContext = asyncContext;
            _agentFactory = agentFactory;
            _cancellationToken = cancellationToken;
        }

        public async Task SendAsync(T context)
        {
            try
            {
                var agent = await _agentFactory(context, _cancellationToken).ConfigureAwait(false);

                await _asyncContext.CreatedAsync(agent).ConfigureAwait(false);

                await _asyncContext.Completed.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await _asyncContext.CreateCanceledAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                await _asyncContext.CreateFaultedAsync(exception).ConfigureAwait(false);
            }
        }

        public void Probe(ProbeContext context)
        {
            context.CreateFilterScope("createAgent");
        }
    }
}
