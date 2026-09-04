using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides a base consume scope provider implementation.
/// </summary>
public abstract class BaseConsumeScopeProvider
{
    readonly IServiceProvider _serviceProvider;
    /// <summary>
    /// Defines the set scoped consume context value.
    /// </summary>
    protected readonly ISetScopedConsumeContext SetScopedConsumeContext;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    protected BaseConsumeScopeProvider(IRegistrationContext context)
        : this(context, context as ISetScopedConsumeContext ?? throw new ArgumentException(nameof(context)))
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="serviceProvider">The service provider value.</param>
    /// <param name="setScopedConsumeContext">The set scoped consume context value.</param>
    protected BaseConsumeScopeProvider(IServiceProvider serviceProvider, ISetScopedConsumeContext setScopedConsumeContext)
    {
        _serviceProvider = serviceProvider;
        SetScopedConsumeContext = setScopedConsumeContext;
    }

    /// <summary>
    /// Gets scope context.
    /// </summary>
    /// <typeparam name="TScopeContext">The t scope context type.</typeparam>
    /// <typeparam name="TPipeContext">The t pipe context type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="existingScopeContextFactory">The existing scope context factory value.</param>
    /// <param name="createdScopeContextFactory">The created scope context factory value.</param>
    /// <param name="pipeContextFactory">The pipe context factory value.</param>
    /// <returns>The result of the operation.</returns>
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
