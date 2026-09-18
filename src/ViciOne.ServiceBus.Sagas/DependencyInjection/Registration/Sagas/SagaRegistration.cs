using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>
/// A saga registration represents a single saga, which will use the container for the scope provider, as well as
/// to resolve the saga repository.
/// </summary>
/// <typeparam name="TSaga">The saga type.</typeparam>
public class SagaRegistration<TSaga> :
    ISagaRegistration
    where TSaga : class, ISaga
{
    readonly List<Action<IRegistrationContext, ISagaConfigurator<TSaga>>> _configureActions;
    readonly object _definitionLock = new();
    readonly IContainerSelector _selector;
    volatile ISagaDefinition<TSaga>? _definition;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="selector">The selector.</param>
    public SagaRegistration(IContainerSelector selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        _selector = selector;
        _configureActions = new List<Action<IRegistrationContext, ISagaConfigurator<TSaga>>>();
        IncludeInConfigureEndpoints = !Type.HasAttribute<ExcludeFromConfigureEndpointsAttribute>();
    }

    /// <summary>Gets the type.</summary>
    public Type Type => typeof(TSaga);

    /// <inheritdoc />
    public Type? StateMachineType => null;

    /// <summary>Gets or sets the include in configure endpoints.</summary>
    public bool IncludeInConfigureEndpoints { get; set; }

    void ISagaRegistration.AddConfigureAction<T>(Action<IRegistrationContext, ISagaConfigurator<T>>? configure)
    {
        if (configure is Action<IRegistrationContext, ISagaConfigurator<TSaga>> action)
            _configureActions.Add(action);
    }

    void ISagaRegistration.Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(context);

        ISagaRepository<TSaga> repository = new DependencyInjectionSagaRepository<TSaga>(context);

        var decoratorRegistration = context.GetService<ISagaRepositoryDecoratorRegistration<TSaga>>();
        if (decoratorRegistration != null)
            repository = decoratorRegistration.DecorateSagaRepository(repository);

        var sagaConfigurator = new SagaConfigurator<TSaga>(repository, configurator);

        GetSagaDefinition(context)
            .Configure(configurator, sagaConfigurator, context);

        foreach (Action<IRegistrationContext, ISagaConfigurator<TSaga>> action in _configureActions)
            action(context, sagaConfigurator);

        LogContext.Info?.Log("Configured endpoint {Endpoint}, Saga: {SagaType}", configurator.InputAddress.GetEndpointName(),
            TypeCache<TSaga>.ShortName);

        configurator.AddEndpointSpecification(sagaConfigurator);
    }

    ISagaDefinition ISagaRegistration.GetDefinition(IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return GetSagaDefinition(context);
    }

    ISagaDefinition<TSaga> GetSagaDefinition(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        if (_definition != null)
            return _definition;

        lock (_definitionLock)
        {
            if (_definition != null)
                return _definition;

            ISagaDefinition<TSaga> definition =
                _selector.GetDefinition<ISagaDefinition<TSaga>>(provider) ?? new DefaultSagaDefinition<TSaga>();

            IEndpointDefinition<TSaga>? endpointDefinition = _selector.GetEndpointDefinition<TSaga>(provider);
            if (endpointDefinition != null)
                definition.EndpointDefinition = endpointDefinition;

            _definition = definition;
            return definition;
        }
    }
}
