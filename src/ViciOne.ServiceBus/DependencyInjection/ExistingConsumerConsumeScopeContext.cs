using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides an existing consumer consume scope context implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
/// <typeparam name="T">The t type.</typeparam>
public class ExistingConsumerConsumeScopeContext<TConsumer, T> :
    IConsumerConsumeScopeContext<TConsumer, T>
    where TConsumer : class
    where T : class
{
    readonly IDisposable _disposable;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="disposable">The disposable value.</param>
    public ExistingConsumerConsumeScopeContext(ConsumerConsumeContext<TConsumer, T> context, IDisposable disposable)
    {
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
        return default;
    }
}
