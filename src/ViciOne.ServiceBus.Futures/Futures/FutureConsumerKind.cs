using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Futures.DependencyInjection;

namespace ViciOne.ServiceBus.Futures;

internal sealed class FutureConsumerKind :
    IConsumerKind,
    IConsumerKindRuntimeConfigurator,
    IConsumerKindTypedConfigurator,
    IConsumerKindBulkConfigurator
{
    public bool IsFallback => false;

    public string Name => "Future";

    public int Order => 40;

    public IEnumerable<IConsumerKindRegistration> GetRegistrations(IConsumerKindContext context)
    {
        foreach (var registration in context.GetRegistrations<IFutureRegistration>())
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
        if (!selector.TryGetRegistration(registrationContext, registrationType, out IFutureRegistration? registration))
            return false;

        registration.Configure(endpointConfigurator, registrationContext);
        return true;
    }

    public bool TryConfigure<TRegistration>(IReceiveEndpointConfigurator endpointConfigurator,
        IRegistrationContext registrationContext, Delegate? configure = null)
        where TRegistration : class
    {
        if (configure != null)
            throw new ArgumentException("Future endpoint configuration does not accept a per-registration callback.", nameof(configure));

        return TryConfigure(typeof(TRegistration), endpointConfigurator, registrationContext);
    }

    public IReadOnlyCollection<Type> ConfigureAll(IReceiveEndpointConfigurator endpointConfigurator,
        IRegistrationContext registrationContext, IReadOnlySet<Type> excludedRegistrationTypes)
    {
        ArgumentNullException.ThrowIfNull(endpointConfigurator);
        ArgumentNullException.ThrowIfNull(registrationContext);
        ArgumentNullException.ThrowIfNull(excludedRegistrationTypes);

        IContainerSelector selector = registrationContext.GetRequiredService<IContainerSelector>();
        IFutureRegistration[] registrations =
        [
            .. selector.GetRegistrations<IFutureRegistration>(registrationContext)
                .Where(registration => !excludedRegistrationTypes.Contains(registration.Type))
        ];

        foreach (IFutureRegistration registration in registrations)
            registration.Configure(endpointConfigurator, registrationContext);

        return [.. registrations.Select(registration => registration.Type)];
    }

    public void ConfigureTestHarness(IConsumerKindTestHarnessContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (IFutureRegistration registration in context.GetRegistrations<IFutureRegistration>())
            context.Observe(typeof(FutureState), registration.Type);
    }


    sealed class Registration(IFutureRegistration registration, IFutureDefinition definition, string endpointName) :
        IConsumerKindRegistration
    {
        static readonly IReadOnlyCollection<string> NoCompanionEndpoints = [];
        readonly IFutureRegistration _registration = registration;

        public Type RegistrationType => _registration.Type;
        public IDefinition Definition { get; } = definition;
        public string EndpointName { get; } = endpointName;
        public IEndpointDefinition? EndpointDefinition { get; } = definition.EndpointDefinition;
        public bool RequiresServiceInstance => false;
        public IReadOnlyCollection<string> CompanionEndpointNames => NoCompanionEndpoints;

        public void Configure(IConsumerKindEndpointContext context)
        {
            _registration.Configure(context.EndpointConfigurator, context.RegistrationContext);
        }
    }
}
