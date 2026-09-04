using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides a created consume scope context implementation.
/// </summary>
public class CreatedConsumeScopeContext :
    IConsumeScopeContext
{
    readonly IDisposable _disposable;
    readonly IServiceScope _scope;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="scope">The scope value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="disposable">The disposable value.</param>
    public CreatedConsumeScopeContext(IServiceScope scope, ConsumeContext context, IDisposable disposable)
    {
        _scope = scope;
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

        if (_scope is IAsyncDisposable asyncDisposable)
            return asyncDisposable.DisposeAsync();

        _scope?.Dispose();
        return default;
    }
}


/// <summary>
/// Provides a created consume scope context implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class CreatedConsumeScopeContext<TMessage> :
    IConsumeScopeContext<TMessage>
    where TMessage : class
{
    readonly IDisposable _disposable;
    readonly IServiceScope _scope;
    readonly ISetScopedConsumeContext _setter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="scope">The scope value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="disposable">The disposable value.</param>
    /// <param name="setter">The setter value.</param>
    public CreatedConsumeScopeContext(IServiceScope scope, ConsumeContext<TMessage> context, IDisposable disposable, ISetScopedConsumeContext setter)
    {
        _scope = scope;
        _disposable = disposable;
        _setter = setter;
        Context = context;
    }

    /// <summary>
    /// Gets the context value.
    /// </summary>
    public ConsumeContext<TMessage> Context { get; }

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
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ValueTask DisposeAsync()
    {
        _disposable?.Dispose();

        if (_scope is IAsyncDisposable asyncDisposable)
            return asyncDisposable.DisposeAsync();

        _scope?.Dispose();
        return default;
    }
}
