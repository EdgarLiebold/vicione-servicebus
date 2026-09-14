using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Courier;

sealed class ActivityConsumerKind :
    IConsumerKind,
    IConsumerKindCompanionConfigurator,
    IConsumerKindDispatcherProvider
{
    public bool IsFallback => false;

    public string Name => "Activity";

    public int Order => 20;

    public IEnumerable<IConsumerKindRegistration> GetRegistrations(IConsumerKindContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var registration in context.GetRegistrations<IActivityRegistration>())
        {
            var definition = registration.GetDefinition(context.RegistrationContext);
            yield return new Registration(registration, definition, context.EndpointNameFormatter);
        }
    }

    public bool TryConfigurePair(Type registrationType, IReceiveEndpointConfigurator primaryEndpointConfigurator,
        IReceiveEndpointConfigurator companionEndpointConfigurator, IRegistrationContext registrationContext)
    {
        ArgumentNullException.ThrowIfNull(registrationType);
        ArgumentNullException.ThrowIfNull(primaryEndpointConfigurator);
        ArgumentNullException.ThrowIfNull(companionEndpointConfigurator);
        ArgumentNullException.ThrowIfNull(registrationContext);

        if (!TryGetRegistration(registrationType, registrationContext, out IActivityRegistration? registration))
            return false;

        registration.Configure(primaryEndpointConfigurator, companionEndpointConfigurator, registrationContext);
        return true;
    }

    public bool TryConfigurePrimary(Type registrationType, IReceiveEndpointConfigurator primaryEndpointConfigurator,
        Uri companionAddress, IRegistrationContext registrationContext)
    {
        ArgumentNullException.ThrowIfNull(registrationType);
        ArgumentNullException.ThrowIfNull(primaryEndpointConfigurator);
        ArgumentNullException.ThrowIfNull(companionAddress);
        ArgumentNullException.ThrowIfNull(registrationContext);

        if (!TryGetRegistration(registrationType, registrationContext, out IActivityRegistration? registration))
            return false;

        registration.ConfigureExecute(primaryEndpointConfigurator, registrationContext, companionAddress);
        return true;
    }

    public bool TryConfigureCompanion(Type registrationType, IReceiveEndpointConfigurator companionEndpointConfigurator,
        IRegistrationContext registrationContext)
    {
        ArgumentNullException.ThrowIfNull(registrationType);
        ArgumentNullException.ThrowIfNull(companionEndpointConfigurator);
        ArgumentNullException.ThrowIfNull(registrationContext);

        if (!TryGetRegistration(registrationType, registrationContext, out IActivityRegistration? registration))
            return false;

        registration.ConfigureCompensate(companionEndpointConfigurator, registrationContext);
        return true;
    }

    public bool TryCreateDispatcher(Type registrationType, IReceiveEndpointDispatcherFactory factory,
        IEndpointNameFormatter formatter, [NotNullWhen(true)] out IReceiveEndpointDispatcher? dispatcher)
    {
        ArgumentNullException.ThrowIfNull(registrationType);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(formatter);

        if (!registrationType.TryGetSingleClosedGenericArguments(typeof(IActivity<,>), out Type[] arguments))
        {
            dispatcher = null;
            return false;
        }

        var dispatcherFactory = (ITypeReceiveEndpointDispatcherFactory)(Activator.CreateInstance(
            typeof(ExecuteActivityReceiveEndpointDispatcher<,>).MakeGenericType(registrationType, arguments[0]))
            ?? throw new InvalidOperationException($"Unable to create an activity dispatcher for '{registrationType}'."));
        dispatcher = dispatcherFactory.Create(factory, formatter);
        return true;
    }

    public void ConfigureTestHarness(IConsumerKindTestHarnessContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (IActivityRegistration registration in context.GetRegistrations<IActivityRegistration>())
            context.Observe(registration.Type);
    }

    static bool TryGetRegistration(Type registrationType, IRegistrationContext registrationContext,
        [NotNullWhen(true)] out IActivityRegistration? registration)
    {
        IContainerSelector selector = registrationContext.GetRequiredService<IContainerSelector>();
        return selector.TryGetRegistration(registrationContext, registrationType, out registration);
    }


    sealed class Registration :
        IConsumerKindRegistration
    {
        readonly IActivityDefinition _definition;
        readonly IActivityRegistration _registration;
        readonly string _compensateEndpointName;

        public Registration(IActivityRegistration registration, IActivityDefinition definition, IEndpointNameFormatter endpointNameFormatter)
        {
            ArgumentNullException.ThrowIfNull(registration);
            ArgumentNullException.ThrowIfNull(definition);
            ArgumentNullException.ThrowIfNull(endpointNameFormatter);

            _registration = registration;
            _definition = definition;
            EndpointName = definition.GetExecuteEndpointName(endpointNameFormatter);
            _compensateEndpointName = definition.GetCompensateEndpointName(endpointNameFormatter);
            CompanionEndpointNames = [_compensateEndpointName];
        }

        public Type RegistrationType => _registration.Type;
        public IDefinition Definition => _definition;
        public string EndpointName { get; }
        public IEndpointDefinition? EndpointDefinition => _definition.ExecuteEndpointDefinition;
        public bool RequiresServiceInstance => false;
        public IReadOnlyCollection<string> CompanionEndpointNames { get; }

        public void Configure(IConsumerKindEndpointContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            context.ConfigureCompanionEndpoint(_compensateEndpointName, _definition.CompensateEndpointDefinition,
                compensate => _registration.Configure(context.EndpointConfigurator, compensate, context.RegistrationContext));
        }
    }
}
