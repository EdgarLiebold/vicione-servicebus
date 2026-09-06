using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Carries state for created consume scope operations.</summary>
public class CreatedConsumeScopeContext :
    IConsumeScopeContext
{
    readonly IDisposable _disposable;
    readonly IServiceScope _scope;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="scope">The scope.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="disposable">The disposable.</param>
    public CreatedConsumeScopeContext(IServiceScope scope, ConsumeContext context, IDisposable disposable)
    {
        _scope = scope;
        _disposable = disposable;
        Context = context;
    }

    /// <summary>Gets the context.</summary>
    public ConsumeContext Context { get; }

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
}


/// <summary>Carries state for created consume scope operations.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class CreatedConsumeScopeContext<TMessage> :
    IConsumeScopeContext<TMessage>
    where TMessage : class
{
    readonly IDisposable _disposable;
    readonly IServiceScope _scope;
    readonly ISetScopedConsumeContext _setter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="scope">The scope.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="disposable">The disposable.</param>
    /// <param name="setter">The setter.</param>
    public CreatedConsumeScopeContext(IServiceScope scope, ConsumeContext<TMessage> context, IDisposable disposable, ISetScopedConsumeContext setter)
    {
        _scope = scope;
        _disposable = disposable;
        _setter = setter;
        Context = context;
    }

    /// <summary>Gets the context.</summary>
    public ConsumeContext<TMessage> Context { get; }

    /// <summary>Gets service.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The service.</returns>
    public T GetService<T>()
        where T : class
    {
        return ActivatorUtilities.GetServiceOrCreateInstance<T>(_scope.ServiceProvider);
    }

    /// <summary>Creates instance.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="arguments">The arguments.</param>
    /// <returns>The created instance.</returns>
    public T CreateInstance<T>(params object[] arguments)
        where T : class
    {
        return ActivatorUtilities.CreateInstance<T>(_scope.ServiceProvider, arguments);
    }

    /// <summary>Pushes consume context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The disposable produced by the operation.</returns>
    public IDisposable PushConsumeContext(ConsumeContext context)
    {
        return _setter.PushContext(_scope, context);
    }

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
}
