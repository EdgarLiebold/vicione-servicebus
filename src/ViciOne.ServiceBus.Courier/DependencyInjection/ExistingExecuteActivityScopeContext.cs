using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Uses a borrowed execution scope and owns only its scoped-context restoration handle.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
internal sealed class ExistingExecuteActivityScopeContext<TActivity, TArguments> :
    IExecuteActivityScopeContext<TActivity, TArguments>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly ActivityScopeLifetime _lifetime;
    readonly IServiceScope _scope;

    /// <summary>Creates a view over an already active activity scope.</summary>
    /// <param name="context">The execution context containing the resolved activity.</param>
    /// <param name="scope">The borrowed dependency-injection scope used for service resolution.</param>
    /// <param name="disposable">The handle that restores the previously active scoped consume context.</param>
    public ExistingExecuteActivityScopeContext(ExecuteActivityContext<TActivity, TArguments> context, IServiceScope scope, IDisposable disposable)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _lifetime = new ActivityScopeLifetime(disposable ?? throw new ArgumentNullException(nameof(disposable)));
    }

    /// <summary>Gets the execution context containing the resolved activity.</summary>
    public ExecuteActivityContext<TActivity, TArguments> Context { get; }

    /// <summary>Restores the prior consume context without disposing the borrowed scope.</summary>
    /// <returns>A completed task.</returns>
    public ValueTask DisposeAsync() => _lifetime.DisposeAsync();

    /// <summary>Resolves a service from the activity scope, creating an instance when necessary.</summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The service.</returns>
    public T GetService<T>()
        where T : class
    {
        return ActivatorUtilities.GetServiceOrCreateInstance<T>(_scope.ServiceProvider);
    }
}
