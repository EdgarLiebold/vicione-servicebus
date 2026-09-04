using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides an existing consume scope context implementation.
/// </summary>
public class ExistingConsumeScopeContext :
    IConsumeScopeContext
{
    readonly IDisposable _disposable;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="disposable">The disposable value.</param>
    public ExistingConsumeScopeContext(ConsumeContext context, IDisposable disposable)
    {
        _disposable = disposable;
        Context = context;
    }

    /// <summary>
    /// Gets the context value.
    /// </summary>
    public ConsumeContext Context { get; }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ValueTask DisposeAsync()
    {
        _disposable?.Dispose();
        return default;
    }
}


/// <summary>
/// Provides an existing consume scope context implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class ExistingConsumeScopeContext<TMessage> :
    IConsumeScopeContext<TMessage>
    where TMessage : class
{
    readonly IDisposable _disposable;
    readonly IServiceScope _scope;
    readonly ISetScopedConsumeContext _setter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="scope">The scope value.</param>
    /// <param name="disposable">The disposable value.</param>
    /// <param name="setter">The setter value.</param>
    public ExistingConsumeScopeContext(ConsumeContext<TMessage> context, IServiceScope scope, IDisposable disposable, ISetScopedConsumeContext setter)
    {
        Context = context;
        _scope = scope;
        _disposable = disposable;
        _setter = setter;
    }

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

    /// <summary>
    /// Creates instance.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="arguments">The arguments value.</param>
    /// <returns>The result of the operation.</returns>
    public T CreateInstance<T>(params object[] arguments)
        where T : class
    {
        return ActivatorUtilities.CreateInstance<T>(_scope.ServiceProvider, arguments);
    }

    /// <summary>
    /// Performs the push consume context operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public IDisposable PushConsumeContext(ConsumeContext context)
    {
        return _setter.PushContext(_scope, context);
    }

    /// <summary>
    /// Gets the context value.
    /// </summary>
    public ConsumeContext<TMessage> Context { get; }
}
