using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.DependencyInjection;

public class DependencyInjectionSagaRepositoryContextFactory<TSaga> :
    ISagaRepositoryContextFactory<TSaga>
    where TSaga : class, ISaga
{
    readonly IServiceProvider _serviceProvider;
    readonly ISetScopedConsumeContext _setter;

    public DependencyInjectionSagaRepositoryContextFactory(IRegistrationContext context)
        : this(context, context as ISetScopedConsumeContext ?? throw new ArgumentException(nameof(context)))
    {
    }

    public DependencyInjectionSagaRepositoryContextFactory(IServiceProvider serviceProvider, ISetScopedConsumeContext setter)
    {
        _serviceProvider = serviceProvider;
        _setter = setter;
    }

    public void Probe(ProbeContext context)
    {
        context.Add("provider", "dependencyInjection");
    }

    public Task SendAsync<T>(ConsumeContext<T> context, IPipe<SagaRepositoryContext<TSaga, T>> next)
        where T : class
    {
        return SendAsync(context, (consumeContext, factory) => factory.SendAsync(consumeContext, next));
    }

    public Task SendQueryAsync<T>(ConsumeContext<T> context, ISagaQuery<TSaga> query, IPipe<SagaRepositoryQueryContext<TSaga, T>> next)
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
