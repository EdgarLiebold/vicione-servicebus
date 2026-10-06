using System;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Exposes state for consume scope operations.</summary>
public interface IConsumeScopeContext :
    IAsyncDisposable
{
    /// <summary>Gets the context.</summary>
    ConsumeContext Context { get; }
}


/// <summary>Exposes state for consume scope operations.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IConsumeScopeContext<out TMessage> :
    IAsyncDisposable
    where TMessage : class
{
    /// <summary>Gets the context.</summary>
    ConsumeContext<TMessage> Context { get; }

    /// <summary>Resolves a service or creates a fallback instance.</summary>
    /// <remarks>
    /// The default provider borrows registered services and owns each fallback created by this method.
    /// It releases fallbacks asynchronously when supported, before restoring the context and releasing an owned DI scope.
    /// Closing rejects new resolutions and waits for admitted resolutions and cleanup; repeated disposal awaits the same outcome.
    /// </remarks>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The service.</returns>
    T GetService<T>()
        where T : class;

    /// <summary>Creates a caller-owned instance.</summary>
    /// <remarks>The default provider does not add explicitly created instances to its fallback cleanup.</remarks>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="arguments">The arguments.</param>
    /// <returns>The created instance.</returns>
    T CreateInstance<T>(params object[] arguments)
        where T : class;

    /// <summary>Pushes consume context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The disposable produced by the operation.</returns>
    IDisposable PushConsumeContext(ConsumeContext context);
}
