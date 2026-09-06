using System;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Exposes state for execute scope operations.</summary>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public interface IExecuteScopeContext<out TArguments> :
    IAsyncDisposable
    where TArguments : class
{
    /// <summary>Gets the context.</summary>
    ExecuteContext<TArguments> Context { get; }

    /// <summary>Gets service.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The service.</returns>
    T GetService<T>()
        where T : class;
}
