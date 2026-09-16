using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Carries state for existing consume scope operations.</summary>
public class ExistingConsumeScopeContext :
    IConsumeScopeContext
{
    readonly ConsumeScopeLifetime _lifetime;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="disposable">The disposable.</param>
    public ExistingConsumeScopeContext(ConsumeContext context, IDisposable disposable)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        _lifetime = new ConsumeScopeLifetime(disposable ?? throw new ArgumentNullException(nameof(disposable)));
    }

    /// <summary>Gets the context.</summary>
    public ConsumeContext Context { get; }

    /// <summary>Releases the resources owned by this instance.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public ValueTask DisposeAsync() => _lifetime.DisposeAsync();
}


/// <summary>Carries state for existing consume scope operations.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class ExistingConsumeScopeContext<TMessage> :
    IConsumeScopeContext<TMessage>
    where TMessage : class
{
    readonly ConsumeScopeLifetime _lifetime;
    readonly IServiceScope _scope;
    readonly ISetScopedConsumeContext _setter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="scope">The scope.</param>
    /// <param name="disposable">The disposable.</param>
    /// <param name="setter">The setter.</param>
    public ExistingConsumeScopeContext(ConsumeContext<TMessage> context, IServiceScope scope, IDisposable disposable, ISetScopedConsumeContext setter)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _lifetime = new ConsumeScopeLifetime(disposable ?? throw new ArgumentNullException(nameof(disposable)));
        _setter = setter ?? throw new ArgumentNullException(nameof(setter));
    }

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

    /// <summary>Creates instance.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="arguments">The arguments.</param>
    /// <returns>The created instance.</returns>
    public T CreateInstance<T>(params object[] arguments)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(arguments);
        return ActivatorUtilities.CreateInstance<T>(_scope.ServiceProvider, arguments);
    }

    /// <summary>Pushes consume context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The disposable produced by the operation.</returns>
    public IDisposable PushConsumeContext(ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _setter.PushContext(_scope, context);
    }

    /// <summary>Gets the context.</summary>
    public ConsumeContext<TMessage> Context { get; }
}
