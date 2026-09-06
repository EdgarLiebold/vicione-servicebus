using System;
using System.Collections.Generic;
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
        foreach (var registration in context.GetRegistrations<IActivityRegistration>())
        {
            var definition = registration.GetDefinition(context.RegistrationContext);
            yield return new Registration(registration, definition, context.EndpointNameFormatter);
        }
    }

    public bool TryConfigurePair(Type registrationType, IReceiveEndpointConfigurator primaryEndpointConfigurator,
        IReceiveEndpointConfigurator companionEndpointConfigurator, IRegistrationContext registrationContext)
    {
        if (!TryGetRegistration(registrationType, registrationContext, out IActivityRegistration? registration))
            return false;

        registration.Configure(primaryEndpointConfigurator, companionEndpointConfigurator, registrationContext);
        return true;
    }

    public bool TryConfigurePrimary(Type registrationType, IReceiveEndpointConfigurator primaryEndpointConfigurator,
        Uri companionAddress, IRegistrationContext registrationContext)
    {
        if (!TryGetRegistration(registrationType, registrationContext, out IActivityRegistration? registration))
            return false;

        registration.ConfigureExecute(primaryEndpointConfigurator, registrationContext, companionAddress);
        return true;
    }

    public bool TryConfigureCompanion(Type registrationType, IReceiveEndpointConfigurator companionEndpointConfigurator,
        IRegistrationContext registrationContext)
    {
        if (!TryGetRegistration(registrationType, registrationContext, out IActivityRegistration? registration))
            return false;

        registration.ConfigureCompensate(companionEndpointConfigurator, registrationContext);
        return true;
    }

    public bool TryCreateDispatcher(Type registrationType, IReceiveEndpointDispatcherFactory factory,
        IEndpointNameFormatter formatter, out IReceiveEndpointDispatcher? dispatcher)
    {
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
            _registration = registration;
            _definition = definition;
            EndpointName = definition.GetExecuteEndpointName(endpointNameFormatter);
            _compensateEndpointName = definition.GetCompensateEndpointName(endpointNameFormatter);
            CompanionEndpointNames = new[] { _compensateEndpointName };
        }

        public Type RegistrationType => _registration.Type;
        public IDefinition Definition => _definition;
        public string EndpointName { get; }
        public IEndpointDefinition? EndpointDefinition => _definition.ExecuteEndpointDefinition;
        public bool RequiresServiceInstance => false;
        public IReadOnlyCollection<string> CompanionEndpointNames { get; }

        public void Configure(IConsumerKindEndpointContext context)
        {
            context.ConfigureCompanionEndpoint(_compensateEndpointName, _definition.CompensateEndpointDefinition,
                compensate => _registration.Configure(context.EndpointConfigurator, compensate, context.RegistrationContext));
        }
    }
}


sealed class ExecuteActivityConsumerKind :
    IConsumerKind,
    IConsumerKindRuntimeConfigurator,
    IConsumerKindDispatcherProvider
{
    public bool IsFallback => false;

    public string Name => "ExecuteActivity";

    public int Order => 30;

    public IEnumerable<IConsumerKindRegistration> GetRegistrations(IConsumerKindContext context)
    {
        foreach (var registration in context.GetRegistrations<IExecuteActivityRegistration>())
        {
            var definition = registration.GetDefinition(context.RegistrationContext);
            yield return new Registration(registration, definition,
                definition.GetExecuteEndpointName(context.EndpointNameFormatter));
        }
    }

    public bool TryConfigure(Type registrationType, IReceiveEndpointConfigurator endpointConfigurator,
        IRegistrationContext registrationContext)
    {
        IContainerSelector selector = registrationContext.GetRequiredService<IContainerSelector>();
        if (!selector.TryGetRegistration(registrationContext, registrationType, out IExecuteActivityRegistration? registration))
            return false;

        registration.Configure(endpointConfigurator, registrationContext);
        return true;
    }

    public bool TryCreateDispatcher(Type registrationType, IReceiveEndpointDispatcherFactory factory,
        IEndpointNameFormatter formatter, out IReceiveEndpointDispatcher? dispatcher)
    {
        if (!registrationType.TryGetSingleClosedGenericArguments(typeof(IExecuteActivity<>), out Type[] arguments)
            || registrationType.ImplementsInterface(typeof(IActivity<,>)))
        {
            dispatcher = null;
            return false;
        }

        var dispatcherFactory = (ITypeReceiveEndpointDispatcherFactory)(Activator.CreateInstance(
            typeof(ExecuteActivityReceiveEndpointDispatcher<,>).MakeGenericType(registrationType, arguments[0]))
            ?? throw new InvalidOperationException($"Unable to create an execute-activity dispatcher for '{registrationType}'."));
        dispatcher = dispatcherFactory.Create(factory, formatter);
        return true;
    }

    public void ConfigureTestHarness(IConsumerKindTestHarnessContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (IExecuteActivityRegistration registration in context.GetRegistrations<IExecuteActivityRegistration>())
            context.Observe(registration.Type);
    }


    sealed class Registration :
        IConsumerKindRegistration
    {
        static readonly IReadOnlyCollection<string> NoCompanionEndpoints = Array.Empty<string>();
        readonly IExecuteActivityRegistration _registration;

        public Registration(IExecuteActivityRegistration registration, IExecuteActivityDefinition definition, string endpointName)
        {
            _registration = registration;
            Definition = definition;
            EndpointDefinition = definition.ExecuteEndpointDefinition;
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
