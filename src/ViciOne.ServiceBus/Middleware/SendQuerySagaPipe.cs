using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a send query saga pipe implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="T">The t type.</typeparam>
public class SendQuerySagaPipe<TSaga, T> :
    IPipe<SagaRepositoryQueryContext<TSaga, T>>
    where TSaga : class, ISaga
    where T : class
{
    readonly IPipe<SagaConsumeContext<TSaga, T>> _next;
    readonly ISagaPolicy<TSaga, T> _policy;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="policy">The policy value.</param>
    /// <param name="next">The next value.</param>
    public SendQuerySagaPipe(ISagaPolicy<TSaga, T> policy, IPipe<SagaConsumeContext<TSaga, T>> next)
    {
        _policy = policy;
        _next = next;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
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
