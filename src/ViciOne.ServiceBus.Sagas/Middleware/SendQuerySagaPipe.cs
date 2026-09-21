using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Dispatches a repository query across matching sagas or invokes the missing-saga policy.</summary>
/// <typeparam name="TSaga">The saga instances selected by the query.</typeparam>
/// <typeparam name="T">The consumed message contract used by the query.</typeparam>
public class SendQuerySagaPipe<TSaga, T> :
    IPipe<ISagaRepositoryQueryContext<TSaga, T>>
    where TSaga : class, ISaga
    where T : class
{
    readonly IPipe<SagaConsumeContext<TSaga, T>> _next;
    readonly ISagaPolicy<TSaga, T> _policy;

    /// <summary>Creates a stage that applies one policy to every loaded query result.</summary>
    /// <param name="policy">The policy for existing and missing sagas.</param>
    /// <param name="next">The consumer pipeline invoked for each loaded saga.</param>
    public SendQuerySagaPipe(ISagaPolicy<TSaga, T> policy, IPipe<SagaConsumeContext<TSaga, T>> next)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    /// <summary>Forwards diagnostic probing to the consumer pipeline.</summary>
    /// <param name="context">The diagnostic scope to forward.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _next.Probe(context);
    }

    /// <summary>Loads each matching saga and dispatches it, falling back to the missing-saga policy if none load.</summary>
    /// <param name="context">The repository query and message context.</param>
    /// <returns>A task that completes after every selected repository action.</returns>
    public async Task SendAsync(ISagaRepositoryQueryContext<TSaga, T> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        bool found = false;
        if (context.Count > 0)
        {
            foreach (var correlationId in context)
            {
                SagaConsumeContext<TSaga, T>? sagaConsumeContext = await SagaRepositoryLifecycle.RequireTaskAsync(
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
