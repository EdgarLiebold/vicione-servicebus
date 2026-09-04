using System;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Defines the contract for consume scope context.
/// </summary>
public interface IConsumeScopeContext :
    IAsyncDisposable
{
    /// <summary>
    /// Gets the context value.
    /// </summary>
    ConsumeContext Context { get; }
}


/// <summary>
/// Defines the contract for consume scope context.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IConsumeScopeContext<out TMessage> :
    IAsyncDisposable
    where TMessage : class
{
    /// <summary>
    /// Gets the context value.
    /// </summary>
    ConsumeContext<TMessage> Context { get; }

    /// <summary>
    /// Gets service.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    T GetService<T>()
        where T : class;

    /// <summary>
    /// Creates instance.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="arguments">The arguments value.</param>
    /// <returns>The result of the operation.</returns>
    T CreateInstance<T>(params object[] arguments)
        where T : class;

    /// <summary>
    /// Performs the push consume context operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    IDisposable PushConsumeContext(ConsumeContext context);
}
