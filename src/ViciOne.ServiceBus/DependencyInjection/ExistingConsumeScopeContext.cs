using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Carries state for existing consume scope operations.</summary>
public class ExistingConsumeScopeContext :
    IConsumeScopeContext
{
    readonly IDisposable _disposable;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="disposable">The disposable.</param>
    public ExistingConsumeScopeContext(ConsumeContext context, IDisposable disposable)
    {
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
        return default;
    }
}


/// <summary>Carries state for existing consume scope operations.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class ExistingConsumeScopeContext<TMessage> :
    IConsumeScopeContext<TMessage>
    where TMessage : class
{
    readonly IDisposable _disposable;
    readonly IServiceScope _scope;
    readonly ISetScopedConsumeContext _setter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="scope">The scope.</param>
    /// <param name="disposable">The disposable.</param>
    /// <param name="setter">The setter.</param>
    public ExistingConsumeScopeContext(ConsumeContext<TMessage> context, IServiceScope scope, IDisposable disposable, ISetScopedConsumeContext setter)
    {
        Context = context;
        _scope = scope;
        _disposable = disposable;
        _setter = setter;
    }

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

    /// <summary>Gets the context.</summary>
    public ConsumeContext<TMessage> Context { get; }
}
