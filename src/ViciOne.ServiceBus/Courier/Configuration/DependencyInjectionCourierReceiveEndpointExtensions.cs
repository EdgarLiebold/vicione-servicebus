using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides dependency-injection receive-endpoint extensions for Courier activities.
/// </summary>
public static class DependencyInjectionCourierReceiveEndpointExtensions
{
    /// <summary>
    /// Performs the execute activity host operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="compensateAddress">The compensate address value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="configure">The configuration callback.</param>
    public static void ExecuteActivityHost<TActivity, TArguments>(this IReceiveEndpointConfigurator configurator, Uri compensateAddress,
        IRegistrationContext context, Action<IExecuteActivityConfigurator<TActivity, TArguments>>? configure = null)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        var executeActivityScopeProvider = new ExecuteActivityScopeProvider<TActivity, TArguments>(context);

        var factory = new ScopeExecuteActivityFactory<TActivity, TArguments>(executeActivityScopeProvider);

        configurator.ExecuteActivityHost(compensateAddress, factory, configure);
    }


    /// <summary>
    /// Performs the execute activity host operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="configure">The configuration callback.</param>
    public static void ExecuteActivityHost<TActivity, TArguments>(this IReceiveEndpointConfigurator configurator, IRegistrationContext context,
        Action<IExecuteActivityConfigurator<TActivity, TArguments>>? configure = null)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        var executeActivityScopeProvider = new ExecuteActivityScopeProvider<TActivity, TArguments>(context);

        var factory = new ScopeExecuteActivityFactory<TActivity, TArguments>(executeActivityScopeProvider);

        configurator.ExecuteActivityHost(factory, configure);
    }


    /// <summary>
    /// Performs the compensate activity host operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="configure">The configuration callback.</param>
    public static void CompensateActivityHost<TActivity, TLog>(this IReceiveEndpointConfigurator configurator, IRegistrationContext context,
        Action<ICompensateActivityConfigurator<TActivity, TLog>>? configure = null)
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class
    {
        var compensateActivityScopeProvider = new CompensateActivityScopeProvider<TActivity, TLog>(context);

        var factory = new ScopeCompensateActivityFactory<TActivity, TLog>(compensateActivityScopeProvider);

        configurator.CompensateActivityHost(factory, configure);
    }
}
