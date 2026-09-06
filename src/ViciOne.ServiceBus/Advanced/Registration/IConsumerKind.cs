using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Contributes one category of registered message handlers to endpoint materialization.</summary>
public interface IConsumerKind
{
    /// <summary>Gets a value indicating whether the category is used only when no capability-specific category claims a registration.</summary>
    bool IsFallback => false;

    /// <summary>Gets the name.</summary>
    string Name { get; }

    /// <summary>Gets the order.</summary>
    int Order { get; }

    /// <summary>Selects the endpoint contributions for the current bus registration.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The endpoint contributions owned by this category.</returns>
    IEnumerable<IConsumerKindRegistration> GetRegistrations(IConsumerKindContext context);

    /// <summary>Determines whether a handler type must be hosted by a service instance.</summary>
    /// <param name="registrationType">The runtime registration type used by the operation.</param>
    /// <returns><see langword="true" /> when a service instance is required; otherwise, <see langword="false" />.</returns>
    bool RequiresServiceInstance(Type registrationType)
    {
        return false;
    }

    /// <summary>Attempts to configure a registration selected by runtime type on an existing endpoint.</summary>
    /// <param name="registrationType">The runtime registration type used by the operation.</param>
    /// <param name="endpointConfigurator">The endpoint configurator.</param>
    /// <param name="registrationContext">The registration context.</param>
    /// <returns><see langword="true" /> when this consumer kind owns and configured the type.</returns>
    bool TryConfigure(Type registrationType, IReceiveEndpointConfigurator endpointConfigurator,
        IRegistrationContext registrationContext)
    {
        return false;
    }

    /// <summary>Attempts to configure a registration that owns a primary and companion endpoint.</summary>
    /// <param name="registrationType">The runtime registration type used by the operation.</param>
    /// <param name="primaryEndpointConfigurator">The primary endpoint configurator.</param>
    /// <param name="companionEndpointConfigurator">The companion endpoint configurator.</param>
    /// <param name="registrationContext">The registration context.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryConfigurePair(Type registrationType, IReceiveEndpointConfigurator primaryEndpointConfigurator,
        IReceiveEndpointConfigurator companionEndpointConfigurator, IRegistrationContext registrationContext)
    {
        return false;
    }

    /// <summary>Attempts to configure the primary endpoint for a registration whose companion endpoint already has an address.</summary>
    /// <param name="registrationType">The runtime registration type used by the operation.</param>
    /// <param name="primaryEndpointConfigurator">The primary endpoint configurator.</param>
    /// <param name="companionAddress">The companion address.</param>
    /// <param name="registrationContext">The registration context.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryConfigurePrimary(Type registrationType, IReceiveEndpointConfigurator primaryEndpointConfigurator,
        Uri companionAddress, IRegistrationContext registrationContext)
    {
        return false;
    }

    /// <summary>Attempts to configure the companion endpoint for a registration.</summary>
    /// <param name="registrationType">The runtime registration type used by the operation.</param>
    /// <param name="companionEndpointConfigurator">The companion endpoint configurator.</param>
    /// <param name="registrationContext">The registration context.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryConfigureCompanion(Type registrationType, IReceiveEndpointConfigurator companionEndpointConfigurator,
        IRegistrationContext registrationContext)
    {
        return false;
    }

    /// <summary>Attempts to configure a typed registration and an optional capability-specific callback on an existing endpoint.</summary>
    /// <typeparam name="TRegistration">The registration type.</typeparam>
    /// <param name="endpointConfigurator">The endpoint configurator.</param>
    /// <param name="registrationContext">The registration context.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns><see langword="true" /> when this consumer kind owns and configured the type.</returns>
    bool TryConfigure<TRegistration>(IReceiveEndpointConfigurator endpointConfigurator,
        IRegistrationContext registrationContext, Delegate? configure = null)
        where TRegistration : class
    {
        return false;
    }

    /// <summary>Configures all registrations owned by this consumer kind on an existing endpoint.</summary>
    /// <param name="endpointConfigurator">The endpoint configurator.</param>
    /// <param name="registrationContext">The registration context.</param>
    /// <param name="excludedRegistrationTypes">Registration types that were already configured.</param>
    /// <returns>The types configured by this invocation.</returns>
    IReadOnlyCollection<Type> ConfigureAll(IReceiveEndpointConfigurator endpointConfigurator,
        IRegistrationContext registrationContext, IReadOnlySet<Type> excludedRegistrationTypes)
    {
        return Array.Empty<Type>();
    }

    /// <summary>Attempts to create a typed dispatcher for a registration owned by this consumer kind.</summary>
    /// <param name="registrationType">The runtime registration type used by the operation.</param>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="formatter">The formatter.</param>
    /// <param name="dispatcher">The created dispatcher when the type is owned by this consumer kind.</param>
    /// <returns><see langword="true" /> when a dispatcher was created.</returns>
    bool TryCreateDispatcher(Type registrationType, IReceiveEndpointDispatcherFactory factory,
        IEndpointNameFormatter formatter, out IReceiveEndpointDispatcher? dispatcher)
    {
        dispatcher = null;
        return false;
    }

    /// <summary>Contributes the registrations owned by this category to a container test harness.</summary>
    /// <param name="context">The context associated with the operation.</param>
    void ConfigureTestHarness(IConsumerKindTestHarnessContext context)
    {
    }
}
