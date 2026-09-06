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

    /// <summary>Gets service.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The service.</returns>
    T GetService<T>()
        where T : class;

    /// <summary>Creates instance.</summary>
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
