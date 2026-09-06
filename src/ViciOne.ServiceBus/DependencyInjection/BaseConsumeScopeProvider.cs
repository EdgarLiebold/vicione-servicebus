using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Provides base consume scope services.</summary>
public abstract class BaseConsumeScopeProvider
{
    readonly IServiceProvider _serviceProvider;
    /// <summary>Exposes the set scoped consume context used by the containing type.</summary>
    protected readonly ISetScopedConsumeContext SetScopedConsumeContext;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    protected BaseConsumeScopeProvider(IRegistrationContext context)
        : this(context, context as ISetScopedConsumeContext ?? throw new ArgumentException(nameof(context)))
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="setScopedConsumeContext">The set scoped consume context.</param>
    protected BaseConsumeScopeProvider(IServiceProvider serviceProvider, ISetScopedConsumeContext setScopedConsumeContext)
    {
        _serviceProvider = serviceProvider;
        SetScopedConsumeContext = setScopedConsumeContext;
    }

    /// <summary>Gets scope context.</summary>
    /// <typeparam name="TScopeContext">The scope context type.</typeparam>
    /// <typeparam name="TPipeContext">The pipe context type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="existingScopeContextFactory">The existing scope context factory.</param>
    /// <param name="createdScopeContextFactory">The created scope context factory.</param>
    /// <param name="pipeContextFactory">The pipe context factory.</param>
    /// <returns>A task that produces the requested value.</returns>
    protected ValueTask<TScopeContext> GetScopeContextAsync<TScopeContext, TPipeContext>(TPipeContext context,
        Func<TPipeContext, IServiceScope, IDisposable, TScopeContext> existingScopeContextFactory,
        Func<TPipeContext, IServiceScope, IDisposable, TScopeContext> createdScopeContextFactory,
        Func<TPipeContext, IServiceScope, IServiceProvider, TPipeContext> pipeContextFactory)
        where TPipeContext : class, PipeContext
    {
        var consumeContext = context as ConsumeContext
            ?? throw new NotSupportedException($"The consume context '{context.GetType().FullName}' does not expose advanced operations.");

        if (context.TryGetPayload<IServiceScope>(out var existingServiceScope))
        {
            return new ValueTask<TScopeContext>(existingScopeContextFactory(context, existingServiceScope,
                SetScopedConsumeContext.PushContext(existingServiceScope, consumeContext)));
        }

        var serviceProvider = context.GetPayload(_serviceProvider);

        var serviceScope = serviceProvider.CreateScope();
        try
        {
            var scopeContext = pipeContextFactory(context, serviceScope, serviceScope.ServiceProvider);

            if (scopeContext.TryGetPayload(out MessageSchedulerContext? schedulerContext))
            {
                var advancedScopeContext = scopeContext as ConsumeContext
                    ?? throw new NotSupportedException($"The consume context '{scopeContext.GetType().FullName}' does not expose advanced operations.");

                scopeContext.AddOrUpdatePayload<MessageSchedulerContext>(
                    () => new ConsumeMessageSchedulerContext(advancedScopeContext, schedulerContext.SchedulerFactory),
                    existing => new ConsumeMessageSchedulerContext(advancedScopeContext, existing.SchedulerFactory));
            }

            var advancedScope = scopeContext as ConsumeContext
                ?? throw new NotSupportedException($"The consume context '{scopeContext.GetType().FullName}' does not expose advanced operations.");

            return new ValueTask<TScopeContext>(createdScopeContextFactory(scopeContext, serviceScope,
                SetScopedConsumeContext.PushContext(serviceScope, advancedScope)));
        }
        catch (Exception ex)
        {
            if (serviceScope is IAsyncDisposable asyncDisposable)
                return ex.DisposeAsync<TScopeContext>(() => asyncDisposable.DisposeAsync());

            serviceScope.Dispose();
            throw;
        }
    }
}
