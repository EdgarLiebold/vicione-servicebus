using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Loads a correlated saga and applies the configured policy before updating its repository state.</summary>
/// <typeparam name="TSaga">The saga instance stored by the repository.</typeparam>
/// <typeparam name="T">The consumed message contract used to select the saga.</typeparam>
public class SendSagaPipe<TSaga, T> :
    IPipe<ISagaRepositoryContext<TSaga, T>>
    where TSaga : class, ISaga
    where T : class
{
    readonly Guid _correlationId;
    readonly IPipe<SagaConsumeContext<TSaga, T>> _next;
    readonly ISagaPolicy<TSaga, T> _policy;

    /// <summary>Creates a pipeline stage for one correlation identifier and saga policy.</summary>
    /// <param name="policy">The policy that selects existing or missing-saga behavior.</param>
    /// <param name="next">The consumer pipeline invoked for an existing saga.</param>
    /// <param name="correlationId">The identifier used to load the saga when it is not pre-inserted.</param>
    public SendSagaPipe(ISagaPolicy<TSaga, T> policy, IPipe<SagaConsumeContext<TSaga, T>> next, Guid correlationId)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _correlationId = correlationId;
    }

    /// <summary>Forwards diagnostics to the owned saga pipeline.</summary>
    /// <param name="context">The supplied diagnostic scope.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _next.Probe(context);
    }

    /// <summary>Loads or pre-inserts the correlated saga, executes its policy, and applies the resulting repository action.</summary>
    /// <param name="context">The repository context that owns saga loading and persistence.</param>
    /// <returns>A task that completes after policy execution, repository action and consume-context disposal.</returns>
    public async Task SendAsync(ISagaRepositoryContext<TSaga, T> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        SagaConsumeContext<TSaga, T>? sagaConsumeContext = null;

        if (_policy.PreInsertInstance(context, out var instance))
        {
            if (instance is null)
                throw new InvalidOperationException("The saga policy returned a null pre-insert instance.");

            sagaConsumeContext = await SagaRepositoryLifecycle.RequireTaskAsync(
                    context.InsertAsync(instance),
                    "The saga repository returned a null insert task.")
                .ConfigureAwait(false);
        }

        sagaConsumeContext ??= await SagaRepositoryLifecycle.RequireTaskAsync(
                context.LoadAsync(_correlationId),
                "The saga repository returned a null load task.")
            .ConfigureAwait(false);
        if (sagaConsumeContext != null)
            await SagaRepositoryLifecycle.SendToExistingAsync(context, _policy, _next, sagaConsumeContext).ConfigureAwait(false);
        else
        {
            Task missing = _policy.MissingAsync(context, new MissingSagaPipe<TSaga, T>(context, _next))
                ?? throw new InvalidOperationException("The saga policy returned a null missing-saga task.");
            await missing.ConfigureAwait(false);
        }
    }
}

static class SagaRepositoryLifecycle
{
    public static async Task SendToExistingAsync<TSaga, TMessage>(
        ISagaRepositoryContext<TSaga, TMessage> repositoryContext,
        ISagaPolicy<TSaga, TMessage> policy,
        IPipe<SagaConsumeContext<TSaga, TMessage>> next,
        SagaConsumeContext<TSaga, TMessage> sagaConsumeContext)
        where TSaga : class, ISaga
        where TMessage : class
    {
        Exception? operationFailure = null;
        try
        {
            try
            {
                sagaConsumeContext.LogUsed();
            }
            catch (Exception)
            {
            }

            Task existing = policy.ExistingAsync(sagaConsumeContext, next)
                ?? throw new InvalidOperationException("The saga policy returned a null existing-saga task.");
            await existing.ConfigureAwait(false);

            if (policy.IsReadOnly)
            {
                await RequireTaskAsync(
                        repositoryContext.UndoAsync(sagaConsumeContext),
                        "The saga repository returned a null undo task.")
                    .ConfigureAwait(false);
            }
            else if (sagaConsumeContext.IsCompleted)
            {
                await RequireTaskAsync(
                        repositoryContext.DeleteAsync(sagaConsumeContext),
                        "The saga repository returned a null delete task.")
                    .ConfigureAwait(false);

                try
                {
                    sagaConsumeContext.LogRemoved();
                }
                catch (Exception)
                {
                }
            }
            else
            {
                await RequireTaskAsync(
                        repositoryContext.UpdateAsync(sagaConsumeContext),
                        "The saga repository returned a null update task.")
                    .ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            operationFailure = exception;
        }

        Exception? disposalFailure = await TryDisposeAsync(sagaConsumeContext).ConfigureAwait(false);
        ThrowIfAny(
            "The existing saga operation or consume-context disposal failed.",
            operationFailure,
            disposalFailure is null ? [] : [disposalFailure]);
    }

    public static Task RequireTaskAsync(Task? task, string message) =>
        task ?? throw new InvalidOperationException(message);

    public static Task<T> RequireTaskAsync<T>(Task<T>? task, string message) =>
        task ?? throw new InvalidOperationException(message);

    public static async Task<Exception?> TryDisposeAsync(object value)
    {
        try
        {
            switch (value)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }

            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    public static void ThrowIfAny(
        string aggregateMessage,
        Exception? operationFailure,
        IReadOnlyList<Exception> cleanupFailures)
    {
        if (operationFailure is null && cleanupFailures.Count == 0)
            return;

        if (operationFailure is not null && cleanupFailures.Count == 0)
            ExceptionDispatchInfo.Throw(operationFailure);

        if (operationFailure is null && cleanupFailures.Count == 1)
            ExceptionDispatchInfo.Throw(cleanupFailures[0]);

        var failures = new List<Exception>(cleanupFailures.Count + 1);
        if (operationFailure is not null)
            failures.Add(operationFailure);
        failures.AddRange(cleanupFailures);

        throw new AggregateException(aggregateMessage, failures);
    }
}
