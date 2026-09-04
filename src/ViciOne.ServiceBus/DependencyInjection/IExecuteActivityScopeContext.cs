using System;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Defines the contract for execute activity scope context.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
public interface IExecuteActivityScopeContext<out TActivity, out TArguments> :
    IAsyncDisposable
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    /// <summary>
    /// Gets the context value.
    /// </summary>
    ExecuteActivityContext<TActivity, TArguments> Context { get; }

    /// <summary>
    /// Gets service.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    T GetService<T>()
        where T : class;
}
