using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides dependency-injection receive-endpoint extensions for Courier activities.</summary>
public static class DependencyInjectionCourierReceiveEndpointExtensions
{
    /// <summary>Executes activity host.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="compensateAddress">The compensate address.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void ExecuteActivityHost<TActivity, TArguments>(this IReceiveEndpointConfigurator configurator, Uri compensateAddress,
        IRegistrationContext context, Action<IExecuteActivityConfigurator<TActivity, TArguments>>? configure = null)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        var executeActivityScopeProvider = new ExecuteActivityScopeProvider<TActivity, TArguments>(context);

        var factory = new ScopeExecuteActivityFactory<TActivity, TArguments>(executeActivityScopeProvider);

        configurator.ExecuteActivityHost(compensateAddress, factory, configure);
    }


    /// <summary>Executes activity host.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void ExecuteActivityHost<TActivity, TArguments>(this IReceiveEndpointConfigurator configurator, IRegistrationContext context,
        Action<IExecuteActivityConfigurator<TActivity, TArguments>>? configure = null)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        var executeActivityScopeProvider = new ExecuteActivityScopeProvider<TActivity, TArguments>(context);

        var factory = new ScopeExecuteActivityFactory<TActivity, TArguments>(executeActivityScopeProvider);

        configurator.ExecuteActivityHost(factory, configure);
    }


    /// <summary>Compensates activity host.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="configure">The callback used to configure the component.</param>
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
