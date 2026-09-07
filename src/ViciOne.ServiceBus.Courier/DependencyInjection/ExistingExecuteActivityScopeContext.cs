using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Carries state for existing execute activity scope operations.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
internal sealed class ExistingExecuteActivityScopeContext<TActivity, TArguments> :
    IExecuteActivityScopeContext<TActivity, TArguments>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly IDisposable _disposable;
    readonly IServiceScope _scope;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="scope">The scope.</param>
    /// <param name="disposable">The disposable.</param>
    public ExistingExecuteActivityScopeContext(ExecuteActivityContext<TActivity, TArguments> context, IServiceScope scope, IDisposable disposable)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _disposable = disposable ?? throw new ArgumentNullException(nameof(disposable));
    }

    /// <summary>Gets the context.</summary>
    public ExecuteActivityContext<TActivity, TArguments> Context { get; }

    /// <summary>Releases the resources owned by this instance.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
