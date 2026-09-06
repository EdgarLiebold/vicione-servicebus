using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Carries state for created compensate scope operations.</summary>
/// <typeparam name="TLog">The log type.</typeparam>
public class CreatedCompensateScopeContext<TLog> :
    ICompensateScopeContext<TLog>
    where TLog : class
{
    readonly IDisposable _disposable;
    readonly IServiceScope _scope;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="scope">The scope.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="disposable">The disposable.</param>
    public CreatedCompensateScopeContext(IServiceScope scope, CompensateContext<TLog> context, IDisposable disposable)
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

        if (_scope is IAsyncDisposable asyncDisposable)
            return asyncDisposable.DisposeAsync();

        _scope?.Dispose();
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
