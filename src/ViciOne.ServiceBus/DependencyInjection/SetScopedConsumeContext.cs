using System;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Carries state for set scoped consume operations.</summary>
public class SetScopedConsumeContext :
    ISetScopedConsumeContext
{
    readonly Func<IServiceProvider, IScopedConsumeContextProvider> _setterProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="setterProvider">The setter provider.</param>
    public SetScopedConsumeContext(Func<IServiceProvider, IScopedConsumeContextProvider> setterProvider)
    {
        ArgumentNullException.ThrowIfNull(setterProvider);

        _setterProvider = setterProvider;
    }

    /// <summary>Pushes context.</summary>
    /// <param name="scope">The scope.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The disposable produced by the operation.</returns>
    public IDisposable PushContext(IServiceScope scope, ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(context);

        return _setterProvider(scope.ServiceProvider).PushContext(context);
    }
}


/// <summary>Carries state for set scoped consume operations.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
public class SetScopedConsumeContext<TBus> :
    ISetScopedConsumeContext
    where TBus : class, IBus
{
    readonly Func<IServiceProvider, IScopedConsumeContextProvider> _setterProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="setterProvider">The setter provider.</param>
    public SetScopedConsumeContext(Func<IServiceProvider, IScopedConsumeContextProvider> setterProvider)
    {
        ArgumentNullException.ThrowIfNull(setterProvider);

        _setterProvider = setterProvider;
    }

    /// <summary>Pushes context.</summary>
    /// <param name="scope">The scope.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The disposable produced by the operation.</returns>
    public IDisposable PushContext(IServiceScope scope, ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(context);

        return _setterProvider(scope.ServiceProvider).PushContext(context);
    }
}
