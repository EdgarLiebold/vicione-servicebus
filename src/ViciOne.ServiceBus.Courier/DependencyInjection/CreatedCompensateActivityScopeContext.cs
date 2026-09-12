using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Owns a newly created compensation scope together with its activity context and scoped-context restoration.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
internal sealed class CreatedCompensateActivityScopeContext<TActivity, TLog> :
    ICompensateActivityScopeContext<TActivity, TLog>
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    readonly IDisposable _disposable;
    readonly IServiceScope _scope;

    /// <summary>Creates an owner for a newly allocated activity scope.</summary>
    /// <param name="context">The compensation context containing the resolved activity.</param>
    /// <param name="scope">The dependency-injection scope owned by this context.</param>
    /// <param name="disposable">The handle that restores the previously active scoped consume context.</param>
    public CreatedCompensateActivityScopeContext(CompensateActivityContext<TActivity, TLog> context, IServiceScope scope, IDisposable disposable)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _disposable = disposable ?? throw new ArgumentNullException(nameof(disposable));
    }

    /// <summary>Gets the compensation context containing the resolved activity.</summary>
    public CompensateActivityContext<TActivity, TLog> Context { get; }

    /// <summary>Restores the prior consume context and disposes the owned dependency-injection scope.</summary>
    /// <returns>A task that completes after asynchronous scope disposal, when supported.</returns>
    public ValueTask DisposeAsync()
    {
        return ActivityScopeDisposal.DisposeAsync(_disposable, _scope);
    }

    /// <summary>Resolves a service from the activity scope, creating an instance when necessary.</summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The service.</returns>
    public T GetService<T>()
        where T : class
    {
        return ActivatorUtilities.GetServiceOrCreateInstance<T>(_scope.ServiceProvider);
    }
}
