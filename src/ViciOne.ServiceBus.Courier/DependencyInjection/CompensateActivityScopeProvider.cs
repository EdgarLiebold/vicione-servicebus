using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Creates compensation scopes and resolves activity instances from their scoped service providers.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
internal sealed class CompensateActivityScopeProvider<TActivity, TLog> :
    BaseConsumeScopeProvider,
    ICompensateActivityScopeProvider<TActivity, TLog>
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    /// <summary>Uses a registration context to create compensation scopes.</summary>
    /// <param name="context">The registration context that supplies services and ambient consume-context handling.</param>
    public CompensateActivityScopeProvider(IRegistrationContext context)
        : base(context)
    {
    }

    /// <summary>Uses explicit services and ambient-context handling to create compensation scopes.</summary>
    /// <param name="serviceProvider">The fallback service provider for newly created scopes.</param>
    /// <param name="setScopedConsumeContext">The component that installs and restores the scoped consume context.</param>
    public CompensateActivityScopeProvider(IServiceProvider serviceProvider, ISetScopedConsumeContext setScopedConsumeContext)
        : base(serviceProvider, setScopedConsumeContext)
    {
    }

    /// <summary>Creates or reuses a scope for a compensation context without resolving the activity.</summary>
    /// <param name="context">The compensation context to bind to the scope.</param>
    /// <param name="cancellationToken">The token that cancels scope acquisition before it starts.</param>
    /// <returns>A value task containing the scoped compensation context.</returns>
    public ValueTask<ICompensateScopeContext<TLog>> GetScopeAsync(CompensateContext<TLog> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<ICompensateScopeContext<TLog>>(cancellationToken);

        return GetScopeContextAsync(context, ExistingScopeContextFactory, CreatedScopeContextFactory, PipeContextFactory);
    }

    /// <summary>Creates or reuses a scope, resolves the activity, and binds it to the compensation context.</summary>
    /// <param name="context">The compensation context to bind to the resolved activity.</param>
    /// <param name="cancellationToken">The token that cancels scope acquisition before it starts.</param>
    /// <returns>A value task containing the resolved activity and its scoped compensation context.</returns>
    public ValueTask<ICompensateActivityScopeContext<TActivity, TLog>> GetActivityScopeAsync(CompensateContext<TLog> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<ICompensateActivityScopeContext<TActivity, TLog>>(cancellationToken);

        return GetScopeContextAsync(context, ExistingActivityScopeContextFactory, CreatedActivityScopeContextFactory, PipeContextFactory);
    }

    /// <summary>Identifies dependency injection as the activity-instance provider in the probe graph.</summary>
    /// <param name="context">The probe context that receives the provider tag.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
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
