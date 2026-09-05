using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Creates dependency-injection scopes for execute pipeline contexts.
/// </summary>
public sealed class ExecuteScopeProvider<TArguments> :
    BaseConsumeScopeProvider,
    IProbeSite
    where TArguments : class
{
    /// <summary>
    /// Initializes the provider from a registration context.
    /// </summary>
    public ExecuteScopeProvider(IRegistrationContext context)
        : base(context)
    {
    }

    /// <summary>
    /// Initializes the provider from an explicit service provider and context setter.
    /// </summary>
    public ExecuteScopeProvider(IServiceProvider serviceProvider, ISetScopedConsumeContext setScopedConsumeContext)
        : base(serviceProvider, setScopedConsumeContext)
    {
    }

    /// <summary>
    /// Gets the scoped execute context.
    /// </summary>
    public ValueTask<IExecuteScopeContext<TArguments>> GetScopeAsync(ExecuteContext<TArguments> context,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<IExecuteScopeContext<TArguments>>(cancellationToken);

        return GetScopeContextAsync(context, ExistingScopeContextFactory, CreatedScopeContextFactory, PipeContextFactory);
    }

    /// <inheritdoc />
    public void Probe(ProbeContext context)
    {
        context.Add("provider", "dependencyInjection");
    }

    static ExecuteContext<TArguments> PipeContextFactory(ExecuteContext<TArguments> context, IServiceScope serviceScope,
        IServiceProvider serviceProvider) =>
        new ExecuteContextScope<TArguments>(context, serviceScope, serviceScope.ServiceProvider, serviceProvider);

    static IExecuteScopeContext<TArguments> ExistingScopeContextFactory(ExecuteContext<TArguments> context,
        IServiceScope serviceScope, IDisposable disposable) =>
        new ExistingExecuteScopeContext<TArguments>(context, serviceScope, disposable);

    static IExecuteScopeContext<TArguments> CreatedScopeContextFactory(ExecuteContext<TArguments> context,
        IServiceScope serviceScope, IDisposable disposable) =>
        new CreatedExecuteScopeContext<TArguments>(context, serviceScope, disposable);
}
