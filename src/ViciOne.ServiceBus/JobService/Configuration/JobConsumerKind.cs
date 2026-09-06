using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.JobService;

sealed class JobConsumerKind :
    IConsumerKind,
    IConsumerKindServiceRequirement,
    IConsumerKindRuntimeConfigurator,
    IConsumerKindBulkConfigurator,
    IConsumerKindDispatcherProvider
{
    public bool IsFallback => false;

    public string Name => "Job";

    public int Order => 50;

    public IEnumerable<IConsumerKindRegistration> GetRegistrations(IConsumerKindContext context)
    {
        foreach (IConsumerRegistration registration in context.GetRegistrations<IConsumerRegistration>()
                     .Where(registration => IsJobConsumer(registration.Type)))
        {
            IConsumerDefinition definition = registration.GetDefinition(context.RegistrationContext);
            yield return new Registration(registration, definition,
                definition.GetEndpointName(context.EndpointNameFormatter));
        }
    }

    public bool RequiresServiceInstance(Type registrationType)
    {
        ArgumentNullException.ThrowIfNull(registrationType);
        return IsJobConsumer(registrationType);
    }

    public bool TryConfigure(Type registrationType, IReceiveEndpointConfigurator endpointConfigurator,
        IRegistrationContext registrationContext)
    {
        ArgumentNullException.ThrowIfNull(registrationType);
        ArgumentNullException.ThrowIfNull(endpointConfigurator);
        ArgumentNullException.ThrowIfNull(registrationContext);

        if (!IsJobConsumer(registrationType))
            return false;

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
            .Where(registration => IsJobConsumer(registration.Type) && !excludedRegistrationTypes.Contains(registration.Type))
            .ToArray();

        foreach (IConsumerRegistration registration in registrations)
            registration.Configure(endpointConfigurator, registrationContext);

        return registrations.Select(registration => registration.Type).ToArray();
    }

    public bool TryCreateDispatcher(Type registrationType, IReceiveEndpointDispatcherFactory factory,
        IEndpointNameFormatter formatter, out IReceiveEndpointDispatcher? dispatcher)
    {
        ArgumentNullException.ThrowIfNull(registrationType);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(formatter);

        if (!IsJobConsumer(registrationType))
        {
            dispatcher = null;
            return false;
        }

        var dispatcherFactory = (ITypeReceiveEndpointDispatcherFactory)(Activator.CreateInstance(
            typeof(ConsumerReceiveEndpointDispatcher<>).MakeGenericType(registrationType))
            ?? throw new InvalidOperationException($"Unable to create a job-consumer dispatcher for '{registrationType}'."));
        dispatcher = dispatcherFactory.Create(factory, formatter);
        return true;
    }

    public void ConfigureTestHarness(IConsumerKindTestHarnessContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (IConsumerRegistration registration in context.GetRegistrations<IConsumerRegistration>()
                     .Where(registration => IsJobConsumer(registration.Type)))
            context.Observe(registration.Type);
    }

    static bool IsJobConsumer(Type registrationType)
    {
        return registrationType.GetInterfaces()
            .Any(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IJobConsumer<>));
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
        public bool RequiresServiceInstance => true;
        public IReadOnlyCollection<string> CompanionEndpointNames => NoCompanionEndpoints;

        public void Configure(IConsumerKindEndpointContext context)
        {
            _registration.Configure(context.EndpointConfigurator, context.RegistrationContext);
        }
    }
}
