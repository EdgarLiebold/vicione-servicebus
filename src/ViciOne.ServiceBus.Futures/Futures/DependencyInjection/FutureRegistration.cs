using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Futures.DependencyInjection;

/// <summary>Configures one registered future on its receive endpoint.</summary>
/// <typeparam name="TFuture">The future state-machine type.</typeparam>
internal sealed class FutureRegistration<TFuture> :
    IFutureRegistration
    where TFuture : class, ISagaStateMachine<FutureState>
{
    readonly IContainerSelector _selector;
    IFutureDefinition<TFuture>? _definition;

    /// <summary>Creates a registration that resolves future definitions from the selected container.</summary>
    /// <param name="selector">The container selector used to resolve definitions and endpoint settings.</param>
    public FutureRegistration(IContainerSelector selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        _selector = selector;
        IncludeInConfigureEndpoints = !Type.HasAttribute<ExcludeFromConfigureEndpointsAttribute>();
    }

    /// <summary>Gets the registered future state-machine type.</summary>
    public Type Type => typeof(TFuture);

    /// <summary>Gets or sets whether convention-based endpoint configuration includes this future.</summary>
    public bool IncludeInConfigureEndpoints { get; set; }

    /// <summary>Connects the resolved future and its repository to a receive endpoint.</summary>
    /// <param name="configurator">The receive endpoint that hosts the future.</param>
    /// <param name="context">The registration context that resolves the state machine and repository.</param>
    public void Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(context);
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

    /// <summary>Gets the resolved definition for this future registration.</summary>
    /// <param name="context">The registration context used to resolve the definition.</param>
    /// <returns>The explicit definition, or the default future definition.</returns>
    public IFutureDefinition GetDefinition(IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return GetFutureDefinition(context);
    }

    IFutureDefinition<TFuture> GetFutureDefinition(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        if (_definition != null)
            return _definition;

        _definition = _selector.GetDefinition<IFutureDefinition<TFuture>>(provider) ?? new DefaultFutureDefinition<TFuture>();

        IEndpointDefinition<TFuture>? endpointDefinition = _selector.GetEndpointDefinition<TFuture>(provider);
        if (endpointDefinition != null)
            _definition.EndpointDefinition = endpointDefinition;

        return _definition;
    }
}
