using System;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides a set scoped consume context implementation.
/// </summary>
public class SetScopedConsumeContext :
    ISetScopedConsumeContext
{
    readonly Func<IServiceProvider, IScopedConsumeContextProvider> _setterProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="setterProvider">The setter provider value.</param>
    public SetScopedConsumeContext(Func<IServiceProvider, IScopedConsumeContextProvider> setterProvider)
    {
        _setterProvider = setterProvider;
    }

    /// <summary>
    /// Performs the push context operation.
    /// </summary>
    /// <param name="scope">The scope value.</param>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public IDisposable PushContext(IServiceScope scope, ConsumeContext context)
    {
        return _setterProvider(scope.ServiceProvider).PushContext(context);
    }
}


/// <summary>
/// Provides a set scoped consume context implementation.
/// </summary>
/// <typeparam name="TBus">The t bus type.</typeparam>
public class SetScopedConsumeContext<TBus> :
    ISetScopedConsumeContext
    where TBus : class, IBus
{
    readonly Func<IServiceProvider, IScopedConsumeContextProvider> _setterProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="setterProvider">The setter provider value.</param>
    public SetScopedConsumeContext(Func<IServiceProvider, IScopedConsumeContextProvider> setterProvider)
    {
        _setterProvider = setterProvider;
    }

    /// <summary>
    /// Performs the push context operation.
    /// </summary>
    /// <param name="scope">The scope value.</param>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public IDisposable PushContext(IServiceScope scope, ConsumeContext context)
    {
        return _setterProvider(scope.ServiceProvider).PushContext(context);
    }
}
