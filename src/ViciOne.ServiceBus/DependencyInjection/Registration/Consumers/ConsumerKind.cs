using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

sealed class ConsumerKind :
    IConsumerKind,
    IConsumerKindRuntimeConfigurator,
    IConsumerKindBulkConfigurator,
    IConsumerKindDispatcherProvider
{
    public bool IsFallback => true;

    public string Name => "Consumer";

    public int Order => 0;

    public IEnumerable<IConsumerKindRegistration> GetRegistrations(IConsumerKindContext context)
    {
        foreach (var registration in context.GetRegistrations<IConsumerRegistration>())
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
        if (!selector.TryGetRegistration(registrationContext, registrationType, out IConsumerRegistration? registration))
            return false;

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
        IConsumerRegistration[] registrations = selector.GetRegistrations<IConsumerRegistration>(registrationContext)
            .Where(registration => !excludedRegistrationTypes.Contains(registration.Type))
            .ToArray();

        foreach (IConsumerRegistration registration in registrations)
            registration.Configure(endpointConfigurator, registrationContext);

        return registrations.Select(registration => registration.Type).ToArray();
    }

    public bool TryCreateDispatcher(Type registrationType, IReceiveEndpointDispatcherFactory factory,
        IEndpointNameFormatter formatter, [NotNullWhen(true)] out IReceiveEndpointDispatcher? dispatcher)
    {
        if (!registrationType.ImplementsInterface<IConsumer>())
        {
            dispatcher = null;
            return false;
        }

        var dispatcherFactory = (ITypeReceiveEndpointDispatcherFactory)(Activator.CreateInstance(
            typeof(ConsumerReceiveEndpointDispatcher<>).MakeGenericType(registrationType))
            ?? throw new InvalidOperationException($"Unable to create a consumer dispatcher for '{registrationType}'."));
        dispatcher = dispatcherFactory.Create(factory, formatter);
        return true;
    }

    public void ConfigureTestHarness(IConsumerKindTestHarnessContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (IConsumerRegistration registration in context.GetRegistrations<IConsumerRegistration>())
            context.Observe(registration.Type);
    }


    sealed class Registration :
        IConsumerKindRegistration
    {
        static readonly IReadOnlyCollection<string> NoCompanionEndpoints = Array.Empty<string>();
        readonly IConsumerRegistration _registration;

        public Registration(IConsumerRegistration registration, IConsumerDefinition definition, string endpointName)
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
        public bool RequiresServiceInstance => _registration.RequiresServiceInstance;
        public IReadOnlyCollection<string> CompanionEndpointNames => NoCompanionEndpoints;

        public void Configure(IConsumerKindEndpointContext context)
        {
            _registration.Configure(context.EndpointConfigurator, context.RegistrationContext);
        }
    }
}
