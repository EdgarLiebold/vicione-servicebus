using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Executes the pipeline for send query saga.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="T">The value type.</typeparam>
public class SendQuerySagaPipe<TSaga, T> :
    IPipe<ISagaRepositoryQueryContext<TSaga, T>>
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
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _next.Probe(context);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(ISagaRepositoryQueryContext<TSaga, T> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        bool found = false;
        if (context.Count > 0)
        {
            foreach (var correlationId in context)
            {
                SagaConsumeContext<TSaga, T>? sagaConsumeContext = await SagaRepositoryLifecycle.RequireTask(
                        context.LoadAsync(correlationId),
                        "The saga repository returned a null load task.")
                    .ConfigureAwait(false);
                if (sagaConsumeContext != null)
                {
                    found = true;
                    await SagaRepositoryLifecycle.SendToExistingAsync(context, _policy, _next, sagaConsumeContext).ConfigureAwait(false);
                }
            }
        }

        if (!found)
        {
            var missingPipe = new MissingSagaPipe<TSaga, T>(context, _next);

            Task missing = _policy.MissingAsync(context, missingPipe)
                ?? throw new InvalidOperationException("The saga policy returned a null missing-saga task.");
            await missing.ConfigureAwait(false);
        }
    }
}
