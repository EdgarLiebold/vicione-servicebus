using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Stores and applies dependency-injection registration for an execution-only activity.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
internal sealed class ExecuteActivityRegistration<TActivity, TArguments> :
    IExecuteActivityRegistration
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly List<Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>>> _configureActions;
    readonly Lock _definitionLock = new();
    readonly IContainerSelector _selector;
    IExecuteActivityDefinition<TActivity, TArguments> _definition = null!;

    /// <summary>Creates registration metadata for an execute-only activity.</summary>
    /// <param name="selector">The container integration used to resolve definitions and endpoint settings.</param>
    public ExecuteActivityRegistration(IContainerSelector selector)
    {
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        _configureActions = [];
        IncludeInConfigureEndpoints = !Type.HasAttribute<ExcludeFromConfigureEndpointsAttribute>();
    }

    /// <summary>Gets the registered activity type.</summary>
    public Type Type => typeof(TActivity);

    /// <summary>Gets or sets whether automatic endpoint configuration includes this activity.</summary>
    public bool IncludeInConfigureEndpoints { get; set; }

    void IExecuteActivityRegistration.AddConfigureAction<T, TArgs>(Action<IRegistrationContext, IExecuteActivityConfigurator<T, TArgs>>? configure)
    {
        if (configure is Action<IRegistrationContext, IExecuteActivityConfigurator<TActivity, TArguments>> action)
            _configureActions.Add(action);
    }

    /// <summary>Configures the activity execution endpoint.</summary>
    /// <param name="configurator">The endpoint that receives activity executions.</param>
    /// <param name="context">The registration context used to resolve definitions and scoped services.</param>
    public void Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(context);

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
        ArgumentNullException.ThrowIfNull(context);

        return GetActivityDefinition(context);
    }

    IExecuteActivityDefinition<TActivity, TArguments> GetActivityDefinition(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        lock (_definitionLock)
        {
            if (_definition != null)
                return _definition;

            IExecuteActivityDefinition<TActivity, TArguments> definition =
                _selector.GetDefinition<IExecuteActivityDefinition<TActivity, TArguments>>(provider)
                ?? new DefaultExecuteActivityDefinition<TActivity, TArguments>();

            IEndpointDefinition<IExecuteActivity<TArguments>>? executeEndpointDefinition =
                _selector.GetEndpointDefinition<IExecuteActivity<TArguments>>(provider);
            if (executeEndpointDefinition != null)
                definition.ExecuteEndpointDefinition = executeEndpointDefinition;

            _definition = definition;
            return definition;
        }
    }
}
