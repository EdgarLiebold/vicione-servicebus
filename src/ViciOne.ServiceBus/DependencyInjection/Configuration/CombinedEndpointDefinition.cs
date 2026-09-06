using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines configuration for combined endpoint.</summary>
public class CombinedEndpointDefinition :
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

    /// <summary>Gets a value indicating whether temporary.</summary>
    public bool IsTemporary => _definitions.All(x => x.IsTemporary);

    /// <summary>Gets the prefetch count.</summary>
    public int? PrefetchCount => _transportQos.PrefetchCount;

    /// <summary>Gets the concurrent message limit.</summary>
    public int? ConcurrentMessageLimit => _transportQos.ConcurrentDeliveryLimit;

    /// <summary>Gets the configure consume topology.</summary>
    public bool ConfigureConsumeTopology { get; }

    /// <summary>Gets endpoint name.</summary>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The endpoint name.</returns>
    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        return _definitions[0].GetEndpointName(formatter);
    }

    /// <summary>Applies the supplied configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    public void Configure<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator
    {
        foreach (var definition in _definitions)
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
