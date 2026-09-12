using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Creates or reuses dependency-injection scopes and transfers their ambient-context lifetime to typed scope contexts.</summary>
public abstract class BaseConsumeScopeProvider
{
    readonly IServiceProvider _serviceProvider;
    /// <summary>Gets the component that installs a consume context into a dependency-injection scope.</summary>
    protected readonly ISetScopedConsumeContext SetScopedConsumeContext;

    /// <summary>Uses the registration context as both the service source and ambient-context installer.</summary>
    /// <param name="context">The registration context that owns the bus container.</param>
    protected BaseConsumeScopeProvider(IRegistrationContext context)
        : this(context ?? throw new ArgumentNullException(nameof(context)),
            context as ISetScopedConsumeContext
            ?? throw new ArgumentException("The registration context cannot install a scoped consume context.", nameof(context)))
    {
    }

    /// <summary>Uses explicit services and ambient-context installation for subsequent scope creation.</summary>
    /// <param name="serviceProvider">The fallback service provider used when the pipe context does not supply one.</param>
    /// <param name="setScopedConsumeContext">The component that installs and restores consume contexts in scopes.</param>
    protected BaseConsumeScopeProvider(IServiceProvider serviceProvider, ISetScopedConsumeContext setScopedConsumeContext)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        SetScopedConsumeContext = setScopedConsumeContext ?? throw new ArgumentNullException(nameof(setScopedConsumeContext));
    }

    /// <summary>Creates the requested typed scope context or wraps an existing scope carried by the pipe context.</summary>
    /// <typeparam name="TScopeContext">The scope context type.</typeparam>
    /// <typeparam name="TPipeContext">The pipe context type.</typeparam>
    /// <param name="context">The pipe context whose payloads select the service provider and optional existing scope.</param>
    /// <param name="existingScopeContextFactory">Creates a non-owning typed view over an existing scope.</param>
    /// <param name="createdScopeContextFactory">Creates an owning typed view over a newly allocated scope.</param>
    /// <param name="pipeContextFactory">Rebinds the pipe context to a newly allocated scope.</param>
    /// <returns>A value task containing the scope context that owns the ambient-context restoration handle.</returns>
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
            IDisposable? existingRestoreContext = null;
            try
            {
                existingRestoreContext = SetScopedConsumeContext.PushContext(existingServiceScope, consumeContext);
                TScopeContext result = existingScopeContextFactory(context, existingServiceScope, existingRestoreContext);
                existingRestoreContext = null;
                return new ValueTask<TScopeContext>(result);
            }
            catch (Exception exception)
            {
                try
                {
                    existingRestoreContext?.Dispose();
                }
                catch (Exception cleanupException)
                {
                    throw new AggregateException("The consume scope could not be created or restored.", exception, cleanupException);
                }

                throw;
            }
        }

        var serviceProvider = context.GetPayload(_serviceProvider);

        var serviceScope = serviceProvider.CreateScope();
        IDisposable? restoreContext = null;
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

            restoreContext = SetScopedConsumeContext.PushContext(serviceScope, advancedScope);
            TScopeContext result = createdScopeContextFactory(scopeContext, serviceScope, restoreContext);
            restoreContext = null;
            return new ValueTask<TScopeContext>(result);
        }
        catch (Exception exception)
        {
            return DisposeFailedScopeAsync<TScopeContext>(exception, restoreContext, serviceScope);
        }
    }

    static async ValueTask<T> DisposeFailedScopeAsync<T>(Exception exception, IDisposable? restoreContext, IServiceScope serviceScope)
    {
        Exception? cleanupFailure = null;
        try
        {
            restoreContext?.Dispose();
        }
        catch (Exception caught)
        {
            cleanupFailure = caught;
        }

        try
        {
            if (serviceScope is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            else
                serviceScope.Dispose();
        }
        catch (Exception caught)
        {
            cleanupFailure = cleanupFailure is null
                ? caught
                : new AggregateException(cleanupFailure, caught);
        }

        if (cleanupFailure is not null)
            throw new AggregateException("The consume scope could not be created or released.", exception, cleanupFailure);

        ExceptionDispatchInfo.Capture(exception).Throw();
        return default!;
    }
}
