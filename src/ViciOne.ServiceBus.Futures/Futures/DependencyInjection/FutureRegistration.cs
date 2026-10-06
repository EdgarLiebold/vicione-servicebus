using System;
using System.Runtime.CompilerServices;
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
    static readonly ConditionalWeakTable<IServiceProvider,
        ConditionalWeakTable<TFuture, FutureRegistration<TFuture>>> StateMachineOwners = new();
    readonly object _definitionLock = new();
    readonly IContainerSelector _selector;
    readonly ConditionalWeakTable<IServiceProvider, IFutureDefinition<TFuture>> _definitions = new();
    readonly ConditionalWeakTable<IFutureDefinition<TFuture>, TFuture> _stateMachines = new();

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
        if (typeof(TFuture).IsGenericType
            && typeof(TFuture).GetGenericTypeDefinition() == typeof(RequestConsumerFuture<,>)
            && StateMachineOwners.TryGetValue(GetProviderIdentity(context), out var owners)
            && owners.TryGetValue(stateMachine, out var owner)
            && !ReferenceEquals(owner, this))
            stateMachine = GetStateMachine(context, GetFutureDefinition(context));
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

    // Only exact built-in companion futures use this cache. Their definition is
    // container-owned; the machine itself has no disposable resource contract.
    internal TFuture GetStateMachine(IServiceProvider provider, IFutureDefinition<TFuture> definition)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(definition);

        lock (_definitionLock)
        {
            if (!_stateMachines.TryGetValue(definition, out TFuture? stateMachine))
            {
                stateMachine = ActivatorUtilities.CreateInstance<TFuture>(provider, definition);
                _stateMachines.Add(definition, stateMachine);
            }

            StateMachineOwners.GetValue(GetProviderIdentity(provider),
                _ => new ConditionalWeakTable<TFuture, FutureRegistration<TFuture>>())
                .GetValue(stateMachine, _ => this);
            return stateMachine;
        }
    }

    // Registration contexts delegate self-service lookup to their underlying DI
    // provider, so explicit machines borrowed into another provider stay unmarked.
    static IServiceProvider GetProviderIdentity(IServiceProvider provider) =>
        provider.GetService(typeof(IServiceProvider)) as IServiceProvider ?? provider;

    IFutureDefinition<TFuture> GetFutureDefinition(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        lock (_definitionLock)
        {
            if (_definitions.TryGetValue(provider, out IFutureDefinition<TFuture>? definition))
                return definition;

            definition = _selector.GetDefinition<IFutureDefinition<TFuture>>(provider) ?? new DefaultFutureDefinition<TFuture>();

            IEndpointDefinition<TFuture>? endpointDefinition = _selector.GetEndpointDefinition<TFuture>(provider);
            if (endpointDefinition != null)
                definition.EndpointDefinition = endpointDefinition;

            _definitions.Add(provider, definition);
            return definition;
        }
    }
}
