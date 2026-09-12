using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Creates execution scopes and resolves activity instances from their scoped service providers.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
internal sealed class ExecuteActivityScopeProvider<TActivity, TArguments> :
    BaseConsumeScopeProvider,
    IExecuteActivityScopeProvider<TActivity, TArguments>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    /// <summary>Uses a registration context to create execution scopes.</summary>
    /// <param name="context">The registration context that supplies services and ambient consume-context handling.</param>
    public ExecuteActivityScopeProvider(IRegistrationContext context)
        : base(context)
    {
    }

    /// <summary>Uses explicit services and ambient-context handling to create execution scopes.</summary>
    /// <param name="serviceProvider">The fallback service provider for newly created scopes.</param>
    /// <param name="setScopedConsumeContext">The component that installs and restores the scoped consume context.</param>
    public ExecuteActivityScopeProvider(IServiceProvider serviceProvider, ISetScopedConsumeContext setScopedConsumeContext)
        : base(serviceProvider, setScopedConsumeContext)
    {
    }

    /// <summary>Creates or reuses a scope for an execution context without resolving the activity.</summary>
    /// <param name="context">The execution context to bind to the scope.</param>
    /// <param name="cancellationToken">The token that cancels scope acquisition before it starts.</param>
    /// <returns>A value task containing the scoped execution context.</returns>
    public ValueTask<IExecuteScopeContext<TArguments>> GetScopeAsync(ExecuteContext<TArguments> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<IExecuteScopeContext<TArguments>>(cancellationToken);

        return GetScopeContextAsync(context, ExistingScopeContextFactory, CreatedScopeContextFactory, PipeContextFactory);
    }

    /// <summary>Creates or reuses a scope, resolves the activity, and binds it to the execution context.</summary>
    /// <param name="context">The execution context to bind to the resolved activity.</param>
    /// <param name="cancellationToken">The token that cancels scope acquisition before it starts.</param>
    /// <returns>A value task containing the resolved activity and its scoped execution context.</returns>
    public ValueTask<IExecuteActivityScopeContext<TActivity, TArguments>> GetActivityScopeAsync(ExecuteContext<TArguments> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<IExecuteActivityScopeContext<TActivity, TArguments>>(cancellationToken);

        return GetScopeContextAsync(context, ExistingActivityScopeContextFactory, CreatedActivityScopeContextFactory, PipeContextFactory);
    }

    /// <summary>Identifies dependency injection as the activity-instance provider in the probe graph.</summary>
    /// <param name="context">The probe context that receives the provider tag.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Add("provider", "dependencyInjection");
    }

    static ExecuteContext<TArguments> PipeContextFactory(ExecuteContext<TArguments> consumeContext, IServiceScope serviceScope,
        IServiceProvider serviceProvider)
    {
        return new ExecuteContextScope<TArguments>(consumeContext, serviceScope, serviceScope.ServiceProvider, serviceProvider);
    }

    static IExecuteScopeContext<TArguments> ExistingScopeContextFactory(ExecuteContext<TArguments> consumeContext, IServiceScope serviceScope,
        IDisposable disposable)
    {
        return new ExistingExecuteScopeContext<TArguments>(consumeContext, serviceScope, disposable);
    }

    static IExecuteScopeContext<TArguments> CreatedScopeContextFactory(ExecuteContext<TArguments> consumeContext, IServiceScope serviceScope,
        IDisposable disposable)
    {
        return new CreatedExecuteScopeContext<TArguments>(consumeContext, serviceScope, disposable);
    }

    static IExecuteActivityScopeContext<TActivity, TArguments> ExistingActivityScopeContextFactory(ExecuteContext<TArguments> consumeContext,
        IServiceScope serviceScope, IDisposable disposable)
    {
        var activity = serviceScope.ServiceProvider.GetService<TActivity>();
        if (activity == null)
            throw new ConsumerException($"Unable to resolve activity type '{TypeCache<TActivity>.ShortName}'.");

        ExecuteActivityContext<TActivity, TArguments> activityContext = consumeContext.CreateActivityContext(activity);

        return new ExistingExecuteActivityScopeContext<TActivity, TArguments>(activityContext, serviceScope, disposable);
    }

    static IExecuteActivityScopeContext<TActivity, TArguments> CreatedActivityScopeContextFactory(ExecuteContext<TArguments> consumeContext,
        IServiceScope serviceScope, IDisposable disposable)
    {
        var activity = serviceScope.ServiceProvider.GetService<TActivity>();
        if (activity == null)
            throw new ConsumerException($"Unable to resolve activity type '{TypeCache<TActivity>.ShortName}'.");

        ExecuteActivityContext<TActivity, TArguments> activityContext = consumeContext.CreateActivityContext(activity);

        return new CreatedExecuteActivityScopeContext<TActivity, TArguments>(activityContext, serviceScope, disposable);
    }
}
