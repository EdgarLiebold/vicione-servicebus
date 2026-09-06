using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Carries state for existing compensate scope operations.</summary>
/// <typeparam name="TLog">The log type.</typeparam>
public class ExistingCompensateScopeContext<TLog> :
    ICompensateScopeContext<TLog>
    where TLog : class
{
    readonly IDisposable _disposable;
    readonly IServiceScope _scope;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="scope">The scope.</param>
    /// <param name="disposable">The disposable.</param>
    public ExistingCompensateScopeContext(CompensateContext<TLog> context, IServiceScope scope, IDisposable disposable)
    {
        _scope = scope;
        _disposable = disposable;
        Context = context;
    }

    /// <summary>Gets the context.</summary>
    public CompensateContext<TLog> Context { get; }

    /// <summary>Releases the resources owned by this instance.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public ValueTask DisposeAsync()
    {
        _disposable?.Dispose();
        return default;
    }

    /// <summary>Gets service.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The service.</returns>
    public T GetService<T>()
        where T : class
    {
        return ActivatorUtilities.GetServiceOrCreateInstance<T>(_scope.ServiceProvider);
    }
}
