using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides dependency-injection receive-endpoint extensions for Courier activities.</summary>
public static class DependencyInjectionCourierReceiveEndpointExtensions
{
    /// <summary>Configures a dependency-injection activity host with compensation.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="compensateAddress">The companion endpoint that compensates completed executions.</param>
    /// <param name="context">The bus registration context used to resolve scoped activity instances.</param>
    /// <param name="configure">The optional callback that configures the activity execution pipeline.</param>
    public static void ExecuteActivityHost<TActivity, TArguments>(this IReceiveEndpointConfigurator configurator, Uri compensateAddress,
        IRegistrationContext context, Action<IExecuteActivityConfigurator<TActivity, TArguments>>? configure = null)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(compensateAddress);
        ArgumentNullException.ThrowIfNull(context);

        var executeActivityScopeProvider = new ExecuteActivityScopeProvider<TActivity, TArguments>(context);

        var factory = new ScopeExecuteActivityFactory<TActivity, TArguments>(executeActivityScopeProvider);

        configurator.ExecuteActivityHost(compensateAddress, factory, configure);
    }


    /// <summary>Configures a dependency-injection execute-only activity host.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The bus registration context used to resolve scoped activity instances.</param>
    /// <param name="configure">The optional callback that configures the activity execution pipeline.</param>
    public static void ExecuteActivityHost<TActivity, TArguments>(this IReceiveEndpointConfigurator configurator, IRegistrationContext context,
        Action<IExecuteActivityConfigurator<TActivity, TArguments>>? configure = null)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(context);

        var executeActivityScopeProvider = new ExecuteActivityScopeProvider<TActivity, TArguments>(context);

        var factory = new ScopeExecuteActivityFactory<TActivity, TArguments>(executeActivityScopeProvider);

        configurator.ExecuteActivityHost(factory, configure);
    }


    /// <summary>Configures a dependency-injection compensation activity host.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The bus registration context used to resolve scoped activity instances.</param>
    /// <param name="configure">The optional callback that configures the activity compensation pipeline.</param>
    public static void CompensateActivityHost<TActivity, TLog>(this IReceiveEndpointConfigurator configurator, IRegistrationContext context,
        Action<ICompensateActivityConfigurator<TActivity, TLog>>? configure = null)
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(context);

        var compensateActivityScopeProvider = new CompensateActivityScopeProvider<TActivity, TLog>(context);

        var factory = new ScopeCompensateActivityFactory<TActivity, TLog>(compensateActivityScopeProvider);

        configurator.CompensateActivityHost(factory, configure);
    }
}
