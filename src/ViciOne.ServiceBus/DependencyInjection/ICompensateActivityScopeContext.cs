using System;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Defines the contract for compensate activity scope context.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TLog">The t log type.</typeparam>
public interface ICompensateActivityScopeContext<out TActivity, out TLog> :
    IAsyncDisposable
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    /// <summary>
    /// Gets the context value.
    /// </summary>
    CompensateActivityContext<TActivity, TLog> Context { get; }

    /// <summary>
    /// Gets service.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    T GetService<T>()
        where T : class;
}
