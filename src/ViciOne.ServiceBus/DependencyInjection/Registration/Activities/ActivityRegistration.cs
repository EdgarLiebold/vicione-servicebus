using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Registers activity services.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
public class ActivityRegistration<TActivity, TArguments, TLog> :
    IActivityRegistration
    where TActivity : class, IActivity<TArguments, TLog>
    where TArguments : class
    where TLog : class
{
    readonly List<Action<IRegistrationContext, ICompensateActivityConfigurator<TActivity, TLog>>> _compensateActions;
    readonly List<Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>>> _executeActions;
    readonly IContainerSelector _selector;
    IActivityDefinition<TActivity, TArguments, TLog> _definition = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="selector">The selector.</param>
    public ActivityRegistration(IContainerSelector selector)
    {
        _selector = selector;
        _executeActions = new List<Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>>>();
        _compensateActions = new List<Action<IRegistrationContext, ICompensateActivityConfigurator<TActivity, TLog>>>();
        IncludeInConfigureEndpoints = !Type.HasAttribute<ExcludeFromConfigureEndpointsAttribute>();
    }

    /// <summary>Gets the type.</summary>
    public Type Type => typeof(TActivity);

    /// <summary>Gets or sets the include in configure endpoints.</summary>
    public bool IncludeInConfigureEndpoints { get; set; }

    /// <summary>Adds configure action to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TA">The a type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    public void AddConfigureAction<T, TA>(Action<IRegistrationContext, IExecuteActivityConfigurator<T, TA>>? configure)
        where T : class, IExecuteActivity<TA>
        where TA : class
    {
        if (configure is Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>> action)
            _executeActions.Add(action);
    }

    /// <summary>Adds configure action to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TL">The l type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    public void AddConfigureAction<T, TL>(Action<IRegistrationContext, ICompensateActivityConfigurator<T, TL>>? configure)
        where T : class, ICompensateActivity<TL>
        where TL : class
    {
        if (configure is Action<IRegistrationContext, ICompensateActivityConfigurator<TActivity, TLog>> action)
            _compensateActions.Add(action);
    }

    /// <summary>Applies the supplied configuration.</summary>
    /// <param name="executeEndpointConfigurator">The execute endpoint configurator.</param>
    /// <param name="compensateEndpointConfigurator">The compensate endpoint configurator.</param>
    /// <param name="context">The context associated with the operation.</param>
    public void Configure(IReceiveEndpointConfigurator executeEndpointConfigurator, IReceiveEndpointConfigurator compensateEndpointConfigurator,
        IRegistrationContext context)
    {
        ConfigureCompensate(compensateEndpointConfigurator, context);

        ConfigureExecute(executeEndpointConfigurator, context, compensateEndpointConfigurator.InputAddress);
    }

    IActivityDefinition IActivityRegistration.GetDefinition(IRegistrationContext context)
    {
        return GetActivityDefinition(context);
    }

    /// <summary>Configures compensate.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    public void ConfigureCompensate(IReceiveEndpointConfigurator configurator, IRegistrationContext context)
    {
        var activityScopeProvider = new CompensateActivityScopeProvider<TActivity, TLog>(context);

        var activityFactory = new ScopeCompensateActivityFactory<TActivity, TLog>(activityScopeProvider);

        var specification = new CompensateActivityHostConfigurator<TActivity, TLog>(activityFactory, configurator);

        configurator.ConfigureConsumeTopology = false;

        GetActivityDefinition(context)
            .Configure(configurator, specification, context);

        foreach (Action<IRegistrationContext, ICompensateActivityConfigurator<TActivity, TLog>> action in _compensateActions)
            action(context, specification);

        LogContext.Info?.Log("Configured endpoint {Endpoint}, Compensate Activity: {ActivityType}", configurator.InputAddress.GetEndpointName(),
            TypeCache<TActivity>.ShortName);

        configurator.AddEndpointSpecification(specification);
    }

    /// <summary>Configures execute.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="compensateAddress">The compensate address.</param>
    public void ConfigureExecute(IReceiveEndpointConfigurator configurator, IRegistrationContext context,
        Uri compensateAddress)
    {
        var activityScopeProvider = new ExecuteActivityScopeProvider<TActivity, TArguments>(context);

        var activityFactory = new ScopeExecuteActivityFactory<TActivity, TArguments>(activityScopeProvider);

        var specification = new ExecuteActivityHostConfigurator<TActivity, TArguments>(activityFactory, compensateAddress, configurator);

        configurator.ConfigureConsumeTopology = false;

        GetActivityDefinition(context)
            .Configure(configurator, specification, context);

        foreach (Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>> action in _executeActions)
            action(context, specification);

        LogContext.Info?.Log("Configured endpoint {Endpoint}, Execute Activity: {ActivityType}", configurator.InputAddress.GetEndpointName(),
            TypeCache<TActivity>.ShortName);

        configurator.AddEndpointSpecification(specification);

        IncludeInConfigureEndpoints = false;
    }

    IActivityDefinition<TActivity, TArguments, TLog> GetActivityDefinition(IServiceProvider provider)
    {
        if (_definition != null)
            return _definition;

        _definition = _selector.GetDefinition<IActivityDefinition<TActivity, TArguments, TLog>>(provider)
            ?? new DefaultActivityDefinition<TActivity, TArguments, TLog>();

        IEndpointDefinition<IExecuteActivity<TArguments>>? executeEndpointDefinition =
            _selector.GetEndpointDefinition<IExecuteActivity<TArguments>>(provider);
        if (executeEndpointDefinition != null)
            _definition.ExecuteEndpointDefinition = executeEndpointDefinition;

        IEndpointDefinition<ICompensateActivity<TLog>>? compensateEndpointDefinition = _selector.GetEndpointDefinition<ICompensateActivity<TLog>>(provider);
        if (compensateEndpointDefinition != null)
            _definition.CompensateEndpointDefinition = compensateEndpointDefinition;

        return _definition;
    }
}
