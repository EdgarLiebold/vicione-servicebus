using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Uses a borrowed compensation scope and owns only its scoped-context restoration handle.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
internal sealed class ExistingCompensateActivityScopeContext<TActivity, TLog> :
    ICompensateActivityScopeContext<TActivity, TLog>
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    readonly IDisposable _disposable;
    readonly IServiceScope _scope;

    /// <summary>Creates a view over an already active activity scope.</summary>
    /// <param name="context">The compensation context containing the resolved activity.</param>
    /// <param name="scope">The borrowed dependency-injection scope used for service resolution.</param>
    /// <param name="disposable">The handle that restores the previously active scoped consume context.</param>
    public ExistingCompensateActivityScopeContext(CompensateActivityContext<TActivity, TLog> context, IServiceScope scope, IDisposable disposable)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _disposable = disposable ?? throw new ArgumentNullException(nameof(disposable));
    }

    /// <summary>Gets the compensation context containing the resolved activity.</summary>
    public CompensateActivityContext<TActivity, TLog> Context { get; }

    /// <summary>Restores the prior consume context without disposing the borrowed scope.</summary>
    /// <returns>A completed task.</returns>
    public ValueTask DisposeAsync()
    {
        _disposable.Dispose();
        return default;
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
