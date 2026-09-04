using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides an existing compensate activity scope context implementation.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TLog">The t log type.</typeparam>
public class ExistingCompensateActivityScopeContext<TActivity, TLog> :
    ICompensateActivityScopeContext<TActivity, TLog>
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    readonly IDisposable _disposable;
    readonly IServiceScope _scope;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="scope">The scope value.</param>
    /// <param name="disposable">The disposable value.</param>
    public ExistingCompensateActivityScopeContext(CompensateActivityContext<TActivity, TLog> context, IServiceScope scope, IDisposable disposable)
    {
        _scope = scope;
        _disposable = disposable;
        Context = context;
    }

    /// <summary>
    /// Gets the context value.
    /// </summary>
    public CompensateActivityContext<TActivity, TLog> Context { get; }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ValueTask DisposeAsync()
    {
        _disposable?.Dispose();
        return default;
    }

    /// <summary>
    /// Gets service.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public T GetService<T>()
        where T : class
    {
        return ActivatorUtilities.GetServiceOrCreateInstance<T>(_scope.ServiceProvider);
    }
}
