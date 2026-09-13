using System;
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
        : this(context, context as ISetScopedConsumeContext ?? throw new ArgumentException(nameof(context)))
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="setter">The setter.</param>
    public DependencyInjectionSagaRepositoryContextFactory(IServiceProvider serviceProvider, ISetScopedConsumeContext setter)
    {
        _serviceProvider = serviceProvider;
        _setter = setter;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
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
        return SendAsync(context, (consumeContext, factory) => factory.SendQueryAsync(consumeContext, query, next));
    }

    async Task SendAsync<T>(ConsumeContext<T> context, Func<ConsumeContext<T>, ISagaRepositoryContextFactory<TSaga>, Task> send)
        where T : class
    {
        var serviceProvider = context.GetPayload(_serviceProvider);

        IDisposable? disposable = null;

        if (context.TryGetPayload<IServiceScope>(out var existingScope))
        {
            disposable = _setter.PushContext(existingScope, context.Advanced());

            try
            {
                var factory = existingScope.ServiceProvider.GetRequiredService<ISagaRepositoryContextFactory<TSaga>>();
                await send(context, factory).ConfigureAwait(false);
            }
            finally
            {
                disposable.Dispose();
            }

            return;
        }

        var serviceScope = serviceProvider.CreateScope();
        try
        {
            var scopeContext = new ConsumeContextScope<T>(context, serviceScope, serviceScope.ServiceProvider);

            if (scopeContext.TryGetPayload(out MessageSchedulerContext? schedulerContext))
            {
                scopeContext.AddOrUpdatePayload<MessageSchedulerContext>(
                    () => new ConsumeMessageSchedulerContext(scopeContext, schedulerContext.SchedulerFactory),
                    existing => new ConsumeMessageSchedulerContext(scopeContext, existing.SchedulerFactory));
            }

            disposable = _setter.PushContext(serviceScope, scopeContext);

            var consumeContextScope = new ConsumeContextScope<T>(scopeContext);

            var factory = serviceScope.ServiceProvider.GetRequiredService<ISagaRepositoryContextFactory<TSaga>>();

            await send(consumeContextScope, factory).ConfigureAwait(false);
        }
        finally
        {
            disposable?.Dispose();

            if (serviceScope is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            else
                serviceScope.Dispose();
        }
    }
}
