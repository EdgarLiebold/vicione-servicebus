using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Sagas;

sealed class SagaConsumerKind :
    IConsumerKind,
    IConsumerKindRuntimeConfigurator,
    IConsumerKindTypedConfigurator,
    IConsumerKindBulkConfigurator,
    IConsumerKindDispatcherProvider
{
    public bool IsFallback => false;

    public string Name => "Saga";

    public int Order => 10;

    public IEnumerable<IConsumerKindRegistration> GetRegistrations(IConsumerKindContext context)
    {
        foreach (var registration in context.GetRegistrations<ISagaRegistration>())
        {
            var definition = registration.GetDefinition(context.RegistrationContext);
            yield return new Registration(registration, definition,
                definition.GetEndpointName(context.EndpointNameFormatter));
        }
    }

    public bool TryConfigure(Type registrationType, IReceiveEndpointConfigurator endpointConfigurator,
        IRegistrationContext registrationContext)
    {
        ArgumentNullException.ThrowIfNull(registrationType);
        ArgumentNullException.ThrowIfNull(endpointConfigurator);
        ArgumentNullException.ThrowIfNull(registrationContext);

        IContainerSelector selector = registrationContext.GetRequiredService<IContainerSelector>();
        if (!selector.TryGetRegistration(registrationContext, registrationType, out ISagaRegistration? registration))
            return false;

        registration.Configure(endpointConfigurator, registrationContext);
        return true;
    }

    public bool TryConfigure<TRegistration>(IReceiveEndpointConfigurator endpointConfigurator,
        IRegistrationContext registrationContext, Delegate? configure = null)
        where TRegistration : class
    {
        ArgumentNullException.ThrowIfNull(endpointConfigurator);
        ArgumentNullException.ThrowIfNull(registrationContext);

        IContainerSelector selector = registrationContext.GetRequiredService<IContainerSelector>();
        if (!selector.TryGetRegistration(registrationContext, typeof(TRegistration), out ISagaRegistration? registration))
            return false;

        if (configure != null)
        {
            if (configure is not Action<ISagaConfigurator<TRegistration>> typedConfigure)
                throw new ArgumentException($"The saga configuration callback must target {TypeCache<ISagaConfigurator<TRegistration>>.ShortName}.", nameof(configure));

            registration.AddConfigureAction<TRegistration>((_, configurator) => typedConfigure(configurator));
        }

        registration.Configure(endpointConfigurator, registrationContext);
        return true;
    }

    public IReadOnlyCollection<Type> ConfigureAll(IReceiveEndpointConfigurator endpointConfigurator,
        IRegistrationContext registrationContext, IReadOnlySet<Type> excludedRegistrationTypes)
    {
        ArgumentNullException.ThrowIfNull(endpointConfigurator);
        ArgumentNullException.ThrowIfNull(registrationContext);
        ArgumentNullException.ThrowIfNull(excludedRegistrationTypes);

        IContainerSelector selector = registrationContext.GetRequiredService<IContainerSelector>();
        ISagaRegistration[] registrations = selector.GetRegistrations<ISagaRegistration>(registrationContext)
            .Where(registration => !excludedRegistrationTypes.Contains(registration.Type))
            .ToArray();

        foreach (ISagaRegistration registration in registrations)
            registration.Configure(endpointConfigurator, registrationContext);

        return registrations.Select(registration => registration.Type).ToArray();
    }

    public bool TryCreateDispatcher(Type registrationType, IReceiveEndpointDispatcherFactory factory,
        IEndpointNameFormatter formatter, [NotNullWhen(true)] out IReceiveEndpointDispatcher? dispatcher)
    {
        ArgumentNullException.ThrowIfNull(registrationType);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(formatter);

        if (!typeof(ISaga).IsAssignableFrom(registrationType))
        {
            dispatcher = null;
            return false;
        }

        var dispatcherFactory = (ITypeReceiveEndpointDispatcherFactory)(Activator.CreateInstance(
            typeof(SagaReceiveEndpointDispatcher<>).MakeGenericType(registrationType))
            ?? throw new InvalidOperationException($"Unable to create a saga dispatcher for '{registrationType}'."));
        dispatcher = dispatcherFactory.Create(factory, formatter);
        return true;
    }

    public void ConfigureTestHarness(IConsumerKindTestHarnessContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (ISagaRegistration registration in context.GetRegistrations<ISagaRegistration>())
        {
            if (registration.StateMachineType is { } stateMachineType)
                context.Observe(registration.Type, stateMachineType);
            else
                context.Observe(registration.Type);
        }
    }


    sealed class Registration :
        IConsumerKindRegistration
    {
        static readonly IReadOnlyCollection<string> NoCompanionEndpoints = Array.Empty<string>();
        readonly ISagaRegistration _registration;

        public Registration(ISagaRegistration registration, ISagaDefinition definition, string endpointName)
        {
            _registration = registration;
            Definition = definition;
            EndpointDefinition = definition.EndpointDefinition;
            EndpointName = endpointName;
        }

        public Type RegistrationType => _registration.Type;
        public IDefinition Definition { get; }
        public string EndpointName { get; }
        public IEndpointDefinition? EndpointDefinition { get; }
        public bool RequiresServiceInstance => false;
        public IReadOnlyCollection<string> CompanionEndpointNames => NoCompanionEndpoints;

        public void Configure(IConsumerKindEndpointContext context)
        {
            _registration.Configure(context.EndpointConfigurator, context.RegistrationContext);
        }
    }
}
