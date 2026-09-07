using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Carries state for created compensate activity scope operations.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
internal sealed class CreatedCompensateActivityScopeContext<TActivity, TLog> :
    ICompensateActivityScopeContext<TActivity, TLog>
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    readonly IDisposable _disposable;
    readonly IServiceScope _scope;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="scope">The scope.</param>
    /// <param name="disposable">The disposable.</param>
    public CreatedCompensateActivityScopeContext(CompensateActivityContext<TActivity, TLog> context, IServiceScope scope, IDisposable disposable)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _disposable = disposable ?? throw new ArgumentNullException(nameof(disposable));
    }

    /// <summary>Gets the context.</summary>
    public CompensateActivityContext<TActivity, TLog> Context { get; }

    /// <summary>Releases the resources owned by this instance.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public ValueTask DisposeAsync()
    {
        _disposable.Dispose();

        if (_scope is IAsyncDisposable asyncDisposable)
            return asyncDisposable.DisposeAsync();

        _scope.Dispose();
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
