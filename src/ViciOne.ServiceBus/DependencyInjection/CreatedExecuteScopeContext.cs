using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Carries state for created execute scope operations.</summary>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public class CreatedExecuteScopeContext<TArguments> :
    IExecuteScopeContext<TArguments>
    where TArguments : class
{
    readonly ConsumeScopeLifetime _lifetime;
    readonly IServiceScope _scope;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="scope">The scope.</param>
    /// <param name="disposable">The disposable.</param>
    public CreatedExecuteScopeContext(ExecuteContext<TArguments> context, IServiceScope scope, IDisposable disposable)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _lifetime = new ConsumeScopeLifetime(disposable ?? throw new ArgumentNullException(nameof(disposable)), scope);
    }

    /// <summary>Gets the context.</summary>
    public ExecuteContext<TArguments> Context { get; }

    /// <summary>Releases the resources owned by this instance.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public ValueTask DisposeAsync() => _lifetime.DisposeAsync();

    /// <summary>Gets service.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The service.</returns>
    public T GetService<T>()
        where T : class
    {
        return ActivatorUtilities.GetServiceOrCreateInstance<T>(_scope.ServiceProvider);
    }
}
