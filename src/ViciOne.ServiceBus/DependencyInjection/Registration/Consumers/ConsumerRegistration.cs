using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>
/// A consumer registration represents a single consumer, which will be resolved from the container using the scope
/// provider. The consumer definition, if present, is loaded from the container and used to configure the consumer
/// within the receive endpoint.
/// </summary>
/// <typeparam name="TConsumer">The consumer type.</typeparam>
public class ConsumerRegistration<TConsumer> :
    IConsumerRegistration
    where TConsumer : class, IConsumer
{
    readonly List<Action<IRegistrationContext, IConsumerConfigurator<TConsumer>>> _configureActions;
    readonly object _definitionLock = new();
    readonly IContainerSelector _selector;
    readonly ConditionalWeakTable<IServiceProvider, IConsumerDefinition<TConsumer>> _definitions = new();

    /// <summary>Initializes a new instance.</summary>
    /// <param name="selector">The selector.</param>
    public ConsumerRegistration(IContainerSelector selector)
    {
        _selector = selector;
        _configureActions = new List<Action<IRegistrationContext, IConsumerConfigurator<TConsumer>>>();
        IncludeInConfigureEndpoints = !Type.HasAttribute<ExcludeFromConfigureEndpointsAttribute>();
    }

    /// <summary>Gets the type.</summary>
    public Type Type => typeof(TConsumer);

    /// <summary>Gets or sets the include in configure endpoints.</summary>
    public bool IncludeInConfigureEndpoints { get; set; }

    /// <summary>Gets or sets a value indicating whether the consumer endpoint must be hosted by a service instance.</summary>
    public bool RequiresServiceInstance { get; set; }

    void IConsumerRegistration.AddConfigureAction<T>(Action<IRegistrationContext, IConsumerConfigurator<T>>? configure)
    {
        if (configure is Action<IRegistrationContext, IConsumerConfigurator<TConsumer>> action)
            _configureActions.Add(action);
    }

    void IConsumerRegistration.Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context)
        => Configure(configurator, context, null);

    internal void Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context,
        Action<IConsumerConfigurator<TConsumer>>? configure)
    {
        IConsumeScopeProvider scopeProvider = new ConsumeScopeProvider(context);
        IConsumerFactory<TConsumer> consumerFactory = new ScopeConsumerFactory<TConsumer>(scopeProvider);

        var decoratorRegistration = context.GetService<IConsumerFactoryDecoratorRegistration<TConsumer>>();
        if (decoratorRegistration != null)
            consumerFactory = decoratorRegistration.DecorateConsumerFactory(consumerFactory);

        var consumerConfigurator = new ConsumerConfigurator<TConsumer>(consumerFactory, configurator);
        consumerConfigurator.Options(new ConsumerBusIdentityOptions(
            context is IBusRegistrationIdentity identity ? identity.BusKey : "unknown"));

        GetConsumerDefinition(context)
            .Configure(configurator, consumerConfigurator, context);

        foreach (Action<IRegistrationContext, IConsumerConfigurator<TConsumer>> action in _configureActions)
            action(context, consumerConfigurator);

        configure?.Invoke(consumerConfigurator);

        var endpointName = configurator.InputAddress.GetEndpointName();

        foreach (var configureReceiveEndpoint in consumerConfigurator.SelectOptions<IConfigureReceiveEndpoint>())
            configureReceiveEndpoint.Configure(endpointName, configurator);

        LogContext.Info?.Log("Configured endpoint {Endpoint}, Consumer: {ConsumerType}", endpointName, TypeCache<TConsumer>.ShortName);

        configurator.AddEndpointSpecification(consumerConfigurator);
    }

    IConsumerDefinition IConsumerRegistration.GetDefinition(IRegistrationContext context)
    {
        return GetConsumerDefinition(context);
    }

    /// <summary>Gets consumer registration configurator.</summary>
    /// <param name="registrationConfigurator">The registration configurator.</param>
    /// <returns>The consumer registration configurator.</returns>
    public IConsumerRegistrationConfigurator GetConsumerRegistrationConfigurator(IRegistrationConfigurator registrationConfigurator)
    {
        return new ConsumerRegistrationConfigurator<TConsumer>(registrationConfigurator, this);
    }

    IConsumerDefinition<TConsumer> GetConsumerDefinition(IServiceProvider provider)
    {
        lock (_definitionLock)
        {
            if (_definitions.TryGetValue(provider, out IConsumerDefinition<TConsumer>? definition))
                return definition;

            definition = _selector.GetDefinition<IConsumerDefinition<TConsumer>>(provider) ?? new DefaultConsumerDefinition<TConsumer>();

            IEndpointDefinition<TConsumer>? endpointDefinition = _selector.GetEndpointDefinition<TConsumer>(provider);
            if (endpointDefinition != null)
                definition.EndpointDefinition = endpointDefinition;

            _definitions.Add(provider, definition);
            return definition;
        }
    }
}
