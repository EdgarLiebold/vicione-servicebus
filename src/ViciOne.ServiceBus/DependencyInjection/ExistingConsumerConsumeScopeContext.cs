using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Carries state for existing consumer consume scope operations.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="T">The value type.</typeparam>
public class ExistingConsumerConsumeScopeContext<TConsumer, T> :
    IConsumerConsumeScopeContext<TConsumer, T>
    where TConsumer : class
    where T : class
{
    readonly IDisposable _disposable;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="disposable">The disposable.</param>
    public ExistingConsumerConsumeScopeContext(ConsumerConsumeContext<TConsumer, T> context, IDisposable disposable)
    {
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
        return default;
    }
}
