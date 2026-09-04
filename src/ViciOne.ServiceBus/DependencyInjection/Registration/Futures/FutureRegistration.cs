using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>
/// Provides a future registration implementation.
/// </summary>
/// <typeparam name="TFuture">The t future type.</typeparam>
public class FutureRegistration<TFuture> :
    IFutureRegistration
    where TFuture : class, SagaStateMachine<FutureState>
{
    readonly IContainerSelector _selector;
    IFutureDefinition<TFuture> _definition = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="selector">The selector value.</param>
    public FutureRegistration(IContainerSelector selector)
    {
        _selector = selector;
        IncludeInConfigureEndpoints = !Type.HasAttribute<ExcludeFromConfigureEndpointsAttribute>();
    }

    /// <summary>
    /// Gets the type value.
    /// </summary>
    public Type Type => typeof(TFuture);

    /// <summary>
    /// Gets or sets the include in configure endpoints value.
    /// </summary>
    public bool IncludeInConfigureEndpoints { get; set; }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="context">The operation context.</param>
    public void Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context)
    {
        var stateMachine = context.GetRequiredService<TFuture>();
        ISagaRepository<FutureState> repository = new DependencyInjectionSagaRepository<FutureState>(context);

        var decoratorRegistration = context.GetService<ISagaRepositoryDecoratorRegistration<FutureState>>();
        if (decoratorRegistration != null)
            repository = decoratorRegistration.DecorateSagaRepository(repository);

        var sagaConfigurator = new ViciOneServiceBusStateMachine<FutureState>.StateMachineSagaConfigurator(stateMachine, repository, configurator);

        GetFutureDefinition(context)
            .Configure(configurator, sagaConfigurator, context);

        LogContext.Info?.Log("Configured endpoint {Endpoint}, Future: {FutureType}",
            configurator.InputAddress.GetEndpointName(), TypeCache<TFuture>.ShortName);

        configurator.AddEndpointSpecification(sagaConfigurator);
    }

    /// <summary>
    /// Gets definition.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public IFutureDefinition GetDefinition(IRegistrationContext context)
    {
        return GetFutureDefinition(context);
    }

    IFutureDefinition<TFuture> GetFutureDefinition(IServiceProvider provider)
    {
        if (_definition != null)
            return _definition;

        _definition = _selector.GetDefinition<IFutureDefinition<TFuture>>(provider) ?? new DefaultFutureDefinition<TFuture>();

        IEndpointDefinition<TFuture>? endpointDefinition = _selector.GetEndpointDefinition<TFuture>(provider);
        if (endpointDefinition != null)
            _definition.EndpointDefinition = endpointDefinition;

        return _definition;
    }
}
