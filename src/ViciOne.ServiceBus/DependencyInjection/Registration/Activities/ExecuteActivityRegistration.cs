using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Registers execute activity services.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public class ExecuteActivityRegistration<TActivity, TArguments> :
    IExecuteActivityRegistration
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly List<Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>>> _configureActions;
    readonly IContainerSelector _selector;
    IExecuteActivityDefinition<TActivity, TArguments> _definition = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="selector">The selector.</param>
    public ExecuteActivityRegistration(IContainerSelector selector)
    {
        _selector = selector;
        _configureActions = new List<Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>>>();
        IncludeInConfigureEndpoints = !Type.HasAttribute<ExcludeFromConfigureEndpointsAttribute>();
    }

    /// <summary>Gets the type.</summary>
    public Type Type => typeof(TActivity);

    /// <summary>Gets or sets the include in configure endpoints.</summary>
    public bool IncludeInConfigureEndpoints { get; set; }

    void IExecuteActivityRegistration.AddConfigureAction<T, TArgs>(Action<IRegistrationContext, IExecuteActivityConfigurator<T, TArgs>>? configure)
    {
        if (configure is Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>> action)
            _configureActions.Add(action);
    }

    /// <summary>Applies the supplied configuration.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    public void Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context)
    {
        var executeActivityScopeProvider = new ExecuteActivityScopeProvider<TActivity, TArguments>(context);

        var executeActivityFactory = new ScopeExecuteActivityFactory<TActivity, TArguments>(executeActivityScopeProvider);

        var specification = new ExecuteActivityHostConfigurator<TActivity, TArguments>(executeActivityFactory, configurator);

        configurator.ConfigureConsumeTopology = false;

        GetActivityDefinition(context)
            .Configure(configurator, specification, context);

        foreach (Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>> action in _configureActions)
            action(context, specification);

        LogContext.Info?.Log("Configured endpoint {Endpoint}, Execute Activity: {ActivityType}", configurator.InputAddress.GetEndpointName(),
            TypeCache<TActivity>.ShortName);

        configurator.AddEndpointSpecification(specification);
    }

    IExecuteActivityDefinition IExecuteActivityRegistration.GetDefinition(IRegistrationContext context)
    {
        return GetActivityDefinition(context);
    }

    IExecuteActivityDefinition<TActivity, TArguments> GetActivityDefinition(IServiceProvider provider)
    {
        if (_definition != null)
            return _definition;

        _definition = _selector.GetDefinition<IExecuteActivityDefinition<TActivity, TArguments>>(provider)
            ?? new DefaultExecuteActivityDefinition<TActivity, TArguments>();

        IEndpointDefinition<IExecuteActivity<TArguments>>? executeEndpointDefinition =
            _selector.GetEndpointDefinition<IExecuteActivity<TArguments>>(provider);
        if (executeEndpointDefinition != null)
            _definition.ExecuteEndpointDefinition = executeEndpointDefinition;

        return _definition;
    }
}
