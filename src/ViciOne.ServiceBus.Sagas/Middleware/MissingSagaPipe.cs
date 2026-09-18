using System;
using System.Collections.Generic;
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
    readonly ISagaRepositoryContext<TSaga, TMessage> _repositoryContext;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="repositoryContext">The repository context.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    public MissingSagaPipe(ISagaRepositoryContext<TSaga, TMessage> repositoryContext, IPipe<SagaConsumeContext<TSaga, TMessage>> next)
    {
        _repositoryContext = repositoryContext ?? throw new ArgumentNullException(nameof(repositoryContext));
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _next.Probe(context);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(SagaConsumeContext<TSaga, TMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        SagaConsumeContext<TSaga, TMessage> sagaConsumeContext = await SagaRepositoryLifecycle.RequireTask(
                _repositoryContext.AddAsync(context.Saga),
                "The saga repository returned a null add task.")
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("The saga repository returned a null added saga context.");

        bool discardAttempted = false;
        Exception? operationFailure = null;
        try
        {
            sagaConsumeContext.LogAdded();

            Task next = _next.SendAsync(sagaConsumeContext)
                ?? throw new InvalidOperationException("The added saga pipeline returned a null task.");
            await next.ConfigureAwait(false);

            if (sagaConsumeContext.IsCompleted)
            {
                discardAttempted = true;
                await SagaRepositoryLifecycle.RequireTask(
                        _repositoryContext.DiscardAsync(sagaConsumeContext),
                        "The saga repository returned a null discard task.")
                    .ConfigureAwait(false);
            }
            else
            {
                await SagaRepositoryLifecycle.RequireTask(
                        _repositoryContext.SaveAsync(sagaConsumeContext),
                        "The saga repository returned a null save task.")
                    .ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            operationFailure = exception;
        }

        var cleanupFailures = new List<Exception>(2);
        if (operationFailure is not null && !discardAttempted)
        {
            try
            {
                await SagaRepositoryLifecycle.RequireTask(
                        _repositoryContext.DiscardAsync(sagaConsumeContext),
                        "The saga repository returned a null discard task.")
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                cleanupFailures.Add(exception);
            }
        }

        Exception? disposalFailure = await SagaRepositoryLifecycle.TryDisposeAsync(sagaConsumeContext).ConfigureAwait(false);
        if (disposalFailure is not null)
            cleanupFailures.Add(disposalFailure);

        SagaRepositoryLifecycle.ThrowIfAny(
            "The added saga operation or consume-context cleanup failed.",
            operationFailure,
            cleanupFailures);
    }
}
