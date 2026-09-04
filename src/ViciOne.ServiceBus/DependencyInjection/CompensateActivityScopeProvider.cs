using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides a compensate activity scope provider implementation.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TLog">The t log type.</typeparam>
public class CompensateActivityScopeProvider<TActivity, TLog> :
    BaseConsumeScopeProvider,
    ICompensateActivityScopeProvider<TActivity, TLog>
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public CompensateActivityScopeProvider(IRegistrationContext context)
        : base(context)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="serviceProvider">The service provider value.</param>
    /// <param name="setScopedConsumeContext">The set scoped consume context value.</param>
    public CompensateActivityScopeProvider(IServiceProvider serviceProvider, ISetScopedConsumeContext setScopedConsumeContext)
        : base(serviceProvider, setScopedConsumeContext)
    {
    }

    /// <summary>
    /// Gets scope.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public ValueTask<ICompensateScopeContext<TLog>> GetScopeAsync(CompensateContext<TLog> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled<global::ViciOne.ServiceBus.DependencyInjection.ICompensateScopeContext<TLog>>(cancellationToken); return GetScopeContextAsync(context, ExistingScopeContextFactory, CreatedScopeContextFactory, PipeContextFactory);
    }

    /// <summary>
    /// Gets activity scope.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public ValueTask<ICompensateActivityScopeContext<TActivity, TLog>> GetActivityScopeAsync(CompensateContext<TLog> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled<global::ViciOne.ServiceBus.DependencyInjection.ICompensateActivityScopeContext<TActivity, TLog>>(cancellationToken); return GetScopeContextAsync(context, ExistingActivityScopeContextFactory, CreatedActivityScopeContextFactory, PipeContextFactory);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.Add("provider", "dependencyInjection");
    }

    static CompensateContext<TLog> PipeContextFactory(CompensateContext<TLog> consumeContext, IServiceScope serviceScope, IServiceProvider serviceProvider)
    {
        return new CompensateContextScope<TLog>(consumeContext, serviceScope, serviceScope.ServiceProvider, serviceProvider);
    }

    static ICompensateScopeContext<TLog> ExistingScopeContextFactory(CompensateContext<TLog> consumeContext, IServiceScope serviceScope,
        IDisposable disposable)
    {
        return new ExistingCompensateScopeContext<TLog>(consumeContext, serviceScope, disposable);
    }

    static ICompensateScopeContext<TLog> CreatedScopeContextFactory(CompensateContext<TLog> consumeContext, IServiceScope serviceScope,
        IDisposable disposable)
    {
        return new CreatedCompensateScopeContext<TLog>(serviceScope, consumeContext, disposable);
    }

    static ICompensateActivityScopeContext<TActivity, TLog> ExistingActivityScopeContextFactory(CompensateContext<TLog> consumeContext,
        IServiceScope serviceScope, IDisposable disposable)
    {
        var activity = serviceScope.ServiceProvider.GetService<TActivity>();
        if (activity == null)
            throw new ConsumerException($"Unable to resolve activity type '{TypeCache<TActivity>.ShortName}'.");

        CompensateActivityContext<TActivity, TLog> activityContext = consumeContext.CreateActivityContext(activity);

        return new ExistingCompensateActivityScopeContext<TActivity, TLog>(activityContext, serviceScope, disposable);
    }

    static ICompensateActivityScopeContext<TActivity, TLog> CreatedActivityScopeContextFactory(CompensateContext<TLog> consumeContext,
        IServiceScope serviceScope, IDisposable disposable)
    {
        var activity = serviceScope.ServiceProvider.GetService<TActivity>();
        if (activity == null)
            throw new ConsumerException($"Unable to resolve activity type '{TypeCache<TActivity>.ShortName}'.");

        CompensateActivityContext<TActivity, TLog> activityContext = consumeContext.CreateActivityContext(activity);

        return new CreatedCompensateActivityScopeContext<TActivity, TLog>(activityContext, serviceScope, disposable);
    }
}
