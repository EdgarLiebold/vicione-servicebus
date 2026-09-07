using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Provides compensate activity scope services.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
internal sealed class CompensateActivityScopeProvider<TActivity, TLog> :
    BaseConsumeScopeProvider,
    ICompensateActivityScopeProvider<TActivity, TLog>
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public CompensateActivityScopeProvider(IRegistrationContext context)
        : base(context)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="setScopedConsumeContext">The set scoped consume context.</param>
    public CompensateActivityScopeProvider(IServiceProvider serviceProvider, ISetScopedConsumeContext setScopedConsumeContext)
        : base(serviceProvider, setScopedConsumeContext)
    {
    }

    /// <summary>Gets scope.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public ValueTask<ICompensateScopeContext<TLog>> GetScopeAsync(CompensateContext<TLog> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<ICompensateScopeContext<TLog>>(cancellationToken);

        return GetScopeContextAsync(context, ExistingScopeContextFactory, CreatedScopeContextFactory, PipeContextFactory);
    }

    /// <summary>Gets activity scope.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public ValueTask<ICompensateActivityScopeContext<TActivity, TLog>> GetActivityScopeAsync(CompensateContext<TLog> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<ICompensateActivityScopeContext<TActivity, TLog>>(cancellationToken);

        return GetScopeContextAsync(context, ExistingActivityScopeContextFactory, CreatedActivityScopeContextFactory, PipeContextFactory);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
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
