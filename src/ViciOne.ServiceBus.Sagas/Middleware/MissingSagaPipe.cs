using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Dispatches a missing saga message to the saga policy, calling Add if necessary.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class MissingSagaPipe<TSaga, TMessage> :
    IPipe<SagaConsumeContext<TSaga, TMessage>>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly IPipe<SagaConsumeContext<TSaga, TMessage>> _next;
    readonly SagaRepositoryContext<TSaga, TMessage> _repositoryContext;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="repositoryContext">The repository context.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    public MissingSagaPipe(SagaRepositoryContext<TSaga, TMessage> repositoryContext, IPipe<SagaConsumeContext<TSaga, TMessage>> next)
    {
        _repositoryContext = repositoryContext;
        _next = next;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        _next.Probe(context);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(SagaConsumeContext<TSaga, TMessage> context)
    {
        SagaConsumeContext<TSaga, TMessage> sagaConsumeContext = await _repositoryContext.AddAsync(context.Saga).ConfigureAwait(false);

        sagaConsumeContext.LogAdded();

        try
        {
            await _next.SendAsync(sagaConsumeContext).ConfigureAwait(false);

            if (sagaConsumeContext.IsCompleted)
                await _repositoryContext.DiscardAsync(sagaConsumeContext).ConfigureAwait(false);
            else
                await _repositoryContext.SaveAsync(sagaConsumeContext).ConfigureAwait(false);
        }
        catch (Exception)
        {
            await _repositoryContext.DiscardAsync(sagaConsumeContext).ConfigureAwait(false);

            throw;
        }
        finally
        {
            switch (sagaConsumeContext)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }
    }
}
