using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Provides execute activity scope services.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public class ExecuteActivityScopeProvider<TActivity, TArguments> :
    BaseConsumeScopeProvider,
    IExecuteActivityScopeProvider<TActivity, TArguments>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public ExecuteActivityScopeProvider(IRegistrationContext context)
        : base(context)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="setScopedConsumeContext">The set scoped consume context.</param>
    public ExecuteActivityScopeProvider(IServiceProvider serviceProvider, ISetScopedConsumeContext setScopedConsumeContext)
        : base(serviceProvider, setScopedConsumeContext)
    {
    }

    /// <summary>Gets scope.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public ValueTask<IExecuteScopeContext<TArguments>> GetScopeAsync(ExecuteContext<TArguments> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled<global::ViciOne.ServiceBus.DependencyInjection.IExecuteScopeContext<TArguments>>(cancellationToken); return GetScopeContextAsync(context, ExistingScopeContextFactory, CreatedScopeContextFactory, PipeContextFactory);
    }

    /// <summary>Gets activity scope.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public ValueTask<IExecuteActivityScopeContext<TActivity, TArguments>> GetActivityScopeAsync(ExecuteContext<TArguments> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled<global::ViciOne.ServiceBus.DependencyInjection.IExecuteActivityScopeContext<TActivity, TArguments>>(cancellationToken); return GetScopeContextAsync(context, ExistingActivityScopeContextFactory, CreatedActivityScopeContextFactory, PipeContextFactory);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
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
