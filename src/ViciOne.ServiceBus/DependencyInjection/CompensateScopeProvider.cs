using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Creates dependency-injection scopes for compensate pipeline contexts.</summary>
/// <typeparam name="TLog">The log type.</typeparam>
public sealed class CompensateScopeProvider<TLog> :
    BaseConsumeScopeProvider,
    IProbeSite
    where TLog : class
{
    /// <summary>Initializes the provider from a registration context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public CompensateScopeProvider(IRegistrationContext context)
        : base(context)
    {
    }

    /// <summary>Initializes the provider from an explicit service provider and context setter.</summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="setScopedConsumeContext">The set scoped consume context.</param>
    public CompensateScopeProvider(IServiceProvider serviceProvider, ISetScopedConsumeContext setScopedConsumeContext)
        : base(serviceProvider, setScopedConsumeContext)
    {
    }

    /// <summary>Gets scope.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public ValueTask<ICompensateScopeContext<TLog>> GetScopeAsync(CompensateContext<TLog> context,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<ICompensateScopeContext<TLog>>(cancellationToken);

        return GetScopeContextAsync(context, ExistingScopeContextFactory, CreatedScopeContextFactory, PipeContextFactory);
    }

    /// <inheritdoc />
    public void Probe(ProbeContext context)
    {
        context.Add("provider", "dependencyInjection");
    }

    static CompensateContext<TLog> PipeContextFactory(CompensateContext<TLog> context, IServiceScope serviceScope,
        IServiceProvider serviceProvider) =>
        new CompensateContextScope<TLog>(context, serviceScope, serviceScope.ServiceProvider, serviceProvider);

    static ICompensateScopeContext<TLog> ExistingScopeContextFactory(CompensateContext<TLog> context,
        IServiceScope serviceScope, IDisposable disposable) =>
        new ExistingCompensateScopeContext<TLog>(context, serviceScope, disposable);

    static ICompensateScopeContext<TLog> CreatedScopeContextFactory(CompensateContext<TLog> context,
        IServiceScope serviceScope, IDisposable disposable) =>
        new CreatedCompensateScopeContext<TLog>(serviceScope, context, disposable);
}
