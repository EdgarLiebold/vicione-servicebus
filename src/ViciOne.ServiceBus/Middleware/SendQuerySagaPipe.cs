using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Executes the pipeline for send query saga.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="T">The value type.</typeparam>
public class SendQuerySagaPipe<TSaga, T> :
    IPipe<SagaRepositoryQueryContext<TSaga, T>>
    where TSaga : class, ISaga
    where T : class
{
    readonly IPipe<SagaConsumeContext<TSaga, T>> _next;
    readonly ISagaPolicy<TSaga, T> _policy;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="policy">The policy.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    public SendQuerySagaPipe(ISagaPolicy<TSaga, T> policy, IPipe<SagaConsumeContext<TSaga, T>> next)
    {
        _policy = policy;
        _next = next;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(SagaRepositoryQueryContext<TSaga, T> context)
    {
        if (context.Count > 0)
        {
            async Task SendToInstanceAsync(Guid correlationId)
            {
                SagaConsumeContext<TSaga, T>? sagaConsumeContext = await context.LoadAsync(correlationId).ConfigureAwait(false);
                if (sagaConsumeContext != null)
                {
                    sagaConsumeContext.LogUsed();

                    try
                    {
                        await _policy.ExistingAsync(sagaConsumeContext, _next).ConfigureAwait(false);

                        if (_policy.IsReadOnly)
                            await context.UndoAsync(sagaConsumeContext).ConfigureAwait(false);
                        else
                        {
                            if (sagaConsumeContext.IsCompleted)
                            {
                                await context.DeleteAsync(sagaConsumeContext).ConfigureAwait(false);

                                sagaConsumeContext.LogRemoved();
                            }
                            else
                                await context.UpdateAsync(sagaConsumeContext).ConfigureAwait(false);
                        }
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

            foreach (var correlationId in context)
                await SendToInstanceAsync(correlationId).ConfigureAwait(false);
        }
        else
        {
            var missingPipe = new MissingSagaPipe<TSaga, T>(context, _next);

            await _policy.MissingAsync(context, missingPipe).ConfigureAwait(false);
        }
    }
}
