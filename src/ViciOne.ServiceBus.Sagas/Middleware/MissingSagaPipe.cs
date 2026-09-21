using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Adds a saga chosen by the missing-saga policy and saves or discards it after dispatch.</summary>
/// <typeparam name="TSaga">The saga instance created for the missing correlation.</typeparam>
/// <typeparam name="TMessage">The incoming message contract that initiates the saga.</typeparam>
public class MissingSagaPipe<TSaga, TMessage> :
    IPipe<SagaConsumeContext<TSaga, TMessage>>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly IPipe<SagaConsumeContext<TSaga, TMessage>> _next;
    readonly ISagaRepositoryContext<TSaga, TMessage> _repositoryContext;

    /// <summary>Creates the stage that owns addition and cleanup of a newly selected saga.</summary>
    /// <param name="repositoryContext">The repository used to add, save, and discard the saga.</param>
    /// <param name="next">The pipeline invoked after the saga has been added.</param>
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

    /// <summary>Adds the saga, dispatches its pipeline, and persists or discards it with cleanup on failure.</summary>
    /// <param name="context">The proposed saga and initiating message from the missing-saga policy.</param>
    /// <returns>A task that completes after repository action and consume-context disposal.</returns>
    public async Task SendAsync(SagaConsumeContext<TSaga, TMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        SagaConsumeContext<TSaga, TMessage> sagaConsumeContext = await SagaRepositoryLifecycle.RequireTaskAsync(
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
                await SagaRepositoryLifecycle.RequireTaskAsync(
                        _repositoryContext.DiscardAsync(sagaConsumeContext),
                        "The saga repository returned a null discard task.")
                    .ConfigureAwait(false);
            }
            else
            {
                await SagaRepositoryLifecycle.RequireTaskAsync(
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
                await SagaRepositoryLifecycle.RequireTaskAsync(
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
