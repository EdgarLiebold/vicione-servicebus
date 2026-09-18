using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Creates dependency injection saga repository context instances.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class DependencyInjectionSagaRepositoryContextFactory<TSaga> :
    ISagaRepositoryContextFactory<TSaga>
    where TSaga : class, ISaga
{
    readonly IServiceProvider _serviceProvider;
    readonly ISetScopedConsumeContext _setter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public DependencyInjectionSagaRepositoryContextFactory(IRegistrationContext context)
        : this(context ?? throw new ArgumentNullException(nameof(context)),
            context as ISetScopedConsumeContext
                ?? throw new ArgumentException(
                    "The registration context must support scoped consume-context ownership.", nameof(context)))
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="setter">The setter.</param>
    public DependencyInjectionSagaRepositoryContextFactory(IServiceProvider serviceProvider, ISetScopedConsumeContext setter)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _setter = setter ?? throw new ArgumentNullException(nameof(setter));
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Add("provider", "dependencyInjection");
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync<T>(ConsumeContext<T> context, IPipe<ISagaRepositoryContext<TSaga, T>> next)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        return SendAsync(context, (consumeContext, factory) => factory.SendAsync(consumeContext, next));
    }

    /// <summary>Sends query.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="query">The query.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendQueryAsync<T>(ConsumeContext<T> context, ISagaQuery<TSaga> query, IPipe<ISagaRepositoryQueryContext<TSaga, T>> next)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(next);

        return SendAsync(context, (consumeContext, factory) => factory.SendQueryAsync(consumeContext, query, next));
    }

    async Task SendAsync<T>(ConsumeContext<T> context, Func<ConsumeContext<T>, ISagaRepositoryContextFactory<TSaga>, Task> send)
        where T : class
    {
        var serviceProvider = context.GetPayload(_serviceProvider);

        IDisposable? disposable = null;

        if (context.TryGetPayload<IServiceScope>(out var existingScope))
        {
            disposable = _setter.PushContext(existingScope, context.Advanced())
                ?? throw new InvalidOperationException("The scoped consume context setter returned a null restore handle.");

            Exception? operationFailure = null;
            try
            {
                var factory = existingScope.ServiceProvider.GetRequiredService<ISagaRepositoryContextFactory<TSaga>>();
                Task execution = send(context, factory)
                    ?? throw new InvalidOperationException("The scoped saga repository context factory returned a null task.");
                await execution.ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                operationFailure = exception;
            }

            Exception? restoreFailure = null;
            try
            {
                disposable.Dispose();
            }
            catch (Exception exception)
            {
                restoreFailure = exception;
            }

            DependencyInjectionSagaScope.ThrowIfAny(
                "The saga repository operation and its scoped consume-context restore both failed.",
                operationFailure,
                restoreFailure is null ? [] : [restoreFailure]);

            return;
        }

        var serviceScope = serviceProvider.CreateScope();
        Exception? createdOperationFailure = null;
        try
        {
            var scopeContext = new ConsumeContextScope<T>(context, serviceScope, serviceScope.ServiceProvider);

            if (scopeContext.TryGetPayload(out MessageSchedulerContext? schedulerContext))
            {
                scopeContext.AddOrUpdatePayload<MessageSchedulerContext>(
                    () => new ConsumeMessageSchedulerContext(scopeContext, schedulerContext.SchedulerFactory),
                    existing => new ConsumeMessageSchedulerContext(scopeContext, existing.SchedulerFactory));
            }

            disposable = _setter.PushContext(serviceScope, scopeContext)
                ?? throw new InvalidOperationException("The scoped consume context setter returned a null restore handle.");

            var consumeContextScope = new ConsumeContextScope<T>(scopeContext);

            var factory = serviceScope.ServiceProvider.GetRequiredService<ISagaRepositoryContextFactory<TSaga>>();

            Task execution = send(consumeContextScope, factory)
                ?? throw new InvalidOperationException("The scoped saga repository context factory returned a null task.");
            await execution.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            createdOperationFailure = exception;
        }

        await DisposeScopeAsync(disposable, serviceScope, createdOperationFailure).ConfigureAwait(false);
    }

    static async Task DisposeScopeAsync(
        IDisposable? restoreContext,
        IServiceScope serviceScope,
        Exception? operationFailure)
    {
        var cleanupFailures = new List<Exception>(2);
        try
        {
            restoreContext?.Dispose();
        }
        catch (Exception exception)
        {
            cleanupFailures.Add(exception);
        }

        try
        {
            if (serviceScope is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            else
                serviceScope.Dispose();
        }
        catch (Exception exception)
        {
            cleanupFailures.Add(exception);
        }

        DependencyInjectionSagaScope.ThrowIfAny(
            "The saga repository operation or its dependency injection cleanup failed.",
            operationFailure,
            cleanupFailures);
    }
}

static class DependencyInjectionSagaScope
{
    public static async Task DisposeAsync(
        IServiceScope serviceScope,
        Exception? operationFailure,
        string aggregateMessage)
    {
        Exception? scopeFailure = null;
        try
        {
            if (serviceScope is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            else
                serviceScope.Dispose();
        }
        catch (Exception exception)
        {
            scopeFailure = exception;
        }

        ThrowIfAny(
            aggregateMessage,
            operationFailure,
            scopeFailure is null ? [] : [scopeFailure]);
    }

    public static void ThrowIfAny(
        string aggregateMessage,
        Exception? operationFailure,
        IReadOnlyList<Exception> cleanupFailures)
    {
        if (operationFailure is null && cleanupFailures.Count == 0)
            return;

        if (operationFailure is not null && cleanupFailures.Count == 0)
            ExceptionDispatchInfo.Capture(operationFailure).Throw();

        if (operationFailure is null && cleanupFailures.Count == 1)
            ExceptionDispatchInfo.Capture(cleanupFailures[0]).Throw();

        var failures = new List<Exception>(cleanupFailures.Count + 1);
        if (operationFailure is not null)
            failures.Add(operationFailure);
        failures.AddRange(cleanupFailures);

        throw new AggregateException(aggregateMessage, failures);
    }
}
