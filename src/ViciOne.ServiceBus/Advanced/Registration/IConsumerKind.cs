using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>
/// Contributes one category of registered message handlers to endpoint materialization.
/// </summary>
public interface IConsumerKind
{
    /// <summary>
    /// Gets a value indicating whether the category is used only when no capability-specific category claims a registration.
    /// </summary>
    bool IsFallback => false;

    /// <summary>
    /// Gets the stable name used to identify the handler category in diagnostics and test harnesses.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the ordering used when multiple handler categories share an endpoint.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Selects the endpoint contributions for the current bus registration.
    /// </summary>
    /// <param name="context">The endpoint-planning context.</param>
    /// <returns>The endpoint contributions owned by this category.</returns>
    IEnumerable<IConsumerKindRegistration> GetRegistrations(IConsumerKindContext context);

    /// <summary>
    /// Determines whether a handler type must be hosted by a service instance.
    /// </summary>
    /// <param name="registrationType">The registered handler type.</param>
    /// <returns><see langword="true" /> when a service instance is required; otherwise, <see langword="false" />.</returns>
    bool RequiresServiceInstance(Type registrationType)
    {
        return false;
    }

    /// <summary>
    /// Attempts to configure a registration selected by runtime type on an existing endpoint.
    /// </summary>
    /// <param name="registrationType">The registration type.</param>
    /// <param name="endpointConfigurator">The existing endpoint configurator.</param>
    /// <param name="registrationContext">The active registration context.</param>
    /// <returns><see langword="true" /> when this consumer kind owns and configured the type.</returns>
    bool TryConfigure(Type registrationType, IReceiveEndpointConfigurator endpointConfigurator,
        IRegistrationContext registrationContext)
    {
        return false;
    }

    /// <summary>
    /// Attempts to configure a registration that owns a primary and companion endpoint.
    /// </summary>
    bool TryConfigurePair(Type registrationType, IReceiveEndpointConfigurator primaryEndpointConfigurator,
        IReceiveEndpointConfigurator companionEndpointConfigurator, IRegistrationContext registrationContext)
    {
        return false;
    }

    /// <summary>
    /// Attempts to configure the primary endpoint for a registration whose companion endpoint already has an address.
    /// </summary>
    bool TryConfigurePrimary(Type registrationType, IReceiveEndpointConfigurator primaryEndpointConfigurator,
        Uri companionAddress, IRegistrationContext registrationContext)
    {
        return false;
    }

    /// <summary>
    /// Attempts to configure the companion endpoint for a registration.
    /// </summary>
    bool TryConfigureCompanion(Type registrationType, IReceiveEndpointConfigurator companionEndpointConfigurator,
        IRegistrationContext registrationContext)
    {
        return false;
    }

    /// <summary>
    /// Attempts to configure a typed registration and an optional capability-specific callback on an existing endpoint.
    /// </summary>
    /// <typeparam name="TRegistration">The registration type.</typeparam>
    /// <param name="endpointConfigurator">The existing endpoint configurator.</param>
    /// <param name="registrationContext">The active registration context.</param>
    /// <param name="configure">The optional capability-specific configuration callback.</param>
    /// <returns><see langword="true" /> when this consumer kind owns and configured the type.</returns>
    bool TryConfigure<TRegistration>(IReceiveEndpointConfigurator endpointConfigurator,
        IRegistrationContext registrationContext, Delegate? configure = null)
        where TRegistration : class
    {
        return false;
    }

    /// <summary>
    /// Configures all registrations owned by this consumer kind on an existing endpoint.
    /// </summary>
    /// <param name="endpointConfigurator">The existing endpoint configurator.</param>
    /// <param name="registrationContext">The active registration context.</param>
    /// <param name="excludedRegistrationTypes">Registration types that were already configured.</param>
    /// <returns>The types configured by this invocation.</returns>
    IReadOnlyCollection<Type> ConfigureAll(IReceiveEndpointConfigurator endpointConfigurator,
        IRegistrationContext registrationContext, IReadOnlySet<Type> excludedRegistrationTypes)
    {
        return Array.Empty<Type>();
    }

    /// <summary>
    /// Attempts to create a typed dispatcher for a registration owned by this consumer kind.
    /// </summary>
    /// <param name="registrationType">The registration type.</param>
    /// <param name="factory">The dispatcher factory.</param>
    /// <param name="formatter">The endpoint-name formatter.</param>
    /// <param name="dispatcher">The created dispatcher when the type is owned by this consumer kind.</param>
    /// <returns><see langword="true" /> when a dispatcher was created.</returns>
    bool TryCreateDispatcher(Type registrationType, IReceiveEndpointDispatcherFactory factory,
        IEndpointNameFormatter formatter, out IReceiveEndpointDispatcher? dispatcher)
    {
        dispatcher = null;
        return false;
    }

    /// <summary>
    /// Contributes the registrations owned by this category to a container test harness.
    /// </summary>
    /// <param name="context">The test-harness observation context.</param>
    void ConfigureTestHarness(IConsumerKindTestHarnessContext context)
    {
    }
}

/// <summary>
/// Provides the pre-container registration view used to attach test-harness observations to consumer kinds.
/// </summary>
public interface IConsumerKindTestHarnessContext
{
    /// <summary>
    /// Gets the stable name of the consumer kind being observed.
    /// </summary>
    string KindName { get; }

    /// <summary>
    /// Enumerates registrations owned by the active bus registration.
    /// </summary>
    /// <typeparam name="TRegistration">The registration contract.</typeparam>
    /// <returns>The registrations visible before the service provider is built.</returns>
    IEnumerable<TRegistration> GetRegistrations<TRegistration>()
        where TRegistration : class, IRegistration;

    /// <summary>
    /// Attaches test-harness observation for a registration and any supporting implementation types.
    /// </summary>
    /// <param name="registrationType">The registered handler or state type.</param>
    /// <param name="supportingTypes">Capability-specific implementation types required by the observation.</param>
    void Observe(Type registrationType, params Type[] supportingTypes);
}

/// <summary>
/// Provides the bus-scoped registration view used by a consumer kind.
/// </summary>
public interface IConsumerKindContext
{
    /// <summary>
    /// Gets the active bus registration context.
    /// </summary>
    IRegistrationContext RegistrationContext { get; }

    /// <summary>
    /// Gets the endpoint name formatter selected for the bus.
    /// </summary>
    IEndpointNameFormatter EndpointNameFormatter { get; }

    /// <summary>
    /// Enumerates registrations that are enabled, selected by the endpoint filter, and not already configured.
    /// </summary>
    /// <typeparam name="TRegistration">The registration contract.</typeparam>
    /// <returns>The selected registrations.</returns>
    IEnumerable<TRegistration> GetRegistrations<TRegistration>()
        where TRegistration : class, IRegistration;
}

/// <summary>
/// Describes one handler registration that contributes to a receive endpoint.
/// </summary>
public interface IConsumerKindRegistration
{
    /// <summary>
    /// Gets the registered handler type.
    /// </summary>
    Type RegistrationType { get; }

    /// <summary>
    /// Gets the definition that owns the endpoint contribution.
    /// </summary>
    IDefinition Definition { get; }

    /// <summary>
    /// Gets the endpoint name selected for the registration.
    /// </summary>
    string EndpointName { get; }

    /// <summary>
    /// Gets the endpoint definition supplied by the registration, if any.
    /// </summary>
    IEndpointDefinition? EndpointDefinition { get; }

    /// <summary>
    /// Gets a value indicating whether the endpoint must be hosted by a service instance.
    /// </summary>
    bool RequiresServiceInstance { get; }

    /// <summary>
    /// Gets endpoint names materialized as companions rather than primary endpoints.
    /// </summary>
    IReadOnlyCollection<string> CompanionEndpointNames { get; }

    /// <summary>
    /// Configures the registration on its materialized endpoint.
    /// </summary>
    /// <param name="context">The materialized endpoint context.</param>
    void Configure(IConsumerKindEndpointContext context);
}

/// <summary>
/// Provides the materialized endpoint and companion-endpoint factory to a consumer-kind registration.
/// </summary>
public interface IConsumerKindEndpointContext
{
    /// <summary>
    /// Gets the active bus registration context.
    /// </summary>
    IRegistrationContext RegistrationContext { get; }

    /// <summary>
    /// Gets the primary receive endpoint configurator.
    /// </summary>
    IReceiveEndpointConfigurator EndpointConfigurator { get; }

    /// <summary>
    /// Materializes a companion endpoint required by the registration.
    /// </summary>
    /// <param name="endpointName">The companion endpoint name.</param>
    /// <param name="endpointDefinition">The companion endpoint definition, if any.</param>
    /// <param name="configure">The companion endpoint configuration callback.</param>
    void ConfigureCompanionEndpoint(string endpointName, IEndpointDefinition? endpointDefinition,
        Action<IReceiveEndpointConfigurator> configure);
}

/// <summary>
/// Hosts consumer-kind endpoints that require a service-instance endpoint.
/// </summary>
public interface IConsumerKindHost :
    IRegistration
{
    /// <summary>
    /// Gets the definition for the service-instance endpoint.
    /// </summary>
    IEndpointDefinition EndpointDefinition { get; }

    /// <summary>
    /// Configures the service instance used by the contributed endpoints.
    /// </summary>
    /// <param name="configurator">The service-instance configurator.</param>
    /// <param name="context">The active registration context.</param>
    void Configure(IServiceInstanceConfigurator configurator, IRegistrationContext context);
}
