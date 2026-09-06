using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Carries state for created consumer consume scope operations.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="T">The value type.</typeparam>
public class CreatedConsumerConsumeScopeContext<TConsumer, T> :
    IConsumerConsumeScopeContext<TConsumer, T>
    where TConsumer : class
    where T : class
{
    readonly IDisposable _disposable;
    readonly IServiceScope _scope;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="scope">The scope.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="disposable">The disposable.</param>
    public CreatedConsumerConsumeScopeContext(IServiceScope scope, ConsumerConsumeContext<TConsumer, T> context, IDisposable disposable)
    {
        _scope = scope;
        _disposable = disposable;
        Context = context;
    }

    /// <summary>Gets the context.</summary>
    public ConsumerConsumeContext<TConsumer, T> Context { get; }

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
