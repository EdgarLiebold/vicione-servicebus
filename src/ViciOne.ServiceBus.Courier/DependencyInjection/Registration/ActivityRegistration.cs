using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Stores and applies dependency-injection registration for a compensatable activity.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
internal sealed class ActivityRegistration<TActivity, TArguments, TLog> :
    IActivityRegistration
    where TActivity : class, IActivity<TArguments, TLog>
    where TArguments : class
    where TLog : class
{
    readonly List<Action<IRegistrationContext, ICompensateActivityConfigurator<TActivity, TLog>>> _compensateActions;
    readonly List<Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>>> _executeActions;
    readonly IContainerSelector _selector;
    IActivityDefinition<TActivity, TArguments, TLog> _definition = null!;

    /// <summary>Creates registration metadata for a compensatable activity.</summary>
    /// <param name="selector">The container integration used to resolve definitions and endpoint settings.</param>
    public ActivityRegistration(IContainerSelector selector)
    {
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        _executeActions = [];
        _compensateActions = [];
        IncludeInConfigureEndpoints = !Type.HasAttribute<ExcludeFromConfigureEndpointsAttribute>();
    }

    /// <summary>Gets the registered activity type.</summary>
    public Type Type => typeof(TActivity);

    /// <summary>Gets or sets whether automatic endpoint configuration includes this activity.</summary>
    public bool IncludeInConfigureEndpoints { get; set; }

    /// <summary>Adds an execution-pipeline configuration action when its activity signature matches this registration.</summary>
    /// <typeparam name="T">The activity type supplied by the caller.</typeparam>
    /// <typeparam name="TA">The activity argument type supplied by the caller.</typeparam>
    /// <param name="configure">The execution-pipeline configuration action.</param>
    public void AddConfigureAction<T, TA>(Action<IRegistrationContext, IExecuteActivityConfigurator<T, TA>>? configure)
        where T : class, IExecuteActivity<TA>
        where TA : class
    {
        if (configure is Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>> action)
            _executeActions.Add(action);
    }

    /// <summary>Adds a compensation-pipeline configuration action when its activity signature matches this registration.</summary>
    /// <typeparam name="T">The activity type supplied by the caller.</typeparam>
    /// <typeparam name="TL">The activity log type supplied by the caller.</typeparam>
    /// <param name="configure">The compensation-pipeline configuration action.</param>
    public void AddConfigureAction<T, TL>(Action<IRegistrationContext, ICompensateActivityConfigurator<T, TL>>? configure)
        where T : class, ICompensateActivity<TL>
        where TL : class
    {
        if (configure is Action<IRegistrationContext, ICompensateActivityConfigurator<TActivity, TLog>> action)
            _compensateActions.Add(action);
    }

    /// <summary>Configures the paired execution and compensation endpoints.</summary>
    /// <param name="executeEndpointConfigurator">The endpoint that receives activity executions.</param>
    /// <param name="compensateEndpointConfigurator">The endpoint that receives activity compensations.</param>
    /// <param name="context">The registration context used to resolve definitions and scoped services.</param>
    public void Configure(IReceiveEndpointConfigurator executeEndpointConfigurator, IReceiveEndpointConfigurator compensateEndpointConfigurator,
        IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(executeEndpointConfigurator);
        ArgumentNullException.ThrowIfNull(compensateEndpointConfigurator);
        ArgumentNullException.ThrowIfNull(context);

        ConfigureCompensate(compensateEndpointConfigurator, context);

        ConfigureExecute(executeEndpointConfigurator, context, compensateEndpointConfigurator.InputAddress);
    }

    IActivityDefinition IActivityRegistration.GetDefinition(IRegistrationContext context)
    {
        return GetActivityDefinition(context);
    }

    /// <summary>Configures the compensation endpoint.</summary>
    /// <param name="configurator">The endpoint that receives activity compensations.</param>
    /// <param name="context">The registration context used to resolve definitions and scoped services.</param>
    public void ConfigureCompensate(IReceiveEndpointConfigurator configurator, IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(context);

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

    /// <summary>Configures the execution endpoint.</summary>
    /// <param name="configurator">The endpoint that receives activity executions.</param>
    /// <param name="context">The registration context used to resolve definitions and scoped services.</param>
    /// <param name="compensateAddress">The endpoint that compensates successful executions.</param>
    public void ConfigureExecute(IReceiveEndpointConfigurator configurator, IRegistrationContext context,
        Uri compensateAddress)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(compensateAddress);

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
