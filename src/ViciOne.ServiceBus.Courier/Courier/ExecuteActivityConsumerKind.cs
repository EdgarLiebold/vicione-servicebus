using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Courier;

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
        ArgumentNullException.ThrowIfNull(context);

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
        ArgumentNullException.ThrowIfNull(registrationType);
        ArgumentNullException.ThrowIfNull(endpointConfigurator);
        ArgumentNullException.ThrowIfNull(registrationContext);

        IContainerSelector selector = registrationContext.GetRequiredService<IContainerSelector>();
        if (!selector.TryGetRegistration(registrationContext, registrationType, out IExecuteActivityRegistration? registration))
            return false;

        registration.Configure(endpointConfigurator, registrationContext);
        return true;
    }

    public bool TryCreateDispatcher(Type registrationType, IReceiveEndpointDispatcherFactory factory,
        IEndpointNameFormatter formatter, [NotNullWhen(true)] out IReceiveEndpointDispatcher? dispatcher)
    {
        ArgumentNullException.ThrowIfNull(registrationType);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(formatter);

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
        static readonly IReadOnlyCollection<string> NoCompanionEndpoints = [];
        readonly IExecuteActivityRegistration _registration;

        public Registration(IExecuteActivityRegistration registration, IExecuteActivityDefinition definition, string endpointName)
        {
            ArgumentNullException.ThrowIfNull(registration);
            ArgumentNullException.ThrowIfNull(definition);
            ArgumentException.ThrowIfNullOrWhiteSpace(endpointName);

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
            ArgumentNullException.ThrowIfNull(context);

            _registration.Configure(context.EndpointConfigurator, context.RegistrationContext);
        }
    }
}
