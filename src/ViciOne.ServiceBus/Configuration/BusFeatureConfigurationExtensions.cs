using System;
using Microsoft.Extensions.DependencyInjection;


namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Places application-owned contract and diagnostic policy declarations inside their owning bus block.
/// </summary>
public static class BusFeatureConfigurationExtensions
{
    /// <summary>Adds contract declarations owned by the default bus.</summary>
    public static IBusRegistrationConfigurator Contracts(
        this IBusRegistrationConfigurator configurator,
        Action<MessageContractCatalogBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        RegisterContracts<IBus>(configurator.Services, configure);
        return configurator;
    }

    /// <summary>Adds contract declarations owned by a typed bus.</summary>
    public static IBusRegistrationConfigurator<TBus> Contracts<TBus>(
        this IBusRegistrationConfigurator<TBus> configurator,
        Action<MessageContractCatalogBuilder> configure)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(configurator);
        RegisterContracts<TBus>(configurator.Services, configure);
        return configurator;
    }

    /// <summary>Enables conservative diagnostic redaction for the default bus.</summary>
    public static IBusRegistrationConfigurator Redaction(this IBusRegistrationConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        RegisterRedaction<IBus>(configurator.Services);
        return configurator;
    }

    /// <summary>Enables conservative diagnostic redaction for a typed bus.</summary>
    public static IBusRegistrationConfigurator<TBus> Redaction<TBus>(this IBusRegistrationConfigurator<TBus> configurator)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(configurator);
        RegisterRedaction<TBus>(configurator.Services);
        return configurator;
    }

    internal static void RegisterContracts<TBus>(
        IServiceCollection services,
        Action<MessageContractCatalogBuilder> configure)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        BusCompositionRegistrations.AddFeature<TBus>(services, "Message contracts", allowMultipleDeclarations: true);
        services.AddViciOneMessageContracts(configure);
    }

    static void RegisterRedaction<TBus>(IServiceCollection services)
        where TBus : class, IBus
    {
        BusCompositionRegistrations.AddFeature<TBus>(services, "Diagnostic redaction", allowMultipleDeclarations: true);
        services.AddViciOneMessageDiagnosticRedaction();
    }
}
