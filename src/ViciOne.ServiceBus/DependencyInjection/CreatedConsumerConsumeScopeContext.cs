using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides a created consumer consume scope context implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
/// <typeparam name="T">The t type.</typeparam>
public class CreatedConsumerConsumeScopeContext<TConsumer, T> :
    IConsumerConsumeScopeContext<TConsumer, T>
    where TConsumer : class
    where T : class
{
    readonly IDisposable _disposable;
    readonly IServiceScope _scope;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="scope">The scope value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="disposable">The disposable value.</param>
    public CreatedConsumerConsumeScopeContext(IServiceScope scope, ConsumerConsumeContext<TConsumer, T> context, IDisposable disposable)
    {
        _scope = scope;
        _disposable = disposable;
        Context = context;
    }

    /// <summary>
    /// Gets the context value.
    /// </summary>
    public ConsumerConsumeContext<TConsumer, T> Context { get; }

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
