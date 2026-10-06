using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Merges the endpoint settings and callbacks of components that share one receive endpoint.</summary>
internal sealed class CombinedEndpointDefinition :
    IEndpointDefinition
{
    readonly IRegistrationContext _context;
    readonly IReadOnlyList<IEndpointDefinition> _definitions;
    readonly EndpointTransportQos _transportQos;

    internal CombinedEndpointDefinition(IReadOnlyList<IEndpointDefinition> definitions, IRegistrationContext context, string endpointName)
    {
        _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        if (_definitions.Count == 0)
            throw new ArgumentException("At least one endpoint definition is required.", nameof(definitions));
        if (_definitions.Any(definition => definition is null))
            throw new ArgumentException("Endpoint definitions must not contain null values.", nameof(definitions));
        if (string.IsNullOrWhiteSpace(endpointName))
            throw new ArgumentException("Endpoint name must not be empty.", nameof(endpointName));

        EndpointQosDeclaration[] qosDeclarations = _definitions
            .Select(definition => new EndpointQosDeclaration(
                endpointName,
                definition is DelegateEndpointDefinition delegated
                    ? delegated.OwnerType
                    : definition.GetType(),
                new EndpointTransportQos
                {
                    PrefetchCount = definition.PrefetchCount,
                    ConcurrentDeliveryLimit = definition.ConcurrentMessageLimit
                },
                GetQosOwnership(definition)))
            .ToArray();
        _transportQos = new EndpointQosTopologyValidator()
            .Validate(qosDeclarations)
            .GetValueOrDefault(endpointName, new EndpointTransportQos());

        if (_definitions.All(x => x.ConfigureConsumeTopology))
            ConfigureConsumeTopology = true;
        else if (_definitions.All(x => x.ConfigureConsumeTopology == false))
            ConfigureConsumeTopology = false;
        else
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Combined Endpoint Definition", "unknown", $"Endpoints are not aligned on ConfigureConsumeTopology: {string.Join(", ", _definitions.Select(x => TypeCache.GetShortName(x.GetType())))}", "Correct the named configuration before starting the host"));
        }
    }

    /// <summary>Gets whether every component declares the shared endpoint temporary.</summary>
    public bool IsTemporary => _definitions.All(x => x.IsTemporary);

    /// <summary>Gets the validated effective prefetch count.</summary>
    public int? PrefetchCount => _transportQos.PrefetchCount;

    /// <summary>Gets the validated effective concurrent-delivery limit.</summary>
    public int? ConcurrentMessageLimit => _transportQos.ConcurrentDeliveryLimit;

    /// <summary>Gets the common consume-topology setting declared by all components.</summary>
    public bool ConfigureConsumeTopology { get; }

    /// <summary>Gets the common endpoint name from the first definition.</summary>
    /// <param name="formatter">The endpoint naming convention.</param>
    /// <returns>The shared endpoint name.</returns>
    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        return _definitions[0].GetEndpointName(formatter);
    }

    /// <summary>Applies every component definition to the shared endpoint.</summary>
    /// <typeparam name="TEndpointConfigurator">The transport-specific receive-endpoint configurator.</typeparam>
    /// <param name="configurator">The receive endpoint to configure.</param>
    /// <param name="context">An optional context that replaces the context captured during composition.</param>
    public void Configure<TEndpointConfigurator>(TEndpointConfigurator configurator, IRegistrationContext? context)
        where TEndpointConfigurator : IReceiveEndpointConfigurator
    {
        ArgumentNullException.ThrowIfNull(configurator);

        foreach (IEndpointDefinition definition in _definitions)
            definition.Configure(configurator, context ?? _context);
    }

    EndpointQosOwnership GetQosOwnership(IEndpointDefinition definition)
    {
        if (definition is not DelegateEndpointDefinition delegated)
            return EndpointQosOwnership.Endpoint;

        int owners = _definitions
            .OfType<DelegateEndpointDefinition>()
            .Count(candidate => ReferenceEquals(candidate.EndpointDefinition, delegated.EndpointDefinition));

        return owners > 1
            ? EndpointQosOwnership.Endpoint
            : EndpointQosOwnership.ConsumerDefinition;
    }
}
