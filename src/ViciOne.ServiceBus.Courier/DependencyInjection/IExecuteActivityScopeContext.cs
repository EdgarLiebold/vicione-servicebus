using System;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Exposes state for execute activity scope operations.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
internal interface IExecuteActivityScopeContext<out TActivity, out TArguments> :
    IAsyncDisposable
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    /// <summary>Gets the context.</summary>
    ExecuteActivityContext<TActivity, TArguments> Context { get; }

    /// <summary>Resolves a service from the activity scope, creating an instance when necessary.</summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The service.</returns>
    T GetService<T>()
        where T : class;
}
