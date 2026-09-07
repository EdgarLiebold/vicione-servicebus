using System;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Exposes state for compensate activity scope operations.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
internal interface ICompensateActivityScopeContext<out TActivity, out TLog> :
    IAsyncDisposable
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    /// <summary>Gets the context.</summary>
    CompensateActivityContext<TActivity, TLog> Context { get; }

    /// <summary>Resolves a service from the activity scope, creating an instance when necessary.</summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The service.</returns>
    T GetService<T>()
        where T : class;
}
